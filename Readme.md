# Infonetica - Configurable Workflow Engine

This repository contains the solution for the Infonetica Software Engineer Intern take-home exercise. It is a minimal backend service built with .NET 8 that implements a configurable state machine API.

## Core Design Choices & Assumptions

*   **Stack**: .NET 8 Minimal API. This choice was made to keep the project lightweight, with minimal boilerplate, and focus on the core logic, as per the guidelines.
*   **Persistence**: All data (definitions and instances) is stored **in-memory**. The application state will be lost upon restart. This follows the "No database required" guideline. A `ConcurrentDictionary` is used to ensure basic thread safety.
*   **Business Logic**: Logic is encapsulated in a singleton `WorkflowService`, separating it from the API endpoint definitions in `Program.cs`. This improves testability and maintainability.
*   **Error Handling**: The service layer uses a generic `Result<T>` object to clearly communicate success or failure back to the API layer. The API then translates these results into appropriate HTTP status codes (`200 OK`, `201 Created`, `400 Bad Request`, `404 Not Found`) with a simple JSON error message.
*   **IDs**:
    *   **Definition IDs**: User-provided `Name` is converted to a slug-like ID (e.g., "Document Approval" becomes `document-approval`). This makes the API more human-readable.
    *   **Instance IDs**: System-generated `Guid`s are used to guarantee uniqueness.
    *   **State & Action IDs**: Are `string`s defined by the user within the context of a definition.
*   **Incremental Definition Creation**: The requirement mentions creating a definition "in one go or incrementally". This implementation supports creating the full definition in one go via `POST /api/definitions`. An incremental approach (e.g., adding states/actions later) would be a good extension, likely implemented via `PUT` or `PATCH` endpoints, but was omitted to stay within the 2-hour time-box.

## How to Run

### Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Instructions

1.  Clone the repository:
    ```sh
    git clone <your-repo-url>
    cd Infonetica-WorkflowEngine
    ```
2.  Navigate to the project directory:
    ```sh
    cd WorkflowEngine
    ```
3.  Run the application:
    ```sh
    dotnet run
    ```
The API will be available at `http://localhost:5000` (or a similar port).

## API Endpoints

Below are examples of how to use the API with `curl`.

### 1. Create a Workflow Definition

Create a simple document review workflow.

**Request:**
`POST /api/definitions`
```bash
curl -X POST http://localhost:5000/api/definitions -H "Content-Type: application/json" -d '{
  "name": "Document Review",
  "states": [
    { "id": "draft", "name": "Draft", "isInitial": true, "isFinal": false },
    { "id": "in_review", "name": "In Review", "isInitial": false, "isFinal": false },
    { "id": "approved", "name": "Approved", "isInitial": false, "isFinal": true },
    { "id": "rejected", "name": "Rejected", "isInitial": false, "isFinal": true }
  ],
  "actions": [
    { "id": "submit", "name": "Submit for Review", "fromStates": ["draft"], "toState": "in_review" },
    { "id": "approve", "name": "Approve Document", "fromStates": ["in_review"], "toState": "approved" },
    { "id": "reject", "name": "Reject Document", "fromStates": ["in_review"], "toState": "rejected" },
    { "id": "rework", "name": "Rework Document", "fromStates": ["rejected"], "toState": "draft" }
  ]
}'
```

**Success Response (201 Created):**
The created definition object is returned. The `id` will be `document-review`.

---

### 2. List All Definitions

**Request:**
`GET /api/definitions`
```bash
curl http://localhost:5000/api/definitions
```

---

### 3. Start a Workflow Instance

**Request:**
`POST /api/instances`
```bash
curl -X POST http://localhost:5000/api/instances -H "Content-Type: application/json" -d '{
  "definitionId": "document-review"
}'
```

**Success Response (201 Created):**
The new instance object is returned. Note its `id` and `currentStateId` ("draft").
```json
{
  "id": "f8a9b9b0-9b4c-4a3e-8b0a-9e1b9b0a9e1b",
  "workflowDefinitionId": "document-review",
  "currentStateId": "draft",
  "history": []
}
```

---

### 4. Get a Specific Instance

**Request:**
`GET /api/instances/{id}`
```bash
# Replace with the actual instance ID from the previous step
curl http://localhost:5000/api/instances/f8a9b9b0-9b4c-4a3e-8b0a-9e1b9b0a9e1b
```

---

### 5. Execute an Action

Move the instance from "draft" to "in_review".

**Request:**
`POST /api/instances/{id}/execute`
```bash
# Replace with the actual instance ID
curl -X POST http://localhost:5000/api/instances/f8a9b9b0-9b4c-4a3e-8b0a-9e1b9b0a9e1b/execute -H "Content-Type: application/json" -d '{
  "actionId": "submit"
}'
```

**Success Response (200 OK):**
The updated instance is returned. Note the `currentStateId` is now "in_review" and the history is populated.
```json
{
  "id": "f8a9b9b0-9b4c-4a3e-8b0a-9e1b9b0a9e1b",
  "workflowDefinitionId": "document-review",
  "currentStateId": "in_review",
  "history": [
    {
      "actionId": "submit",
      "stateId": "in_review",
      "timestamp": "2023-10-27T10:30:00.123Z"
    }
  ]
}
```

**Error Response Example (400 Bad Request):**
If you try to execute the "submit" action again from the "in_review" state:
```json
{
  "error": "Action 'submit' cannot be executed from the current state 'in_review'."
}
```

## Potential Future Improvements (TODOs)

*   **Unit & Integration Tests**: Add a test suite to formalize correctness and prevent regressions.
*   **Database Persistence**: Replace the in-memory store with a database (e.g., PostgreSQL with EF Core or a document DB like MongoDB) for true persistence.
*   **Authentication & Authorization**: Secure the API.
*   **Asynchronous Workflows**: For long-running tasks, the engine could be extended to support asynchronous operations.
*   **Enhanced Logging**: Integrate a structured logging framework like Serilog.