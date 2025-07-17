namespace WorkflowEngine.Models;

public record Action(
    string Id,
    string Name,
    List<string> FromStates,
    string ToState,
    bool Enabled = true 
);