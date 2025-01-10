# DimonSmart.AiUtils

DimonSmart.AiUtils is a library designed to extract JSON from responses generated by Large Language Models (LLMs). It ensures accurate extraction even when the LLM response contains additional text like explanations or phrases such as "here is your JSON."

## Features

- **ExtractJson**: Extracts the largest valid JSON object or array from the provided text.
- **ExtractAllJsons**: Extracts all valid JSON objects or arrays from the text.
- **ExtractJson (overload)**: Extracts a single JSON fragment starting from a specific index in the text.

## API Overview

### ExtractJson(string? text)
Extracts the largest valid JSON object or array from the input text.

```csharp
string json = JsonExtractor.ExtractJson(responseText);
```

### ExtractAllJsons(string? text)
Extracts all valid JSON objects or arrays from the input text.

```csharp
var jsons = JsonExtractor.ExtractAllJsons(responseText);
```

### ExtractJson(int start, string? text)
Extracts a single valid JSON fragment starting from the specified index in the text.

```csharp
string json = JsonExtractor.ExtractJson(0, responseText);
```

## Installation

Install via NuGet:

```bash
dotnet add package DimonSmart.AiUtils
```

## Use Cases

- Preprocess LLM responses to extract structured JSON data.
- Handle mixed LLM responses with additional text explanations or metadata.
- Support for extracting multiple JSON fragments from a single response.

## License

This library is licensed under the [0BSD License](LICENSE).
