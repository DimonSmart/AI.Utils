# DimonSmart.AiUtils.SemanticKernel

Semantic Kernel helpers for long-running tool cycles.

## LongCycleFunctionCallReducer
Trims long function-call chains by keeping:
- Initial contiguous System/User messages (header).
- Last N tool turns (assistant tool-call + matching tool results by CallId).
- Last M regular tail messages.

Note: ReduceAsync returns null when no reduction is needed.

## Installation
```bash
dotnet add package DimonSmart.AiUtils.SemanticKernel
```

## Usage
```csharp
using DimonSmart.AiUtils.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var reducer = new LongCycleFunctionCallReducer(lastToolsToKeep: 2, lastMessagesToKeep: 4);
var reduced = await reducer.ReduceAsync(history) ?? history;
```
