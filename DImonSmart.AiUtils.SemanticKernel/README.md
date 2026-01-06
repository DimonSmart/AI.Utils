## DimonSmart.AiUtils.SemanticKernel

Semantic Kernel helpers for long-running tool cycles.

### Motivation

In tool-calling loops chat history grows very fast:

- Assistant messages that contain tool calls (function calls).
- Tool messages with function results.
- Occasional plain text messages (retries, warnings, fallback responses).

Sending the whole history on every iteration increases cost and latency, and it also increases the risk of breaking the required tool-call sequence if trimming cuts a tool call from its matching tool result.

Semantic Kernel provides built-in reducers (for example, truncation reducers), but in long-running tool loops you often want a deterministic strategy:

- Always keep the initial System/User instructions.
- Keep only the most recent tool turns.
- Optionally keep a small tail of regular (non-tool) messages.

This package provides a reducer designed for that pattern.
It also provides a tool-invocation loop that calls the reducer between function invocations to keep long processing cycles stable and bounded.

### LongCycleFunctionCallReducer

Trims long function-call chains by keeping:

- Initial contiguous System/User messages (header).
- Last N tool turns (assistant tool-call message + matching tool results by CallId).
- Last M regular tail messages (non-tool messages and assistant messages without tool-calls).

`ReduceAsync` returns `null` when no reduction is needed.

#### Parameters

* `lastToolsToKeep`: how many latest tool turns to keep.

  * `0` means drop all tool turns.
* `lastMessagesToKeep`: how many latest regular (non-tool) messages to keep.

  * `0` means keep no tail regular messages (only header and kept tool turns).

### Installation

```bash
dotnet add package DimonSmart.AiUtils.SemanticKernel
```

### Minimal example (manual tool loop)

```csharp
using DimonSmart.AiUtils.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// keep header + last 2 tool turns + last 1 regular message
var reducer = new LongCycleFunctionCallReducer(lastToolsToKeep: 2, lastMessagesToKeep: 1);

while (true)
{
    // Reduce history before sending it to the model
    var reducedHistory = await reducer.ReduceAsync(history) ?? history;

    var assistant = await chat.GetChatMessageContentAsync(reducedHistory, settings, kernel);
    history.Add(assistant);

    var calls = FunctionCallContent.GetFunctionCalls(assistant);
    foreach (var call in calls)
    {
        var result = await call.InvokeAsync(kernel);
        history.Add(result.ToChatMessage());
    }

    // Optional: reduce again if your loop grows very quickly
    // history = new ChatHistory(await reducer.ReduceAsync(history) ?? history);
}
```

### Usage

```csharp
using DimonSmart.AiUtils.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var reducer = new LongCycleFunctionCallReducer(lastToolsToKeep: 2, lastMessagesToKeep: 4);
var reduced = await reducer.ReduceAsync(history) ?? history;
```

### ReducedToolInvocationLoop

Runs a tool-calling loop with optional history reduction before sending the prompt and between tool calls.
This is useful when a single assistant turn contains multiple tool calls and you need to reduce history after each tool result.

#### Options

- `MaxTurns`: safety limit for the loop.
- `EnforceSingleToolCallPerTurn`: if true, logs a warning when multiple calls are returned.
- `Reducer`: optional reducer called before each LLM call and after each tool result.
- `Settings`: optional prompt settings (autoInvoke is always forced to false).
- `StopOnToolResult`: optional predicate to stop the loop on a specific tool result.

#### Example

```csharp
var res = await new ReducedToolInvocationLoop(
        chat,
        kernel,
        new ToolLoopOptions
        {
            StopOnToolResult = (call, result) =>
                $"{call.PluginName}.{call.FunctionName}".Equals("Lamp.NeedToBlinkLight", StringComparison.OrdinalIgnoreCase) &&
                result.Result is bool b && b == false
        })
    .RunAsync(history);

Console.WriteLine(res.Reason);
Console.WriteLine(res.FinalAssistantMessage?.Content);
```
