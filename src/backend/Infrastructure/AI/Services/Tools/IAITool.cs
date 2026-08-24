namespace AI;

/// <summary>
/// Native tool the agent can call. Each implementation is registered at
/// startup and surfaced to the model as a function definition.
/// </summary>
public interface IAITool
{
    /// <summary>Unique tool name (used in function-calling).</summary>
    string Name { get; }

    /// <summary>Human-readable description shown to the model.</summary>
    string Description { get; }

    /// <summary>JSON Schema describing the tool's input parameters.</summary>
    object InputSchema { get; }

    /// <summary>
    /// Execute the tool with model-provided JSON arguments. The returned string
    /// is fed back to the model as the tool result; tools may also return JSON
    /// matching <see cref="AIToolResponseEnvelope"/> to attach source items.
    /// </summary>
    Task<string> ExecuteAsync(
        string arguments,
        AccessControlContext context,
        CancellationToken cancellationToken = default);
}
