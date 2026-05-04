using SoftwareWorker.BYO.CLI.Core.Engine;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;

namespace SoftwareWorker.BYO.Tests;

public sealed class WorkflowServiceTests : IDisposable
{
    private readonly string _originalWorkflowsFilePath;
    private readonly string _testStorageDirectory;

    public WorkflowServiceTests()
    {
        _originalWorkflowsFilePath = WorkflowService.WorkflowsFilePath;
        _testStorageDirectory = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDirectory);

        WorkflowService.WorkflowsFilePath = Path.Combine(_testStorageDirectory, "workflows.json");
    }

    [Fact]
    public void Create_AndGetList_ShouldPersistWorkflowWithNormalizedBookmark()
    {
        var created = WorkflowService.Create(
            name: "Deploy.Workflow",
            steps: [CreateMessageStep("run")],
            bookmark: " /DevOps/Deploy/ ");

        var workflows = WorkflowService.GetList();

        Assert.Single(workflows);
        Assert.Equal("Deploy.Workflow", created.Name);
        Assert.Equal("DevOps/Deploy", created.Bookmark);
        Assert.Single(created.Steps);
        Assert.Equal("Deploy.Workflow", workflows[0].Name);
    }

    [Fact]
    public void Create_ShouldThrow_WhenWorkflowNameAlreadyExists()
    {
        WorkflowService.Create("Duplicate.Workflow", [CreateMessageStep("first")], bookmark: "Ops");

        Assert.Throws<InvalidOperationException>(() =>
            WorkflowService.Create("Duplicate.Workflow", [CreateMessageStep("second")], bookmark: "Ops"));
    }

    [Fact]
    public void Update_ShouldChangeOnlyProvidedFields_AndSetUpdatedAt()
    {
        WorkflowService.Create("Build.Workflow", [CreateMessageStep("old")], bookmark: "Old");

        var updated = WorkflowService.Update(
            name: "Build.Workflow",
            newName: "Build.Workflow.New",
            description: "new",
            steps: [CreateMessageStep("new")],
            folderPath: "/Pipelines/CI/");

        Assert.NotNull(updated);
        Assert.Equal("Build.Workflow.New", updated.Name);
        Assert.Equal("Pipelines/CI", updated.Bookmark);
        Assert.Single(updated.Steps);
        Assert.Equal("new", updated.Steps[0].Prompt);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public void Update_ShouldReturnNull_WhenWorkflowDoesNotExist()
    {
        var updated = WorkflowService.Update("Missing.Workflow", description: "x");

        Assert.Null(updated);
    }

    [Fact]
    public void Delete_ShouldRemoveWorkflowByName()
    {
        WorkflowService.Create("Delete.A", [CreateMessageStep("a")], bookmark: "Ops");
        WorkflowService.Create("Delete.B", [CreateMessageStep("b")], bookmark: "Ops");

        var deleted = WorkflowService.Delete("Delete.B");
        var workflows = WorkflowService.GetList();

        Assert.True(deleted);
        Assert.Single(workflows);
        Assert.Equal("Delete.A", workflows[0].Name);
    }

    [Fact]
    public void Delete_ShouldReturnFalse_WhenWorkflowDoesNotExist()
    {
        WorkflowService.Create("Delete.A", [CreateMessageStep("a")], bookmark: "Ops");

        var deleted = WorkflowService.Delete("Missing.Workflow");

        Assert.False(deleted);
        Assert.Single(WorkflowService.GetList());
    }

    public void Dispose()
    {
        WorkflowService.WorkflowsFilePath = _originalWorkflowsFilePath;

        if (Directory.Exists(_testStorageDirectory))
        {
            Directory.Delete(_testStorageDirectory, recursive: true);
        }
    }

    private static WorkflowStep CreateMessageStep(string prompt)
    {
        return new WorkflowStep
        {
            StepType = WorkflowStepTypeEnum.Message,
            Prompt = prompt,
            WaitForEnter = false
        };
    }
}

public sealed class WorkflowCliTests : IDisposable
{
    private readonly string _originalWorkflowsFilePath;
    private readonly string _testStorageDirectory;

    public WorkflowCliTests()
    {
        _originalWorkflowsFilePath = WorkflowService.WorkflowsFilePath;
        _testStorageDirectory = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDirectory);

        WorkflowService.WorkflowsFilePath = Path.Combine(_testStorageDirectory, "workflows.json");
    }

    [Fact]
    public void Route_Run_ShouldExecuteWorkflowWhenWorkflowExists()
    {
        var workflowName = $"workflow-run-{Guid.NewGuid():N}";
        WorkflowService.Create(
            workflowName,
            [CreateMessageStep("hello")],
            bookmark: "/DevOps/Deploy/");

        var exitCode = CommandsRouter.Route([
            "workflows",
            "run",
            "--name", workflowName,
            "--bookmark", "DevOps/Deploy"
        ]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Route_Run_ShouldNotFailWhenWorkflowIsMissing()
    {
        var exitCode = CommandsRouter.Route([
            "workflows",
            "run",
            "--name", "missing-workflow",
            "--bookmark", "DevOps/Deploy"
        ]);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Route_Run_ShouldNotFailWhenRequiredParameterIsMissing()
    {
        var exitCode = CommandsRouter.Route([
            "workflows",
            "run",
            "--name", "any-workflow"
        ]);

        Assert.Equal(0, exitCode);
    }

    public void Dispose()
    {
        WorkflowService.WorkflowsFilePath = _originalWorkflowsFilePath;

        if (Directory.Exists(_testStorageDirectory))
        {
            Directory.Delete(_testStorageDirectory, recursive: true);
        }
    }

    private static WorkflowStep CreateMessageStep(string prompt)
    {
        return new WorkflowStep
        {
            StepType = WorkflowStepTypeEnum.Message,
            Prompt = prompt,
            WaitForEnter = false
        };
    }
}
