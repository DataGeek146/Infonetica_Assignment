namespace WorkflowEngine.Models;

public record State(
    string Id,
    string Name,
    bool IsInitial,
    bool IsFinal,
    string? Description = null 
);