using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DimonSmart.AiUtils.SemanticKernel;

public sealed class ToolLoopOptions
{
    public int MaxTurns { get; init; } = 2000;

    public bool EnforceSingleToolCallPerTurn { get; init; } = true;

    // Optional. If null, no reduction is applied.
    public IChatHistoryReducer? Reducer { get; init; }

    // Optional. If null, a default instance is created.
    // The loop always forces autoInvoke = false.
    public PromptExecutionSettings? Settings { get; init; }

    // Optional. If provided, the loop stops when the predicate returns true.
    public Func<FunctionCallContent, FunctionResultContent, bool>? StopOnToolResult { get; init; }
}

public enum ToolLoopStopReason
{
    AssistantFinalMessage,
    ToolStop,
    MaxTurnsReached
}

public sealed record ToolLoopResult(
    ToolLoopStopReason Reason,
    ChatHistory History,
    ChatMessageContent? FinalAssistantMessage,
    FunctionCallContent? StopCall = null,
    FunctionResultContent? StopResult = null);

public sealed class ReducedToolInvocationLoop
{
    private readonly IChatCompletionService _chat;
    private readonly Kernel _kernel;
    private readonly ToolLoopOptions _options;
    private readonly ILogger? _log;

    public ReducedToolInvocationLoop(
        IChatCompletionService chat,
        Kernel kernel,
        ToolLoopOptions options,
        ILogger? log = null)
    {
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _log = log;
    }

    public async Task<ToolLoopResult> RunAsync(ChatHistory history, CancellationToken ct = default)
    {
        if (history is null) throw new ArgumentNullException(nameof(history));

        var settings = _options.Settings ?? new PromptExecutionSettings();
        settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false);

        for (int turn = 1; turn <= _options.MaxTurns; turn++)
        {
            history = await ReduceAsync(history, ct);

            _log?.LogInformation("Turn {Turn}: calling LLM (history={Count})", turn, history.Count);

            var assistant = await _chat.GetChatMessageContentAsync(history, settings, _kernel, ct);
            history.Add(assistant);

            var calls = GetFunctionCalls(assistant).ToList();

            // No tool calls means the assistant returned the final answer.
            if (calls.Count == 0)
                return new ToolLoopResult(ToolLoopStopReason.AssistantFinalMessage, history, assistant);

            if (_options.EnforceSingleToolCallPerTurn && calls.Count != 1)
            {
                _log?.LogWarning(
                    "Multiple tool calls returned while EnforceSingleToolCallPerTurn=true. Count={Count}. Executing sequentially.",
                    calls.Count);
            }

            foreach (var call in calls)
            {
                ct.ThrowIfCancellationRequested();

                if (!IsRegisteredInKernel(call))
                {
                    history.Add(new FunctionResultContent(
                            call,
                            $"ERROR: Unknown function '{call.PluginName}.{call.FunctionName}'. Use only functions registered in the kernel.")
                        .ToChatMessage());
                    continue;
                }

                FunctionResultContent result;
                try
                {
                    result = await call.InvokeAsync(_kernel, ct);
                }
                catch (Exception ex)
                {
                    history.Add(new FunctionResultContent(
                            call,
                            $"ERROR invoking '{call.PluginName}.{call.FunctionName}': {ex.GetType().Name}: {ex.Message}")
                        .ToChatMessage());
                    continue;
                }

                history.Add(result.ToChatMessage());
                history = await ReduceAsync(history, ct);

                if (_options.StopOnToolResult?.Invoke(call, result) == true)
                    return new ToolLoopResult(ToolLoopStopReason.ToolStop, history, assistant, call, result);
            }
        }

        return new ToolLoopResult(ToolLoopStopReason.MaxTurnsReached, history, history.LastOrDefault());
    }

    private bool IsRegisteredInKernel(FunctionCallContent call)
    {
        var fn = call.FunctionName;
        if (string.IsNullOrWhiteSpace(fn))
            return false;

        return _kernel.Plugins.TryGetFunction(call.PluginName, fn, out _);
    }

    private async Task<ChatHistory> ReduceAsync(ChatHistory history, CancellationToken ct)
    {
        var reducer = _options.Reducer;
        if (reducer is null) return history;

        var reduced = await reducer.ReduceAsync(history, ct);
        return reduced is null ? history : new ChatHistory(reduced);
    }

    private static System.Collections.Generic.IEnumerable<FunctionCallContent> GetFunctionCalls(ChatMessageContent msg)
    {
        try { return FunctionCallContent.GetFunctionCalls(msg); }
        catch { return msg.Items.OfType<FunctionCallContent>(); }
    }
}
