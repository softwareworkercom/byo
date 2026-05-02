using Spectre.Console;
using Spectre.Console.Rendering;
using System.Data;
using System.Globalization;
using System.Text;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class UserInterfaceService
    {
        private static readonly object _singleLineStatusLock = new();
        private static int _singleLineStatusLength;

        public static string? SelectPath()
        {
            var folderPath = AnsiConsole.Ask<string>("Please enter the folder location:");
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                ShowError("Invalid folder location.");
                return null;
            }
            return folderPath;
        }

        /// <summary>
        /// Interactively selects a folder from the file system with cross-platform support.
        /// Allows navigation through directories using arrow keys and selection with Enter.
        /// </summary>
        /// <param name="startingPath">Optional starting directory path. Defaults to current directory if not specified or invalid.</param>
        /// <param name="title">Optional title to display. Defaults to "Select a folder".</param>
        /// <returns>The selected folder path, or null if cancelled</returns>
        public static string? SelectFolder(string? startingPath = null, string? title = null)
        {
            // Determine starting path
            var currentPath = startingPath;
            if (string.IsNullOrWhiteSpace(currentPath) || !Directory.Exists(currentPath))
            {
                currentPath = Directory.GetCurrentDirectory();
            }

            while (true)
            {
                try
                {
                    AnsiConsole.Clear();

                    // Display title and current path
                    var displayTitle = string.IsNullOrWhiteSpace(title) ? "Select a folder" : title;
                    ShowRule(displayTitle);
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[cyan]Current location:[/] {Markup.Escape(currentPath)}");
                    AnsiConsole.WriteLine();

                    // Get directories and build selection list
                    var choices = new List<string>();

                    // Add parent directory option (unless at root)
                    var parentDir = Directory.GetParent(currentPath);
                    if (parentDir != null)
                    {
                        choices.Add(".. (Parent Directory)");
                    }

                    // Add current directory selection option
                    choices.Add(". (Select Current Folder)");

                    // Add subdirectories
                    try
                    {
                        var subdirectories = Directory.GetDirectories(currentPath)
                            .Select(d => new DirectoryInfo(d).Name)
                            .OrderBy(d => d)
                            .ToList();

                        choices.AddRange(subdirectories);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ShowWarning("Access denied to some directories in this location.");
                    }

                    // Add manual entry and cancel options
                    choices.Add("--- Enter path manually ---");
                    choices.Add("--- Cancel ---");

                    // Show selection prompt
                    var selection = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[cyan]Choose a folder or navigate:[/]")
                            .PageSize(20)
                            .MoreChoicesText("[grey](Move up and down to see more options)[/]")
                            .UseConverter(Markup.Escape)
                            .AddChoices(choices)
                    );

                    // Handle selection
                    if (selection == ".. (Parent Directory)")
                    {
                        if (parentDir != null)
                        {
                            currentPath = parentDir.FullName;
                        }
                    }
                    else if (selection == ". (Select Current Folder)")
                    {
                        AnsiConsole.Clear();
                        return currentPath;
                    }
                    else if (selection == "--- Enter path manually ---")
                    {
                        AnsiConsole.Clear();
                        var manualPath = AnsiConsole.Ask<string>("[cyan]Enter folder path:[/]");
                        if (!string.IsNullOrWhiteSpace(manualPath) && Directory.Exists(manualPath))
                        {
                            return Path.GetFullPath(manualPath);
                        }
                        else
                        {
                            ShowError("Invalid folder path.");
                            WaitForEnter();
                        }
                    }
                    else if (selection == "--- Cancel ---")
                    {
                        AnsiConsole.Clear();
                        return null;
                    }
                    else
                    {
                        // Navigate into selected subdirectory
                        currentPath = Path.Combine(currentPath, selection);
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Error accessing directory: {ex.Message}");
                    WaitForEnter();

                    // Reset to current directory if there's an error
                    currentPath = Directory.GetCurrentDirectory();
                }
            }
        }

        public static string SelectTime()
        {
            var hour = AnsiConsole.Prompt(
                new TextPrompt<int>("[cyan]Enter hour (0-23):[/]")
                    .Validate(h => h >= 0 && h <= 23
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Hour must be between 0 and 23[/]"))
            );

            var minute = AnsiConsole.Prompt(
                new TextPrompt<int>("[cyan]Enter minute (0-59):[/]")
                    .Validate(m => m >= 0 && m <= 59
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Minute must be between 0 and 59[/]"))
            );

            return $"{hour:D2}:{minute:D2}";
        }

        public static DateTime SelectDate()
        {
            // Display calendar view
            var today = DateTime.Today;
            var currentMonth = today;
            var selectedDate = today;

            while (true)
            {
                AnsiConsole.Clear();

                // Display calendar for current month
                DisplayCalendar(currentMonth, selectedDate);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[cyan]Navigation:[/]");
                AnsiConsole.MarkupLine("  [grey]←/→[/] Previous/Next month");
                AnsiConsole.MarkupLine("  [grey]↑/↓[/] Previous/Next week");
                AnsiConsole.MarkupLine("  [grey]Enter[/] Select highlighted date");
                AnsiConsole.MarkupLine("  [grey]T[/] Jump to today");
                AnsiConsole.MarkupLine("  [grey]Q[/] Quit and enter date manually");
                AnsiConsole.WriteLine();

                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        currentMonth = currentMonth.AddMonths(-1);
                        selectedDate = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                        break;
                    case ConsoleKey.DownArrow:
                        currentMonth = currentMonth.AddMonths(1);
                        selectedDate = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                        break;
                    case ConsoleKey.LeftArrow:
                        selectedDate = selectedDate.AddDays(-1);
                        currentMonth = selectedDate;
                        break;
                    case ConsoleKey.RightArrow:
                        selectedDate = selectedDate.AddDays(1);
                        currentMonth = selectedDate;
                        break;
                    case ConsoleKey.Enter:
                        AnsiConsole.Clear();
                        return selectedDate;
                    case ConsoleKey.T:
                        selectedDate = today;
                        currentMonth = today;
                        break;
                }
            }
        }

        private static void DisplayCalendar(DateTime month, DateTime selectedDate)
        {
            var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

            // Display month and year header
            var header = month.ToString("MMMM yyyy");
            AnsiConsole.MarkupLine($"[bold cyan]{header.PadLeft(20)}[/]");
            AnsiConsole.WriteLine();

            // Create calendar table
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan)
                .HideHeaders();

            // Add day headers
            table.AddColumn("Sun");
            table.AddColumn("Mon");
            table.AddColumn("Tue");
            table.AddColumn("Wed");
            table.AddColumn("Thu");
            table.AddColumn("Fri");
            table.AddColumn("Sat");

            // Add day name row
            table.AddRow("[bold]Sun[/]", "[bold]Mon[/]", "[bold]Tue[/]", "[bold]Wed[/]", "[bold]Thu[/]", "[bold]Fri[/]", "[bold]Sat[/]");

            var currentDay = 1;
            var daysInMonth = lastDayOfMonth.Day;

            // Generate calendar rows
            while (currentDay <= daysInMonth)
            {
                var week = new string[7];

                for (int i = 0; i < 7; i++)
                {
                    if (currentDay == 1 && i < startDayOfWeek)
                    {
                        week[i] = "  ";
                    }
                    else if (currentDay <= daysInMonth)
                    {
                        var currentDate = new DateTime(month.Year, month.Month, currentDay);
                        var dayStr = currentDay.ToString().PadLeft(2);

                        if (currentDate.Date == selectedDate.Date)
                        {
                            week[i] = $"[black on cyan]{dayStr}[/]"; // Highlighted selected date
                        }
                        else if (currentDate.Date == DateTime.Today)
                        {
                            week[i] = $"[green]{dayStr}[/]"; // Today in green
                        }
                        else
                        {
                            week[i] = $"[grey]{dayStr}[/]";
                        }

                        currentDay++;
                    }
                    else
                    {
                        week[i] = "  ";
                    }
                }

                table.AddRow(week);
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]Selected:[/] {selectedDate:yyyy-MM-dd}");
        }

        public static string SelectSingleItem(string title, List<string> items)
        {
            var escapedTitle = Markup.Escape(title);

            var selectedItem = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[cyan]Select {escapedTitle}:[/]")
                    .PageSize(20)
                    .MoreChoicesText($"[grey](Move up and down to reveal more {escapedTitle})[/]")
                    .UseConverter(Markup.Escape)
                    .AddChoices(items)
            );

            return selectedItem;
        }

        public static List<string> SelectMultipleItems(string title, List<string> items)
        {
            var escapedTitle = Markup.Escape(title);

            var selectedItems = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title($"[cyan]Select {escapedTitle} (use space to select, enter to confirm):[/]")
                    .PageSize(20)
                    .MoreChoicesText($"[grey](Move up and down to reveal more {escapedTitle})[/]")
                    .NotRequired() // Allow no selection
                    .UseConverter(Markup.Escape)
                    .AddChoices(items)
            );

            if (selectedItems.Count.Equals(0))
            {
                selectedItems = items;
            }

            return selectedItems;
        }

        public static bool SelectBoolean(string title)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<bool>()
                    .Title($"[cyan]{Markup.Escape(title)}[/]")
                    .AddChoices(new[] { true, false })
            );

            return choice;
        }

        public static DateTime SelectDateTimeRangeFromNow()
        {
            var ranges = new Dictionary<string, string>
            {
                { "custom", "Custom Date Range" },
                { "15m", "Past 15 Minutes" },
                { "1h", "Past 1 Hour" },
                { "4h", "Past 4 Hours" },
                { "1d", "Past 1 Day" },
                { "2d", "Past 2 Days" },
                { "3d", "Past 3 Days" },
                { "1w", "Past 7 Days" },
                { "15d", "Past 15 Days" },
                { "1mo", "Past 1 Month" },
                { "3mo", "Past 3 Months" },
                { "6mo", "Past 6 Months" },
                { "9mo", "Past 9 Months" },
                { "12mo", "Past 12 Months" }
            };

            var selectedRange = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select time range:[/]")
                    .PageSize(11)
                    .UseConverter(Markup.Escape)
                    .AddChoices(ranges.Values)
            );

            var endDate = DateTime.Now;
            var startDate = endDate;

            // Find the key for the selected value
            var rangeKey = ranges.FirstOrDefault(x => x.Value == selectedRange).Key;

            if (rangeKey == "custom")
            {
                // Custom date range selection
                ShowWarning("Select start date:");
                var startDateString = SelectDate();
                var startTimeString = SelectTime();

                var isoDateTime = $"{startDateString:yyyy-MM-dd}T{startTimeString}:00";
                startDate = DateTime.ParseExact(isoDateTime, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            }
            else
            {
                startDate = rangeKey switch
                {
                    "15m" => endDate.AddMinutes(-15),
                    "1h" => endDate.AddHours(-1),
                    "4h" => endDate.AddHours(-4),
                    "1d" => endDate.AddDays(-1),
                    "2d" => endDate.AddDays(-2),
                    "3d" => endDate.AddDays(-3),
                    "1w" => endDate.AddDays(-7),
                    "15d" => endDate.AddDays(-15),
                    "1mo" => endDate.AddMonths(-1),
                    "3mo" => endDate.AddMonths(-3),
                    "6mo" => endDate.AddMonths(-6),
                    "9mo" => endDate.AddMonths(-9),
                    "12mo" => endDate.AddMonths(-12),
                    _ => endDate
                };
            }

            return startDate;
        }

        public static TEnum SelectEnum<TEnum>(string? title = null) where TEnum : struct, Enum
        {
            var enumValues = Enum.GetValues<TEnum>();
            var enumNames = enumValues.Select(e => e.ToString()).ToList();

            var promptTitle = string.IsNullOrEmpty(title)
                ? $"[cyan]Select {Markup.Escape(typeof(TEnum).Name)}:[/]"
                : $"[cyan]{Markup.Escape(title)}:[/]";

            var selectedName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(promptTitle)
                    .PageSize(20)
                    .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                    .UseConverter(Markup.Escape)
                    .AddChoices(enumNames)
            );

            return Enum.Parse<TEnum>(selectedName);
        }

        /// <summary>
        /// Executes an async action for each item in a collection with a progress bar.
        /// </summary>
        /// <typeparam name="TItem">The type of items to process</typeparam>
        /// <typeparam name="TResult">The type of result returned by the action</typeparam>
        /// <param name="items">The collection of items to process</param>
        /// <param name="action">The async action to execute for each item</param>
        /// <param name="description">The description displayed in the progress bar</param>
        /// <returns>A dictionary mapping items to their results</returns>
        public static async Task<Dictionary<TItem, TResult>> ExecuteWithProgressAsync<TItem, TResult>(
            IEnumerable<TItem> items,
            Func<TItem, Task<TResult>> action,
            string description = "[green]Processing[/]")
        {
            var itemsList = items.ToList();
            var results = new Dictionary<TItem, TResult>();

            await AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask(description, maxValue: itemsList.Count);

                    foreach (var item in itemsList)
                    {
                        try
                        {
                            var result = await action(item);
                            results[item] = result;
                        }
                        catch (Exception ex)
                        {
                            ShowError($"Error processing {item}: {ex.Message}");
                            // Store default value for failed items
                            results[item] = default;
                        }
                        task.Increment(1);
                    }
                });

            return results;
        }

        public static (DateTime FromDate, DateTime ToDate) SelectTimeframe()
        {
            var ranges = new Dictionary<string, string>
            {
                { "1w", "Past 7 Days" },
                { "2w", "Past 2 Weeks" },
                { "1mo", "Past 1 Month" },
                { "3mo", "Past 3 Months" },
                { "6mo", "Past 6 Months" },
                { "1y", "Past 1 Year" },
                { "custom", "Custom Date Range" }
            };

            var selectedRange = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select timeframe:[/]")
                    .PageSize(10)
                    .UseConverter(Markup.Escape)
                    .AddChoices(ranges.Values)
            );

            var toDate = DateTime.Now;
            var fromDate = toDate;

            var rangeKey = ranges.FirstOrDefault(x => x.Value == selectedRange).Key;

            if (rangeKey == "custom")
            {
                ShowWarning("Select start date:");
                fromDate = SelectDate();
                ShowWarning("Select end date:");
                toDate = SelectDate();
            }
            else
            {
                fromDate = rangeKey switch
                {
                    "1w" => toDate.AddDays(-7),
                    "2w" => toDate.AddDays(-14),
                    "1mo" => toDate.AddMonths(-1),
                    "3mo" => toDate.AddMonths(-3),
                    "6mo" => toDate.AddMonths(-6),
                    "1y" => toDate.AddYears(-1),
                    _ => toDate.AddDays(-30)
                };
            }

            return (fromDate, toDate);
        }

        public static string TruncateWithEllipsis(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Remove line breaks and replace with spaces
            var cleanedText = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            if (cleanedText.Length <= maxLength)
                return cleanedText;

            if (maxLength <= 3)
                return cleanedText.Substring(0, Math.Max(0, maxLength));

            return cleanedText.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// Shows a message in red color in the console.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void ShowRed(string message)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
        }

        /// <summary>
        /// Shows an error message in red color in the console.
        /// </summary>
        /// <param name="message">The error message to display</param>
        public static void ShowError(string message)
        {
            ShowRed(message);
        }

        /// <summary>
        /// Shows a message in yellow color in the console.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void ShowYellow(string message)
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");
        }

        /// <summary>
        /// Shows a warning message in yellow color in the console.
        /// </summary>
        /// <param name="message">The warning message to display</param>
        public static void ShowWarning(string message)
        {
            ShowYellow(message);
        }

        /// <summary>
        /// Shows a message in blue color in the console.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void ShowBlue(string message)
        {
            AnsiConsole.MarkupLine($"[blue]{Markup.Escape(message)}[/]");
        }

        /// <summary>
        /// Shows an informational message in blue color in the console.
        /// </summary>
        /// <param name="message">The informational message to display</param>
        public static void ShowInformation(string message)
        {
            ShowBlue(message);
        }

        public static void ShowComment(string message)
        {
            ShowGrey(message);
        }

        public static void ShowTable(Table table)
        {
            AnsiConsole.Write(table);
        }

        public static void ShowTable(DataTable dataTable)
        {
            var table = ConvertDataTableToTable(dataTable);
            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Shows a message in green color in the console.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void ShowGreen(string message)
        {
            AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");
        }

        /// <summary>
        /// Shows a success message in green color in the console.
        /// </summary>
        /// <param name="message">The success message to display</param>
        public static void ShowSuccess(string message)
        {
            ShowGreen(message);
        }

        /// <summary>
        /// Shows a message in cyan color in the console.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void ShowCyan(string message)
        {
            AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(message)}[/]");
        }

        /// <summary>
        /// Shows a message in grey color in the console.
        /// </summary>
        /// <param name="message">The message to display</param>
        public static void ShowGrey(string message)
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
        }

        /// <summary>
        /// Shows a message in the specified color.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="color">The color to use</param>
        public static void ShowColoredMessage(string message, string color)
        {
            AnsiConsole.MarkupLine($"[{color}]{Markup.Escape(message)}[/]");
        }

        /// <summary>
        /// Prompts the user with a Yes/No question.
        /// </summary>
        /// <param name="question">The question to ask</param>
        /// <returns>True if Yes, False if No</returns>
        public static bool AskYesNo(string question)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[cyan]{Markup.Escape(question)}[/]")
                    .AddChoices(new[] { "Yes", "No" })
            );

            return choice == "Yes";
        }

        /// <summary>
        /// Prompts the user for text input.
        /// </summary>
        /// <param name="prompt">The prompt message</param>
        /// <param name="allowEmpty">Whether to allow empty input</param>
        /// <returns>The user's input</returns>
        public static string AskInput(string prompt, bool allowEmpty = false)
        {
            var textPrompt = new TextPrompt<string>($"[cyan]{Markup.Escape(prompt)}:[/]");

            if (!allowEmpty)
            {
                textPrompt.Validate(input =>
                    !string.IsNullOrWhiteSpace(input)
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Input cannot be empty[/]")
                );
            }
            else
            {
                textPrompt.AllowEmpty();
            }

            return AnsiConsole.Prompt(textPrompt);
        }

        /// <summary>
        /// Prompts the user for text input with a pre-filled default value.
        /// </summary>
        /// <param name="prompt">The prompt message</param>
        /// <param name="defaultValue">The default value shown to the user; accepted as-is if Enter is pressed</param>
        /// <returns>The user's input, or the default value if unchanged</returns>
        public static string AskInput(string prompt, string defaultValue)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>($"[cyan]{Markup.Escape(prompt)}:[/]")
                    .DefaultValue(defaultValue)
            );
        }

        /// <summary>
        /// Prompts the user for secret input (masked).
        /// </summary>
        /// <param name="prompt">The prompt message</param>
        /// <returns>The user's secret input</returns>
        public static string AskSecret(string prompt)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>($"[cyan]{Markup.Escape(prompt)}:[/]")
                    .Secret()
                    .Validate(input =>
                        !string.IsNullOrWhiteSpace(input)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Input cannot be empty[/]")
                    )
            );
        }

        /// <summary>
        /// Prompts the user to press Enter to continue.
        /// </summary>
        /// <param name="message">Optional message to display</param>
        public static void WaitForEnter(string message = "Press Enter to continue...")
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
            Console.ReadLine();
        }

        /// <summary>
        /// Prompts the user to select a month from the last N months.
        /// </summary>
        /// <param name="monthsBack">Number of months to show (default: 12)</param>
        /// <returns>The selected DateTime representing the first day of the selected month</returns>
        public static DateTime SelectMonth(int monthsBack = 12)
        {
            var months = new List<DateTime>();
            var now = DateTime.Now;

            for (int i = 0; i < monthsBack; i++)
            {
                months.Add(now.AddMonths(-i));
            }

            var selectedMonth = AnsiConsole.Prompt(
                new SelectionPrompt<DateTime>()
                    .Title("[cyan]Select a month:[/]")
                    .PageSize(monthsBack)
                    .UseConverter(d => d.ToString("MMMM yyyy"))
                    .AddChoices(months)
            );

            return selectedMonth;
        }

        /// <summary>
        /// Prompts the user to select a single item from a collection with a custom display converter.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="title">The title for the selection prompt</param>
        /// <param name="items">The collection of items to choose from</param>
        /// <param name="converter">Function to convert item to display string</param>
        /// <param name="pageSize">Number of items to display per page (default: 15)</param>
        /// <returns>The selected item</returns>
        public static T SelectSingleItem<T>(string title, IEnumerable<T> items, Func<T, string> converter, int pageSize = 15) where T : notnull
        {
            var selectedItem = AnsiConsole.Prompt(
                new SelectionPrompt<T>()
                    .Title($"[cyan]{Markup.Escape(title)}:[/]")
                    .PageSize(pageSize)
                    .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                    .UseConverter(converter)
                    .AddChoices(items)
            );

            return selectedItem;
        }

        /// <summary>
        /// Writes a raw markup string to the console. Use this for complex formatting
        /// with multiple colors or styles that cannot be expressed with the simpler Show methods.
        /// </summary>
        /// <param name="markup">The Spectre.Console markup string to display</param>
        public static void ShowMarkup(string markup)
        {
            AnsiConsole.MarkupLine(markup);
        }

        /// <summary>
        /// Displays a horizontal rule with an optional title.
        /// </summary>
        /// <param name="title">The title to display in the rule</param>
        public static void ShowRule(string title)
        {
            AnsiConsole.Write(new Spectre.Console.Rule(title).RuleStyle("grey"));
        }

        /// <summary>
        /// Displays a Panel renderable object.
        /// </summary>
        /// <param name="panel">The Panel to render</param>
        public static void ShowPanel(Panel panel)
        {
            AnsiConsole.Write(panel);
        }

        /// <summary>
        /// Displays any Spectre.Console renderable object.
        /// </summary>
        /// <param name="renderable">The renderable to write to the console</param>
        public static void ShowRenderable(IRenderable renderable)
        {
            AnsiConsole.Write(renderable);
        }

        /// <summary>
        /// Writes an empty line to the console.
        /// </summary>
        public static void WriteLine()
        {
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Writes a plain text line to the console.
        /// </summary>
        /// <param name="text">The text to display</param>
        public static void WriteLine(string text)
        {
            AnsiConsole.WriteLine(text);
        }

        /// <summary>
        /// Updates a status message in-place on the current console line.
        /// </summary>
        /// <param name="text">The status text to display</param>
        public static void UpdateSingleLineStatus(string text)
        {
            lock (_singleLineStatusLock)
            {
                var statusText = text ?? string.Empty;
                var paddedStatusText = statusText.PadRight(_singleLineStatusLength);
                Console.Write($"\r{paddedStatusText}");
                _singleLineStatusLength = statusText.Length;
            }
        }

        /// <summary>
        /// Completes the current single-line status output by moving to the next line.
        /// </summary>
        public static void CompleteSingleLineStatus()
        {
            lock (_singleLineStatusLock)
            {
                if (_singleLineStatusLength == 0)
                {
                    return;
                }

                Console.WriteLine();
                _singleLineStatusLength = 0;
            }
        }

        /// <summary>
        /// Prompts the user for confirmation with a default value.
        /// </summary>
        /// <param name="question">The question to display</param>
        /// <param name="defaultValue">The default answer (true for Yes, false for No)</param>
        /// <returns>True if the user confirms, false otherwise</returns>
        public static bool Confirm(string question, bool defaultValue = true)
        {
            return AnsiConsole.Confirm(question, defaultValue);
        }

        /// <summary>
        /// Prompts the user for typed input.
        /// </summary>
        /// <typeparam name="T">The type of value to ask for</typeparam>
        /// <param name="prompt">The prompt message</param>
        /// <returns>The user's input converted to the specified type</returns>
        public static T Ask<T>(string prompt) where T : notnull
        {
            return AnsiConsole.Ask<T>(prompt);
        }

        /// <summary>
        /// Prompts the user for typed input with a default value.
        /// </summary>
        /// <typeparam name="T">The type of value to ask for</typeparam>
        /// <param name="prompt">The prompt message</param>
        /// <param name="defaultValue">The default value if user provides no input</param>
        /// <returns>The user's input converted to the specified type</returns>
        public static T Ask<T>(string prompt, T defaultValue) where T : notnull
        {
            return AnsiConsole.Ask(prompt, defaultValue);
        }

        /// <summary>
        /// Prompts the user using a Spectre.Console prompt object.
        /// </summary>
        /// <typeparam name="T">The type of value returned by the prompt</typeparam>
        /// <param name="prompt">The prompt to display</param>
        /// <returns>The user's response</returns>
        public static T Prompt<T>(IPrompt<T> prompt)
        {
            return AnsiConsole.Prompt(prompt);
        }

        /// <summary>
        /// Executes an async action with a status spinner.
        /// </summary>
        /// <param name="status">The status message to display</param>
        /// <param name="action">The async action to execute</param>
        public static async Task ExecuteWithStatusAsync(string status, Func<StatusContext, Task> action)
        {
            await AnsiConsole.Status()
                .StartAsync(status, action);
        }

        /// <summary>
        /// Executes an async action with a progress display.
        /// </summary>
        /// <param name="action">The async action to execute with a ProgressContext</param>
        public static async Task ExecuteWithProgressAsync(Func<ProgressContext, Task> action)
        {
            await AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                })
                .StartAsync(action);
        }

        /// <summary>
        /// Executes a synchronous action with a progress display.
        /// </summary>
        /// <param name="action">The action to execute with a ProgressContext</param>
        public static void ExecuteWithProgress(Action<ProgressContext> action)
        {
            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                })
                .Start(action);
        }

        /// <summary>
        /// Clears the console.
        /// </summary>
        public static void Clear()
        {
            AnsiConsole.Clear();
        }

        /// <summary>
        /// Gets the current console width.
        /// </summary>
        /// <returns>The width of the console in characters</returns>
        public static int GetConsoleWidth()
        {
            return AnsiConsole.Profile.Width;
        }

        /// <summary>
        /// Converts a DataTable to a Spectre.Console Table with auto-adjusted column widths
        /// based on actual content width.
        /// </summary>
        /// <param name="dataTable">The DataTable to convert</param>
        /// <returns>A Spectre.Console Table ready for display</returns>
        private static Table ConvertDataTableToTable(DataTable dataTable)
        {
            var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).ShowRowSeparators();
            var consoleWidth = GetConsoleWidth();
            if (consoleWidth <= 0)
            {
                consoleWidth = 120;
            }

            var columnCount = Math.Max(1, dataTable.Columns.Count);
            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var columnHeaders = new string[dataTable.Columns.Count];
            var contentWidths = new int[dataTable.Columns.Count];

            for (int columnIndex = 0; columnIndex < dataTable.Columns.Count; columnIndex++)
            {
                var column = dataTable.Columns[columnIndex];
                var baseName = string.IsNullOrWhiteSpace(column.ColumnName)
                    ? $"Column {columnIndex + 1}"
                    : column.ColumnName;
                baseName = SanitizeTableText(baseName);
                while (!columnNames.Add(baseName))
                {
                    baseName = $"{baseName} (duplicate)";
                }
                columnHeaders[columnIndex] = baseName;
                contentWidths[columnIndex] = baseName.Length;
            }

            foreach (DataRow row in dataTable.Rows)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var value = row[i]?.ToString();
                    value = SanitizeTableText(value);
                    contentWidths[i] = Math.Max(contentWidths[i], value.Length);
                }
            }

            for (int columnIndex = 0; columnIndex < dataTable.Columns.Count; columnIndex++)
            {
                var headerText = columnHeaders[columnIndex];
                table.AddColumn(new TableColumn(Markup.Escape(headerText)));
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var cells = new string[dataTable.Columns.Count];
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var value = row[i]?.ToString();
                    value = SanitizeTableText(value);
                    var displayValue = value;
                    cells[i] = Markup.Escape(displayValue);
                }
                table.AddRow(cells);
            }

            return table;
        }



        private static string SanitizeTableText(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (char.IsControl(character))
                {
                    builder.Append(' ');
                    continue;
                }

                builder.Append(character);
            }

            return builder.ToString();
        }

    }
}
