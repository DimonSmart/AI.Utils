using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DimonSmart.AiUtils.SemanticKernel.Tests;

public class LongCycleFunctionCallReducerTests
{
    private const string FunctionName = "run";
    private const string PluginName = "tools";

    [Fact]
    public async Task ReduceAsync_KeepsHeaderLastToolTurnAndTailRegularMessages()
    {
        var system = Message(AuthorRole.System, "sys");
        var user = Message(AuthorRole.User, "user");
        var firstRegular = Message(AuthorRole.Assistant, "first");
        var toolCall1 = ToolCall("call-1");
        var toolResult1 = ToolResult("call-1");
        var toolResultIgnored = ToolResult("call-x");
        var midRegular = Message(AuthorRole.Assistant, "mid");
        var toolCall2 = ToolCall("call-2");
        var toolResult2 = ToolResult("call-2");
        var tailAssistant = Message(AuthorRole.Assistant, "tail");
        var tailUser = Message(AuthorRole.User, "tail-user");

        var history = new List<ChatMessageContent>
        {
            system,
            user,
            firstRegular,
            toolCall1,
            toolResult1,
            toolResultIgnored,
            midRegular,
            toolCall2,
            toolResult2,
            tailAssistant,
            tailUser
        };

        var reducer = new LongCycleFunctionCallReducer(lastToolsToKeep: 1, lastMessagesToKeep: 2);

        var reduced = await reducer.ReduceAsync(history);

        Assert.NotNull(reduced);
        var list = reduced!.ToList();

        Assert.Collection(list,
            message => Assert.Same(system, message),
            message => Assert.Same(user, message),
            message => Assert.Same(toolCall2, message),
            message => Assert.Same(toolResult2, message),
            message => Assert.Same(tailAssistant, message),
            message => Assert.Same(tailUser, message));
    }

    [Fact]
    public async Task ReduceAsync_DropsToolTurns_WhenDisabled()
    {
        var system = Message(AuthorRole.System, "sys");
        var user = Message(AuthorRole.User, "user");
        var toolCall = ToolCall("call-1");
        var toolResult = ToolResult("call-1");
        var tailAssistant = Message(AuthorRole.Assistant, "tail");
        var tailUser = Message(AuthorRole.User, "tail-user");

        var history = new List<ChatMessageContent>
        {
            system,
            user,
            toolCall,
            toolResult,
            tailAssistant,
            tailUser
        };

        var reducer = new LongCycleFunctionCallReducer(lastToolsToKeep: 0, lastMessagesToKeep: 2);

        var reduced = await reducer.ReduceAsync(history);

        Assert.NotNull(reduced);
        var list = reduced!.ToList();

        Assert.Collection(list,
            message => Assert.Same(system, message),
            message => Assert.Same(user, message),
            message => Assert.Same(tailAssistant, message),
            message => Assert.Same(tailUser, message));
    }

    [Fact]
    public async Task ReduceAsync_AllowsToolCallWithoutResults()
    {
        var system = Message(AuthorRole.System, "sys");
        var user = Message(AuthorRole.User, "user");
        var toolCall = ToolCall("call-1");
        var tailAssistant = Message(AuthorRole.Assistant, "tail");

        var history = new List<ChatMessageContent>
        {
            system,
            user,
            toolCall,
            tailAssistant
        };

        var reducer = new LongCycleFunctionCallReducer(lastToolsToKeep: 1, lastMessagesToKeep: 0);

        var reduced = await reducer.ReduceAsync(history);

        Assert.NotNull(reduced);
        var list = reduced!.ToList();

        Assert.Collection(list,
            message => Assert.Same(system, message),
            message => Assert.Same(user, message),
            message => Assert.Same(toolCall, message));
    }

    private static ChatMessageContent Message(AuthorRole role, string content) =>
        new ChatMessageContent(role, content);

    private static ChatMessageContent ToolCall(string callId)
    {
        var items = new ChatMessageContentItemCollection
        {
            new FunctionCallContent(FunctionName, PluginName, callId, new KernelArguments())
        };

        return new ChatMessageContent(AuthorRole.Assistant, items);
    }

    private static ChatMessageContent ToolResult(string callId)
    {
        var items = new ChatMessageContentItemCollection
        {
            new FunctionResultContent(FunctionName, PluginName, callId, "ok")
        };

        return new ChatMessageContent(AuthorRole.Tool, items);
    }
}
