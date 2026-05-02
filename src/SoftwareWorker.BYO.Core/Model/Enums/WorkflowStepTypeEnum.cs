namespace SoftwareWorker.BYO.Core.Model.Enums
{
    /// <summary>
    /// Types of steps that can be included in a workflow
    /// </summary>
    public enum WorkflowStepTypeEnum
    {
        /// <summary>
        /// Display a message to the user
        /// </summary>
        Message,

        /// <summary>
        /// Prompt user for Yes/No confirmation
        /// </summary>
        YesNoQuestion,

        /// <summary>
        /// Prompt user for text input to be stored as a setting
        /// </summary>
        InputAsSetting,

        /// <summary>
        /// Prompt user for text input to be stored as a secret
        /// </summary>
        InputAsSecret,

        /// <summary>
        /// Execute a saved command
        /// </summary>
        ExecuteCommand
    }
}
