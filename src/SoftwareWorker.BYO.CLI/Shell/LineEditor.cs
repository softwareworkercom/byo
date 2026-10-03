using System.Text;

namespace SoftwareWorker.BYO.CLI.Shell
{
    internal enum ShellReadStatus
    {
        Submitted,
        Cancelled,
        EndOfInput
    }

    internal sealed record ShellReadResult(ShellReadStatus Status, string Text);

    /// <param name="Text">The prompt as plain text, used to measure its width.</param>
    /// <param name="Ansi">The prompt as written to the terminal, including ANSI styles.</param>
    internal sealed record ShellPrompt(string Text, string Ansi);

    /// <summary>
    /// An interactive line editor inspired by PrettyPrompt: syntax highlighting, a completion menu with
    /// documentation, history navigation filtered by the typed prefix, inline history suggestions,
    /// selection, undo/redo and word navigation. Long lines wrap across terminal rows.
    /// </summary>
    internal sealed class LineEditor
    {
        public const int MaxVisibleMenuItems = 8;
        private const string WordDelimiters = " \t/\\:=.,;'\"()[]{}|";

        private readonly IShellConsole _console;
        private readonly ShellHistory _history;
        private readonly Func<string, int, ShellCompletionResult> _getCompletions;
        private readonly Func<string, IReadOnlyList<ClassifiedToken>> _classify;
        private readonly Stack<EditorSnapshot> _undo = new();
        private readonly Stack<EditorSnapshot> _redo = new();

        private ShellPrompt _prompt = new("> ", "> ");
        private string _text = string.Empty;
        private int _caret;
        private int? _selectionAnchor;
        private EditKind _lastEdit;

        private ShellCompletionResult? _completion;
        private int _menuIndex;
        private int _menuScroll;
        private bool _isTextChanged;
        private bool _isAutoCompletionRequested;

        private int _historyIndex = -1;
        private string _historyPrefix = string.Empty;
        private string _historyDraft = string.Empty;

        private int _renderedCaretRow;

        public LineEditor(
            IShellConsole console,
            ShellHistory history,
            Func<string, int, ShellCompletionResult> getCompletions,
            Func<string, IReadOnlyList<ClassifiedToken>> classify)
        {
            _console = console;
            _history = history;
            _getCompletions = getCompletions;
            _classify = classify;
        }

        private enum EditKind
        {
            None,
            Insert,
            Delete,
            Other
        }

        private enum KeyOutcome
        {
            Continue,
            Submit,
            Cancel,
            EndOfInput
        }

        private readonly record struct EditorSnapshot(string Text, int Caret);

        public string Text => _text;

        public int Caret => _caret;

        public bool IsCompletionMenuOpen => _completion != null;

        public IReadOnlyList<ShellCompletionItem> CompletionItems => _completion?.Items ?? [];

        public int SelectedCompletionIndex => _menuIndex;

        private bool HasSelection => _selectionAnchor.HasValue && _selectionAnchor.Value != _caret;

        private int SelectionStart => Math.Min(_selectionAnchor ?? _caret, _caret);

        private int SelectionEnd => Math.Max(_selectionAnchor ?? _caret, _caret);

        public ShellReadResult ReadLine(ShellPrompt prompt)
        {
            _prompt = prompt;
            ResetState();

            var treatControlCAsInput = _console.TreatControlCAsInput;
            _console.TreatControlCAsInput = true;

            try
            {
                if (_console.CursorLeft > 0)
                {
                    _console.Write("\r\n");
                }

                _renderedCaretRow = 0;
                Render();

                while (true)
                {
                    var outcome = HandleKey(_console.ReadKey());

                    // Process pasted text in one go instead of re-rendering after every character.
                    while (outcome == KeyOutcome.Continue && _console.KeyAvailable)
                    {
                        outcome = HandleKey(_console.ReadKey());
                    }

                    switch (outcome)
                    {
                        case KeyOutcome.Submit:
                            Finish(marker: null);
                            return new ShellReadResult(ShellReadStatus.Submitted, _text);
                        case KeyOutcome.Cancel:
                            Finish(marker: "^C");
                            return new ShellReadResult(ShellReadStatus.Cancelled, _text);
                        case KeyOutcome.EndOfInput:
                            Finish(marker: null);
                            return new ShellReadResult(ShellReadStatus.EndOfInput, string.Empty);
                        default:
                            UpdateCompletionsAfterEdit();
                            Render();
                            break;
                    }
                }
            }
            finally
            {
                _console.TreatControlCAsInput = treatControlCAsInput;
            }
        }

        private void ResetState()
        {
            _text = string.Empty;
            _caret = 0;
            _selectionAnchor = null;
            _lastEdit = EditKind.None;
            _undo.Clear();
            _redo.Clear();
            _completion = null;
            _isTextChanged = false;
            _isAutoCompletionRequested = false;
            _historyIndex = -1;
        }

        private KeyOutcome HandleKey(ConsoleKeyInfo key)
        {
            var control = (key.Modifiers & ConsoleModifiers.Control) != 0;
            var shift = (key.Modifiers & ConsoleModifiers.Shift) != 0;
            var alt = (key.Modifiers & ConsoleModifiers.Alt) != 0;

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    return _completion != null && AcceptCompletion(isSubmitting: true)
                        ? KeyOutcome.Continue
                        : KeyOutcome.Submit;
                case ConsoleKey.Tab:
                    if (_completion != null)
                    {
                        if (shift)
                        {
                            MoveMenuSelection(-1);
                        }
                        else
                        {
                            AcceptCompletion(isSubmitting: false);
                        }
                    }
                    else if (!shift)
                    {
                        OpenCompletionsOnRequest();
                    }
                    return KeyOutcome.Continue;
                case ConsoleKey.Escape:
                    if (_completion != null)
                    {
                        CloseMenu();
                    }
                    else if (HasSelection)
                    {
                        _selectionAnchor = null;
                    }
                    else if (_text.Length > 0)
                    {
                        ReplaceText(0, _text.Length, string.Empty, EditKind.Other);
                    }
                    return KeyOutcome.Continue;
                case ConsoleKey.UpArrow:
                    if (_completion != null)
                    {
                        MoveMenuSelection(-1);
                    }
                    else
                    {
                        ShowPreviousHistoryEntry();
                    }
                    return KeyOutcome.Continue;
                case ConsoleKey.DownArrow:
                    if (_completion != null)
                    {
                        MoveMenuSelection(1);
                    }
                    else
                    {
                        ShowNextHistoryEntry();
                    }
                    return KeyOutcome.Continue;
                case ConsoleKey.PageUp when _completion != null:
                    MoveMenuSelection(-MaxVisibleMenuItems);
                    return KeyOutcome.Continue;
                case ConsoleKey.PageDown when _completion != null:
                    MoveMenuSelection(MaxVisibleMenuItems);
                    return KeyOutcome.Continue;
                case ConsoleKey.LeftArrow:
                    if (!shift && HasSelection)
                    {
                        MoveCaret(SelectionStart, extendSelection: false);
                    }
                    else
                    {
                        MoveCaret(control ? FindWordStart(_text, _caret) : _caret - 1, shift);
                    }
                    return KeyOutcome.Continue;
                case ConsoleKey.RightArrow:
                    if (!shift && AcceptSuggestion(nextWordOnly: control))
                    {
                        return KeyOutcome.Continue;
                    }

                    if (!shift && HasSelection)
                    {
                        MoveCaret(SelectionEnd, extendSelection: false);
                    }
                    else
                    {
                        MoveCaret(control ? FindWordEnd(_text, _caret) : _caret + 1, shift);
                    }
                    return KeyOutcome.Continue;
                case ConsoleKey.Home:
                    MoveCaret(0, shift);
                    return KeyOutcome.Continue;
                case ConsoleKey.End:
                    if (!shift && AcceptSuggestion(nextWordOnly: false))
                    {
                        return KeyOutcome.Continue;
                    }

                    MoveCaret(_text.Length, shift);
                    return KeyOutcome.Continue;
                case ConsoleKey.Backspace:
                    DeleteBackward(wholeWord: control);
                    return KeyOutcome.Continue;
                case ConsoleKey.Delete:
                    DeleteForward(wholeWord: control);
                    return KeyOutcome.Continue;
                case ConsoleKey.Spacebar when control:
                    OpenCompletionsOnRequest();
                    return KeyOutcome.Continue;
            }

            // AltGr is reported as Control+Alt on Windows and must still type characters.
            if (control && !alt)
            {
                return HandleControlKey(key.Key);
            }

            if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
            {
                InsertCharacter(key.KeyChar);
            }

            return KeyOutcome.Continue;
        }

        private KeyOutcome HandleControlKey(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.A:
                    CloseMenu();
                    _selectionAnchor = 0;
                    _caret = _text.Length;
                    break;
                case ConsoleKey.C:
                    return KeyOutcome.Cancel;
                case ConsoleKey.D:
                    if (_text.Length == 0)
                    {
                        return KeyOutcome.EndOfInput;
                    }
                    DeleteForward(wholeWord: false);
                    break;
                case ConsoleKey.L:
                    _console.Write("\u001b[2J\u001b[H");
                    _renderedCaretRow = 0;
                    break;
                case ConsoleKey.Z:
                    Undo();
                    break;
                case ConsoleKey.Y:
                    Redo();
                    break;
                case ConsoleKey.W:
                    DeleteBackward(wholeWord: true);
                    break;
                case ConsoleKey.U:
                    if (_caret > 0)
                    {
                        ReplaceText(0, _caret, string.Empty, EditKind.Other);
                    }
                    break;
                case ConsoleKey.K:
                    if (_caret < _text.Length)
                    {
                        ReplaceText(_caret, _text.Length, string.Empty, EditKind.Other);
                    }
                    break;
            }

            return KeyOutcome.Continue;
        }

        #region Editing

        private void InsertCharacter(char character)
        {
            var isWhitespace = char.IsWhiteSpace(character);
            var start = HasSelection ? SelectionStart : _caret;
            var end = HasSelection ? SelectionEnd : _caret;

            ReplaceText(start, end, character.ToString(), isWhitespace || start != end ? EditKind.Other : EditKind.Insert);

            if (isWhitespace)
            {
                CloseMenu();
                _isAutoCompletionRequested = false;
            }
            else
            {
                _isAutoCompletionRequested = true;
            }
        }

        private void DeleteBackward(bool wholeWord)
        {
            if (HasSelection)
            {
                ReplaceText(SelectionStart, SelectionEnd, string.Empty, EditKind.Other);
                return;
            }

            if (_caret == 0)
            {
                return;
            }

            var start = wholeWord ? FindWordStart(_text, _caret) : _caret - 1;
            ReplaceText(start, _caret, string.Empty, wholeWord ? EditKind.Other : EditKind.Delete);
        }

        private void DeleteForward(bool wholeWord)
        {
            if (HasSelection)
            {
                ReplaceText(SelectionStart, SelectionEnd, string.Empty, EditKind.Other);
                return;
            }

            if (_caret >= _text.Length)
            {
                return;
            }

            var end = wholeWord ? FindWordEnd(_text, _caret) : _caret + 1;
            ReplaceText(_caret, end, string.Empty, wholeWord ? EditKind.Other : EditKind.Delete);
        }

        private void ReplaceText(int start, int end, string replacement, EditKind kind)
        {
            RecordUndo(kind);
            _text = string.Concat(_text.AsSpan(0, start), replacement, _text.AsSpan(end));
            _caret = start + replacement.Length;
            _selectionAnchor = null;
            _historyIndex = -1;
            _isTextChanged = true;
        }

        private void MoveCaret(int position, bool extendSelection)
        {
            if (extendSelection)
            {
                _selectionAnchor ??= _caret;
            }
            else
            {
                _selectionAnchor = null;
            }

            _caret = Math.Clamp(position, 0, _text.Length);
            _lastEdit = EditKind.None;
            CloseMenu();
        }

        private void RecordUndo(EditKind kind)
        {
            // Consecutive typing or deleting is undone as a single step.
            if (kind != EditKind.Other && kind == _lastEdit)
            {
                return;
            }

            _undo.Push(new EditorSnapshot(_text, _caret));
            _redo.Clear();
            _lastEdit = kind;
        }

        private void Undo()
        {
            if (_undo.Count == 0)
            {
                return;
            }

            _redo.Push(new EditorSnapshot(_text, _caret));
            RestoreSnapshot(_undo.Pop());
        }

        private void Redo()
        {
            if (_redo.Count == 0)
            {
                return;
            }

            _undo.Push(new EditorSnapshot(_text, _caret));
            RestoreSnapshot(_redo.Pop());
        }

        private void RestoreSnapshot(EditorSnapshot snapshot)
        {
            _text = snapshot.Text;
            _caret = Math.Clamp(snapshot.Caret, 0, _text.Length);
            _selectionAnchor = null;
            _lastEdit = EditKind.None;
            _historyIndex = -1;
            CloseMenu();
        }

        private static bool IsWordDelimiter(char character) => WordDelimiters.Contains(character);

        private static int FindWordStart(string text, int position)
        {
            var index = Math.Min(position, text.Length);
            while (index > 0 && IsWordDelimiter(text[index - 1]))
            {
                index--;
            }

            while (index > 0 && !IsWordDelimiter(text[index - 1]))
            {
                index--;
            }

            return index;
        }

        private static int FindWordEnd(string text, int position)
        {
            var index = Math.Max(position, 0);
            while (index < text.Length && IsWordDelimiter(text[index]))
            {
                index++;
            }

            while (index < text.Length && !IsWordDelimiter(text[index]))
            {
                index++;
            }

            return index;
        }

        #endregion

        #region History

        private string? GetSuggestion()
        {
            if (HasSelection || _caret != _text.Length)
            {
                return null;
            }

            return _history.FindSuggestion(_text);
        }

        private bool AcceptSuggestion(bool nextWordOnly)
        {
            var suggestion = GetSuggestion();
            if (suggestion == null)
            {
                return false;
            }

            CloseMenu();
            var end = nextWordOnly ? FindWordEnd(suggestion, _text.Length) : suggestion.Length;
            ReplaceText(0, _text.Length, suggestion[..end], EditKind.Other);
            return true;
        }

        private void ShowPreviousHistoryEntry()
        {
            var entries = _history.Entries;
            if (_historyIndex == -1)
            {
                // The text typed before navigating filters the history, like PrettyPrompt.
                _historyDraft = _text;
                _historyPrefix = _text;
                _historyIndex = entries.Count;
            }

            for (var index = _historyIndex - 1; index >= 0; index--)
            {
                if (entries[index].StartsWith(_historyPrefix, StringComparison.Ordinal) && entries[index] != _text)
                {
                    _historyIndex = index;
                    ShowHistoryText(entries[index]);
                    return;
                }
            }
        }

        private void ShowNextHistoryEntry()
        {
            if (_historyIndex == -1)
            {
                return;
            }

            var entries = _history.Entries;
            for (var index = _historyIndex + 1; index < entries.Count; index++)
            {
                if (entries[index].StartsWith(_historyPrefix, StringComparison.Ordinal) && entries[index] != _text)
                {
                    _historyIndex = index;
                    ShowHistoryText(entries[index]);
                    return;
                }
            }

            _historyIndex = -1;
            ShowHistoryText(_historyDraft);
        }

        private void ShowHistoryText(string text)
        {
            _text = text;
            _caret = text.Length;
            _selectionAnchor = null;
            _lastEdit = EditKind.None;
            CloseMenu();
        }

        #endregion

        #region Completion

        private void OpenCompletionsOnRequest()
        {
            if (UpdateCompletions(isRequested: true) && _completion!.Items.Count == 1)
            {
                AcceptCompletion(isSubmitting: false);
            }
        }

        private void UpdateCompletionsAfterEdit()
        {
            if (!_isTextChanged)
            {
                return;
            }

            _isTextChanged = false;
            if (_completion != null || _isAutoCompletionRequested)
            {
                UpdateCompletions(isRequested: false);
            }

            _isAutoCompletionRequested = false;
        }

        private bool UpdateCompletions(bool isRequested)
        {
            ShellCompletionResult result;
            try
            {
                result = _getCompletions(_text, _caret);
            }
            catch
            {
                result = ShellCompletionResult.Empty(_caret);
            }

            var hasNothingToOffer = result.Items.Count == 0 ||
                (!isRequested && result.WordToComplete.Length == 0) ||
                (!isRequested && result.Items.Count == 1 && result.Items[0].InsertText == GetReplacedText(result));

            if (hasNothingToOffer)
            {
                CloseMenu();
                return false;
            }

            // The list is re-ranked on every keystroke, so the best match is always selected first.
            _completion = result;
            _menuIndex = 0;
            _menuScroll = 0;
            return true;
        }

        private bool AcceptCompletion(bool isSubmitting)
        {
            var completion = _completion;
            if (completion == null || completion.Items.Count == 0)
            {
                return false;
            }

            var item = completion.Items[Math.Clamp(_menuIndex, 0, completion.Items.Count - 1)];
            var start = Math.Min(completion.ReplaceStart, _text.Length);
            var end = Math.Clamp(completion.ReplaceEnd, start, _text.Length);
            CloseMenu();

            // Pressing Enter on an already complete word runs the line instead of re-inserting the word.
            if (isSubmitting && _text[start..end] == item.InsertText)
            {
                return false;
            }

            var isDirectory = item.Kind == ShellCompletionKind.Directory;
            var isFollowedBySpace = end < _text.Length && char.IsWhiteSpace(_text[end]);
            var replacement = item.InsertText + (isDirectory || isFollowedBySpace ? string.Empty : " ");

            ReplaceText(start, end, replacement, EditKind.Other);
            if (isFollowedBySpace && !isDirectory)
            {
                _caret++;
            }

            _isTextChanged = false;
            if (isDirectory)
            {
                // Keep drilling into folders without pressing Tab again.
                UpdateCompletions(isRequested: false);
            }

            return true;
        }

        private string GetReplacedText(ShellCompletionResult result)
        {
            var start = Math.Min(result.ReplaceStart, _text.Length);
            var end = Math.Clamp(result.ReplaceEnd, start, _text.Length);
            return _text[start..end];
        }

        private void MoveMenuSelection(int delta)
        {
            if (_completion == null || _completion.Items.Count == 0)
            {
                return;
            }

            var count = _completion.Items.Count;
            _menuIndex = Math.Abs(delta) == 1
                ? (_menuIndex + delta + count) % count
                : Math.Clamp(_menuIndex + delta, 0, count - 1);
            EnsureMenuSelectionVisible();
        }

        private void EnsureMenuSelectionVisible()
        {
            if (_menuIndex < _menuScroll)
            {
                _menuScroll = _menuIndex;
            }
            else if (_menuIndex >= _menuScroll + MaxVisibleMenuItems)
            {
                _menuScroll = _menuIndex - MaxVisibleMenuItems + 1;
            }
        }

        private void CloseMenu()
        {
            _completion = null;
            _menuIndex = 0;
            _menuScroll = 0;
        }

        #endregion

        #region Rendering

        private void Finish(string? marker)
        {
            CloseMenu();
            _selectionAnchor = null;
            Render(isFinal: true);

            var width = GetWidth();
            var contentLength = _prompt.Text.Length + _text.Length;
            var output = new StringBuilder();

            if (marker != null)
            {
                output.Append(ShellTheme.Ghost).Append(marker).Append(ShellTheme.Reset);
            }

            // When the content fills the last row exactly the cursor is already at the start of a new row.
            if (marker != null || contentLength == 0 || contentLength % width != 0)
            {
                output.Append("\r\n");
            }

            _console.Write(output.ToString());
            _renderedCaretRow = 0;
        }

        /// <summary>
        /// Redraws the prompt, the highlighted input, the inline suggestion and the completion menu.
        /// All cursor movement is relative to the prompt row, so the frame stays correct when the terminal scrolls.
        /// </summary>
        private void Render(bool isFinal = false)
        {
            var width = GetWidth();
            var output = new StringBuilder();

            output.Append("\u001b[?25l");
            output.Append('\r');
            if (_renderedCaretRow > 0)
            {
                output.Append("\u001b[").Append(_renderedCaretRow).Append('A');
            }
            output.Append("\u001b[J");

            output.Append(_prompt.Ansi).Append(ShellTheme.Reset);
            AppendHighlightedText(output, isFinal);

            var suggestion = isFinal ? null : GetSuggestion();
            var ghostText = suggestion?[_text.Length..] ?? string.Empty;
            output.Append(ShellTheme.Ghost).Append(ghostText).Append(ShellTheme.Reset);

            var contentLength = _prompt.Text.Length + _text.Length + ghostText.Length;
            if (contentLength > 0 && contentLength % width == 0)
            {
                // Move off the pending-wrap column so every terminal agrees where the cursor is.
                output.Append(" \b");
            }

            var cursorRow = contentLength / width;
            if (!isFinal && _completion != null)
            {
                var menuColumn = (_prompt.Text.Length + Math.Min(_completion.ReplaceStart, _text.Length)) % width;
                cursorRow += AppendMenu(output, width, menuColumn);
            }

            var caretPosition = _prompt.Text.Length + (isFinal ? _text.Length : _caret);
            var caretRow = caretPosition / width;
            var caretColumn = caretPosition % width;

            if (cursorRow > caretRow)
            {
                output.Append("\u001b[").Append(cursorRow - caretRow).Append('A');
            }

            output.Append('\r');
            if (caretColumn > 0)
            {
                output.Append("\u001b[").Append(caretColumn).Append('C');
            }

            output.Append("\u001b[?25h");
            _renderedCaretRow = caretRow;
            _console.Write(output.ToString());
        }

        private void AppendHighlightedText(StringBuilder output, bool isFinal)
        {
            if (_text.Length == 0)
            {
                return;
            }

            var styles = new string[_text.Length];
            Array.Fill(styles, string.Empty);

            try
            {
                foreach (var classified in _classify(_text))
                {
                    var style = ShellTheme.For(classified.Kind, classified.Token.IsQuoted);
                    for (var index = classified.Token.Start; index < Math.Min(classified.Token.End, _text.Length); index++)
                    {
                        styles[index] = style;
                    }
                }
            }
            catch
            {
                // Highlighting is cosmetic; fall back to plain text.
            }

            if (!isFinal && HasSelection)
            {
                for (var index = SelectionStart; index < SelectionEnd; index++)
                {
                    styles[index] += ShellTheme.Selection;
                }
            }

            string? currentStyle = null;
            for (var index = 0; index < _text.Length; index++)
            {
                if (styles[index] != currentStyle)
                {
                    currentStyle = styles[index];
                    output.Append(ShellTheme.Reset).Append(currentStyle);
                }

                output.Append(_text[index]);
            }

            output.Append(ShellTheme.Reset);
        }

        private int AppendMenu(StringBuilder output, int width, int preferredColumn)
        {
            var items = _completion!.Items;
            var visibleCount = Math.Min(MaxVisibleMenuItems, items.Count);
            var maxRowWidth = width - 1;

            var labelWidth = Math.Min(items.Max(i => i.Label.Length), Math.Max(10, maxRowWidth / 2));
            var descriptionWidth = Math.Min(items.Max(i => i.Description?.Length ?? 0), 60);
            descriptionWidth = Math.Max(0, Math.Min(descriptionWidth, maxRowWidth - labelWidth - 5));
            var rowWidth = labelWidth + 2 + (descriptionWidth > 0 ? descriptionWidth + 2 : 0);
            if (rowWidth > maxRowWidth)
            {
                labelWidth = Math.Max(1, maxRowWidth - 2);
                descriptionWidth = 0;
                rowWidth = labelWidth + 2;
            }

            var column = Math.Min(preferredColumn, Math.Max(0, maxRowWidth - rowWidth));
            var lines = 0;

            for (var index = _menuScroll; index < _menuScroll + visibleCount; index++)
            {
                var item = items[index];
                var isSelected = index == _menuIndex;

                StartMenuLine(output, column);
                output.Append(isSelected ? ShellTheme.MenuSelected : ShellTheme.MenuItem)
                    .Append(' ')
                    .Append(Fit(item.Label, labelWidth))
                    .Append(' ');

                if (descriptionWidth > 0)
                {
                    output.Append(isSelected ? string.Empty : ShellTheme.MenuDescription)
                        .Append(Fit(item.Description ?? string.Empty, descriptionWidth))
                        .Append("  ");
                }

                output.Append(ShellTheme.Reset);
                lines++;
            }

            // Extended documentation for the selected item when it does not fit in the menu row.
            var selectedDescription = items[_menuIndex].Description ?? string.Empty;
            if (selectedDescription.Length > descriptionWidth && maxRowWidth - column > 10)
            {
                StartMenuLine(output, column);
                output.Append(ShellTheme.MenuDescription)
                    .Append(' ')
                    .Append(Fit(selectedDescription, maxRowWidth - column - 1).TrimEnd())
                    .Append(ShellTheme.Reset);
                lines++;
            }

            if (items.Count > visibleCount)
            {
                StartMenuLine(output, column);
                output.Append(ShellTheme.MenuFooter)
                    .Append($" {_menuIndex + 1}/{items.Count}")
                    .Append(ShellTheme.Reset);
                lines++;
            }

            return lines;
        }

        private static void StartMenuLine(StringBuilder output, int column)
        {
            output.Append("\r\n");
            if (column > 0)
            {
                output.Append("\u001b[").Append(column).Append('C');
            }
        }

        private static string Fit(string text, int width)
        {
            if (width <= 0)
            {
                return string.Empty;
            }

            if (text.Length <= width)
            {
                return text.PadRight(width);
            }

            return width <= 3 ? text[..width] : text[..(width - 3)] + "...";
        }

        private int GetWidth() => Math.Max(20, _console.WindowWidth);

        #endregion
    }
}
