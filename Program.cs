using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IWorkflowService, WorkflowService>();

var app = builder.Build();


var workflowApi = app.MapGroup("/api");

var definitionsApi = workflowApi.MapGroup("/definitions");

definitionsApi.MapPost("/", (IWorkflowService service, WorkflowDefinition definition) =>
{
    var result = service.CreateDefinition(definition);
    return result.IsSuccess
        ? Results.Created($"/api/definitions/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { error = result.ErrorMessage });
}).WithSummary("Create a new workflow definition.");

definitionsApi.MapGet("/", (IWorkflowService service) =>
{
    return Results.Ok(service.GetAllDefinitions());
}).WithSummary("List all workflow definitions.");

definitionsApi.MapGet("/{id}", (IWorkflowService service, string id) =>
{
    var result = service.GetDefinition(id);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.ErrorMessage });
}).WithSummary("Get a specific workflow definition by its ID.");

var instancesApi = workflowApi.MapGroup("/instances");

instancesApi.MapPost("/", (IWorkflowService service, [FromBody] StartInstanceRequest request) =>
{
    var result = service.StartInstance(request.DefinitionId);
    return result.IsSuccess
        ? Results.Created($"/api/instances/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { error = result.ErrorMessage });
}).WithSummary("Start a new workflow instance from a definition.");

instancesApi.MapGet("/", (IWorkflowService service) =>
{
    return Results.Ok(service.GetAllInstances());
}).WithSummary("List all running workflow instances.");

instancesApi.MapGet("/{id:guid}", (IWorkflowService service, Guid id) =>
{
    var result = service.GetInstance(id);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.ErrorMessage });
}).WithSummary("Get a specific workflow instance by its ID.");

instancesApi.MapPost("/{id:guid}/execute", (IWorkflowService service, Guid id, [FromBody] ExecuteActionRequest request) =>
{
    var result = service.ExecuteAction(id, request.ActionId);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.ErrorMessage });
}).WithSummary("Execute an action on a workflow instance.");


app.Run();

public record StartInstanceRequest(string DefinitionId);
public record ExecuteActionRequest(string ActionId);