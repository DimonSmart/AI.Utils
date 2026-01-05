# DimonSmart.AiUtils

Utilities for extracting structured content from LLM responses.

## Features
- Extract JSON objects/arrays from mixed text.
- Parse <think> tags to separate thoughts from final answers.

## Installation
```bash
dotnet add package DimonSmart.AiUtils
```

## Usage
```csharp
using DimonSmart.AiUtils;

var json = JsonExtractor.ExtractJson(responseText);
var all = JsonExtractor.ExtractAllJsons(responseText);

var result = ThinkTagParser.ExtractThinkAnswer(responseText);
var thoughts = result.Thoughts;
var answer = result.Answer;
```
