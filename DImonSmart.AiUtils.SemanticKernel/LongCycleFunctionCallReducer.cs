using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DimonSmart.AiUtils.SemanticKernel;

/// <summary>
/// Trims long function-call chains for long-running cycles by keeping:
/// 1) Initial contiguous prefix of System/User messages ("header").
/// 2) Last N tool turns: assistant tool-call message + matching tool results by CallId inside the contiguous tool block.
/// 3) Last M regular messages from the tail (non-tool and non tool-call assistant).
///
/// lastToolsToKeep == 0  => drops all tool turns
/// lastMessagesToKeep == 0 => keeps no tail regular messages (only header + tool turns)
/// </summary>
public sealed class LongCycleFunctionCallReducer : IChatHistoryReducer
{
    public int LastToolsToKeep { get; }
    public int LastMessagesToKeep { get; }

    public LongCycleFunctionCallReducer(int lastToolsToKeep, int lastMessagesToKeep)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lastToolsToKeep);
        ArgumentOutOfRangeException.ThrowIfNegative(lastMessagesToKeep);

        LastToolsToKeep = lastToolsToKeep;
        LastMessagesToKeep = lastMessagesToKeep;
    }

    public Task<IEnumerable<ChatMessageContent>?> ReduceAsync(
        IReadOnlyList<ChatMessageContent> chatHistory,
        CancellationToken cancellationToken = default)
    {
        if (chatHistory.Count == 0)
            return Task.FromResult<IEnumerable<ChatMessageContent>?>(null);

        var headerEnd = HeaderEnd(chatHistory);

        var keep = new HashSet<int>(Enumerable.Range(0, headerEnd));

        if (LastToolsToKeep > 0)
        {
            keep.UnionWith(
                ToolTurns(chatHistory, headerEnd)
                    .TakeLast(LastToolsToKeep)
                    .SelectMany(t => t.Indices));
        }

        if (LastMessagesToKeep > 0)
            keep.UnionWith(TailRegularMessageIndices(chatHistory, LastMessagesToKeep));

        var reduced = keep
            .OrderBy(i => i)
            .Select(i => chatHistory[i])
            .ToList();

        return Task.FromResult<IEnumerable<ChatMessageContent>?>(
            IsSameByReference(chatHistory, reduced) ? null : reduced);
    }

    private static int HeaderEnd(IReadOnlyList<ChatMessageContent> history) =>
        history.TakeWhile(m => IsSystemOrUser(m.Role)).Count();

    private static bool IsSystemOrUser(AuthorRole role) =>
        role == AuthorRole.System || role == AuthorRole.User;

    private static IEnumerable<int> TailRegularMessageIndices(IReadOnlyList<ChatMessageContent> history, int lastMessagesToKeep)
    {
        if (lastMessagesToKeep == 0)
            return [];

        static bool IsRegular(ChatMessageContent m) =>
            m.Role != AuthorRole.Tool &&
            !(m.Role == AuthorRole.Assistant && HasAnyFunctionCall(m));

        return history
            .Select((m, i) => (m, i))
            .Where(x => IsRegular(x.m))
            .Select(x => x.i)
            .TakeLast(lastMessagesToKeep);
    }

    private static IEnumerable<ToolTurn> ToolTurns(IReadOnlyList<ChatMessageContent> history, int startIndex)
    {
        for (var i = startIndex; i < history.Count; i++)
        {
            if (history[i].Role != AuthorRole.Assistant)
                continue;

            var callIds = GetCallIds(history[i]).ToHashSet(StringComparer.Ordinal);
            if (callIds.Count == 0)
                continue;

            var toolBlockIndices = ContiguousToolBlockIndices(history, i + 1).ToList();
            var matchingToolIndices = toolBlockIndices
                .Where(toolIdx => ToolMessageHasAnyMatchingCallId(history[toolIdx], callIds))
                .ToList();

            var indices = new[] { i }.Concat(matchingToolIndices).ToList();
            yield return new ToolTurn(indices);

            if (toolBlockIndices.Count > 0)
                i = toolBlockIndices[^1]; // jump over the tool block (if any)
        }
    }

    private static IEnumerable<int> ContiguousToolBlockIndices(IReadOnlyList<ChatMessageContent> history, int startIndex) =>
        history
            .Skip(startIndex)
            .TakeWhile(m => m.Role == AuthorRole.Tool)
            .Select((_, offset) => startIndex + offset);

    private static IEnumerable<string> GetCallIds(ChatMessageContent assistantMsg)
    {
        IEnumerable<FunctionCallContent> calls;
        try { calls = FunctionCallContent.GetFunctionCalls(assistantMsg); }
        catch { calls = assistantMsg.Items?.OfType<FunctionCallContent>() ?? []; }

        return calls
            .Select(c => c.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))!
            .Select(id => id!);
    }

    private static bool ToolMessageHasAnyMatchingCallId(ChatMessageContent toolMsg, HashSet<string> expectedCallIds)
    {
        var results = toolMsg.Items?.OfType<FunctionResultContent>() ?? [];
        return results
            .Select(r => r.CallId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Any(id => expectedCallIds.Contains(id!));
    }

    private static bool HasAnyFunctionCall(ChatMessageContent msg)
    {
        try { return FunctionCallContent.GetFunctionCalls(msg).Any(); }
        catch { return msg.Items?.OfType<FunctionCallContent>().Any() == true; }
    }

    private static bool IsSameByReference(IReadOnlyList<ChatMessageContent> original, IReadOnlyList<ChatMessageContent> candidate) =>
        original.Count == candidate.Count &&
        original.Zip(candidate, (a, b) => ReferenceEquals(a, b)).All(x => x);

    private sealed record ToolTurn(IReadOnlyList<int> Indices);
}
