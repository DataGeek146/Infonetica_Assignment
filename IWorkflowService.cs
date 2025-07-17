using WorkflowEngine.Models;
using Microsoft.AspNetCore.Mvc;

namespace WorkflowEngine.Services;

public interface IWorkflowService
{
    Result<WorkflowDefinition> CreateDefinition(WorkflowDefinition definition);
    Result<WorkflowDefinition> GetDefinition(string definitionId);
    IEnumerable<WorkflowDefinition> GetAllDefinitions();

    Result<WorkflowInstance> StartInstance(string definitionId);
    Result<WorkflowInstance> GetInstance(Guid instanceId);
    IEnumerable<WorkflowInstance> GetAllInstances();
    Result<WorkflowInstance> ExecuteAction(Guid instanceId, string actionId);
}
public record Result<T>(T? Value, string? ErrorMessage = null)
{
    public bool IsSuccess => ErrorMessage == null;
    public bool IsFailure => !IsSuccess;
}