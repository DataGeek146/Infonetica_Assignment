using System.Collections.Concurrent;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

public class WorkflowService : IWorkflowService
{

    private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new();
    private readonly ConcurrentDictionary<Guid, WorkflowInstance> _instances = new();

    public Result<WorkflowDefinition> CreateDefinition(WorkflowDefinition definition)
    {
      
        if (string.IsNullOrWhiteSpace(definition.Name))
            return new(null, "Definition Name cannot be empty.");

        if (!definition.States.Any())
            return new(null, "Definition must have at least one state.");

        if (definition.States.Count(s => s.IsInitial) != 1)
            return new(null, "Definition must have exactly one initial state.");

        var stateIds = definition.States.Select(s => s.Id).ToHashSet();
        if (stateIds.Count != definition.States.Count)
            return new(null, "State IDs must be unique within a definition.");

        foreach (var action in definition.Actions)
        {
            if (!stateIds.Contains(action.ToState))
                return new(null, $"Action '{action.Id}' points to an unknown toState '{action.ToState}'.");
            foreach (var fromState in action.FromStates)
            {
                if (!stateIds.Contains(fromState))
                    return new(null, $"Action '{action.Id}' contains an unknown fromState '{fromState}'.");
            }
        }

        definition.Id = definition.Name.ToLower().Replace(" ", "-"); 
        if (!_definitions.TryAdd(definition.Id, definition))
            return new(null, $"A workflow definition with ID '{definition.Id}' already exists.");

        return new(definition);
    }

    public Result<WorkflowDefinition> GetDefinition(string definitionId) =>
        _definitions.TryGetValue(definitionId, out var def)
            ? new(def)
            : new(null, $"Definition with ID '{definitionId}' not found.");

    public IEnumerable<WorkflowDefinition> GetAllDefinitions() => _definitions.Values;


    public Result<WorkflowInstance> StartInstance(string definitionId)
    {
        var defResult = GetDefinition(definitionId);
        if (defResult.IsFailure) return new(null, defResult.ErrorMessage);

        var definition = defResult.Value!;
        var initialState = definition.States.Single(s => s.IsInitial);

        var instance = new WorkflowInstance
        {
            WorkflowDefinitionId = definition.Id,
            CurrentStateId = initialState.Id,
        };

        _instances[instance.Id] = instance;
        return new(instance);
    }

    public Result<WorkflowInstance> GetInstance(Guid instanceId) =>
        _instances.TryGetValue(instanceId, out var instance)
            ? new(instance)
            : new(null, $"Instance with ID '{instanceId}' not found.");

    public IEnumerable<WorkflowInstance> GetAllInstances() => _instances.Values;

    public Result<WorkflowInstance> ExecuteAction(Guid instanceId, string actionId)
    {

        var instanceResult = GetInstance(instanceId);
        if (instanceResult.IsFailure) return new(null, instanceResult.ErrorMessage);
        var instance = instanceResult.Value!;

        var defResult = GetDefinition(instance.WorkflowDefinitionId);

        if (defResult.IsFailure) return new(null, "Internal Error: Could not find definition for existing instance.");
        var definition = defResult.Value!;
        
        var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
        if (currentState is null)
            return new(null, $"Internal Error: Current state '{instance.CurrentStateId}' not found in definition.");
        
        if (currentState.IsFinal)
            return new(null, "Cannot execute action. The workflow instance is in a final state.");

        var action = definition.Actions.FirstOrDefault(a => a.Id == actionId);
        if (action is null)
            return new(null, $"Action '{actionId}' not found in the workflow definition.");

        if (!action.Enabled)
            return new(null, $"Action '{actionId}' is disabled.");

        if (!action.FromStates.Contains(instance.CurrentStateId))
            return new(null, $"Action '{actionId}' cannot be executed from the current state '{instance.CurrentStateId}'.");

        instance.CurrentStateId = action.ToState;
        instance.History.Add(new HistoryEntry(action.Id, action.ToState, DateTime.UtcNow));
        
        return new(instance);
    }
}