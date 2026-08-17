# Error Handling Specification

> **This is a PERMANENT technical document.** It defines the standard error handling patterns for App Template projects. Do not change the core patterns — the middleware and the frontend both depend on them.

---

## The default: return the DTO, use the status code

The built-in controllers return the DTO directly and let the HTTP status code carry the outcome. This is the pattern to copy:

```csharp
// Success
return Ok(_mapper.Map<EntityDto>(entity));
return Ok(_mapper.Map<List<EntityDto>>(items));

// Client errors
return NotFound("Entity not found");
return BadRequest("Name is required");
```

An optional envelope, `ApiResponse<T>` in `API/Models/ApiResponse.cs`, is available if your project prefers a uniform body on every response:

```csharp
return Ok(ApiResponse<EntityDto>.Ok(dto, "Entity created successfully"));
return Ok(ApiResponse<EntityDto>.NotFound("Entity not found"));
```

Pick one and stay consistent across your project. Mixing the two means every frontend caller has to guess which shape it is unwrapping.

## HTTP Status Code Usage

| Status Code                 | When to Use                                | Example                                   |
| --------------------------- | ------------------------------------------ | ----------------------------------------- |
| `200 OK`                    | Successful GET, POST (save/edit)           | Return entity/list wrapped in ApiResponse |
| `204 No Content`            | Successful DELETE                          | `return NoContent();`                     |
| `400 Bad Request`           | Validation failure, invalid input          | Missing required fields, invalid format   |
| `401 Unauthorized`          | No valid session                           | Handled by SessionValidationMiddleware    |
| `403 Forbidden`             | Valid session but insufficient permissions | `[RequireAccessFunction]` attribute       |
| `404 Not Found`             | Entity does not exist                      | GET/Edit/Delete with invalid ID           |
| `500 Internal Server Error` | Unhandled exception                        | Caught by ExceptionHandlingMiddleware     |

## Controller Error Patterns

### GET by ID

```csharp
[HttpGet("{id}")]
[RequireAccessFunction(AccessFunctionCodes.Api.YourEntityRead)]
public async Task<ActionResult<YourEntityDto>> Get(int id)
{
    var item = await _service.GetByIdAsync(id);
    if (item == null)
        return NotFound("Item not found");
    return Ok(_mapper.Map<YourEntityDto>(item));
}
```

### Edit (Update)

```csharp
[HttpPost]
[RequireAccessFunction(AccessFunctionCodes.Api.YourEntityManage)]
public async Task<ActionResult<YourEntityDto>> Edit([FromBody] YourEntityDto dto)
{
    if (dto.Id <= 0)
        return BadRequest("Invalid ID");

    var existing = await _service.GetByIdAsync(dto.Id);
    if (existing == null)
        return NotFound("Item not found");

    // Assign the fields the caller is allowed to change, one at a time.
    existing.Name = dto.Name;
    existing.IsActive = dto.IsActive;

    var updated = await _service.SaveOrUpdateAsync(existing);
    return Ok(_mapper.Map<YourEntityDto>(updated));
}
```

> **Do not write `_mapper.Map(dto, existing)` here.** Projecting a request body wholesale onto a loaded entity lets a caller overwrite audit columns, owner ids, and anything else you never meant to expose. Explicit assignment is the whole point of this pattern.

### Delete

```csharp
[HttpPost("Delete/{id}")]
[RequireAccessFunction(AccessFunctionCodes.Api.YourEntityManage)]
public async Task<ActionResult> Delete(int id)
{
    var result = await _service.DeleteAsync(id);
    if (!result)
        return NotFound("Item not found");
    return Ok();
}
```

### Validation in Service Layer

```csharp
// Throw exceptions for business rule violations
// These are caught by ExceptionHandlingMiddleware
if (entity.StartDate > entity.EndDate)
    throw new InvalidOperationException("Start date must be before end date");
```

## Exception Handling Middleware

The `ExceptionHandlingMiddleware` (DO NOT MODIFY) catches all unhandled exceptions and returns a structured error response:

```json
{
  "statusCode": 500,
  "message": "An unexpected error occurred",
  "details": "Exception details (only in Development environment)"
}
```

## Frontend Error Handling

### API Service Layer

```typescript
// Services should let errors propagate to the component
const yourEntityService = {
  async getAll(): Promise<YourEntity[]> {
    const response = await api.get<YourEntity[]>("/api/YourEntity/GetAll");
    return response.data;
  },
};
```

### Vue Component Layer

```typescript
const fetchItems = async () => {
  isLoading.value = true;
  try {
    items.value = await yourEntityService.getAll();
  } catch (error) {
    toast.error("Failed to load items");
    console.error("Error loading items:", error);
  } finally {
    isLoading.value = false;
  }
};
```

### Standard Error Toast Messages

| Operation   | Success Message              | Error Message           |
| ----------- | ---------------------------- | ----------------------- |
| Create      | "Item created successfully"  | "Failed to create item" |
| Update      | "Item updated successfully"  | "Failed to update item" |
| Delete      | "Item deleted successfully"  | "Failed to delete item" |
| Load        | _(none — just show data)_    | "Failed to load items"  |
| File Upload | "File uploaded successfully" | "Failed to upload file" |

## Rules

1. **Never swallow exceptions silently** — always log or display to user
2. **Use `useToast()` for user-facing messages** — never `alert()` or `console.log` alone
3. **Business logic errors go in services** — controllers only handle HTTP-level concerns
4. **Don't expose internal details** — error messages to users should be friendly, not stack traces
5. **Always handle loading states** — show a spinner while data loads, handle the error if it fails
6. **Delete operations require confirmation** — show a confirm dialog (`ConfirmDialog`, or `AppConfirmDialog` from `@apptemplate/ui`) before deleting
7. **Log with `ILogger`** — use `_logger.LogError()` for errors, `_logger.LogWarning()` for recoverable issues
8. **Never log a credential, token, or session id** — not in an error message, not in a debug line, not in a breadcrumb

## Retry Policy (For External Integrations)

When calling external APIs or services, use this retry strategy:

| Scenario                | Retry      | Wait                             |
| ----------------------- | ---------- | -------------------------------- |
| Network timeout         | 3 attempts | Exponential backoff (1s, 2s, 4s) |
| HTTP 429 (Rate Limit)   | 3 attempts | Use `Retry-After` header         |
| HTTP 500 (Server Error) | 2 attempts | 2s, 5s                           |
| HTTP 401/403            | No retry   | Log and alert                    |
| HTTP 404                | No retry   | Return not found                 |
