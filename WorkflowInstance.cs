namespace WorkflowEngine.Models;

public record HistoryEntry(string ActionId, string StateId, DateTime Timestamp);

public class WorkflowInstance
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string WorkflowDefinitionId { get; init; } = string.Empty;
    public string CurrentStateId { get; set; } = string.Empty;
    public List<HistoryEntry> History { get; set; } = new();
}