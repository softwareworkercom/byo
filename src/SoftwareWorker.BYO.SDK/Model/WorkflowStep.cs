using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.CLI.Core.Model
{
    /// <summary>
    /// Represents a single step in a workflow
    /// </summary>
    public class WorkflowStep
    {
        /// <summary>
        /// Type of the workflow step
        /// </summary>
        public WorkflowStepTypeEnum StepType { get; set; }

        /// <summary>
        /// Message to display or question to ask (used for Message, YesNoQuestion, InputAsSetting, InputAsSecret)
        /// </summary>
        public string? Prompt { get; set; }

        /// <summary>
        /// Color for the message (used for Message type)
        /// </summary>
        public MessageColorEnum Color { get; set; } = MessageColorEnum.Default;

        /// <summary>
        /// Whether to wait for the user to press Enter before continuing (used for Message type)
        /// </summary>
        public bool WaitForEnter { get; set; } = false;

        /// <summary>
        /// Whether to interrupt the workflow flow if the answer is No (used for YesNoQuestion)
        /// </summary>
        public bool InterruptOnNo { get; set; } = false;

        /// <summary>
        /// Key name for storing the input value (used for InputAsSetting, InputAsSecret)
        /// </summary>
        public string? StorageKey { get; set; }

        /// <summary>
        /// Name of the saved command to execute (used for ExecuteCommand). Leave null/empty to use an inline custom command instead.
        /// </summary>
        public string? CommandName { get; set; }

        /// <summary>
        /// Inline command executable to run without a saved command (used for ExecuteCommand when CommandName is not set)
        /// </summary>
        public string? CommandExecutable { get; set; }

        /// <summary>
        /// Working directory for the inline command (used for ExecuteCommand)
        /// </summary>
        public string? CommandDirectory { get; set; }

        /// <summary>
        /// Shell to use for the inline command (used for ExecuteCommand)
        /// </summary>
        public ShellTypeEnum? CommandShell { get; set; }

        /// <summary>
        /// Whether command execution should run asynchronously in the background (used for ExecuteCommand)
        /// </summary>
        public bool RunAsync { get; set; }

    }
}
