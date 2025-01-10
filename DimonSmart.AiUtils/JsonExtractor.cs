namespace DimonSmart.AiUtils
{
    public static class JsonExtractor
    {
        public static string ExtractJson(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // We scan for top-level '{' or '['
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                var open = ch == '{' || ch == '[';
                if (!open)
                    continue;

                // Attempt a parse
                var candidate = GetBracketSubstring(text, i);
                if (string.IsNullOrEmpty(candidate))
                    continue;

                // Attempt a naive "recursive parse"
                var success = IsWellFormedJson(candidate.Trim());
                if (success)
                    return candidate;
            }

            return string.Empty;
        }

        // Use bracket matching to extract a balanced substring
        private static string? GetBracketSubstring(string text, int startIndex)
        {
            var stack = new Stack<char>();
            var inString = false;
            var escaped = false;

            stack.Push(text[startIndex]);

            for (var j = startIndex + 1; j < text.Length; j++)
            {
                var current = text[j];

                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }
                }
                else
                {
                    if (current == '"')
                    {
                        inString = true;
                    }
                    else if (current == '{' || current == '[')
                    {
                        stack.Push(current);
                    }
                    else if (current == '}' || current == ']')
                    {
                        if (stack.Count == 0)
                            return null;
                        var top = stack.Peek();
                        var match = (top == '{' && current == '}') ||
                                     (top == '[' && current == ']');
                        if (!match)
                            return null;
                        stack.Pop();

                        if (stack.Count == 0)
                            return text.Substring(startIndex, j - startIndex + 1);
                    }
                }
            }

            return null;
        }

        // Recursively parse a subset of JSON: objects, arrays, strings, ignoring
        // advanced numeric or boolean forms for brevity. Returns false if something is clearly off.
        private static bool IsWellFormedJson(string s)
        {
            s = s.Trim();
            if (s.Length < 2)
                return false;

            if (s[0] == '{')
                return ParseObject(s);
            if (s[0] == '[')
                return ParseArray(s);

            return false;
        }

        private static bool ParseObject(string s)
        {
            // Expect { ... }
            if (s.Length < 2 || s[^1] != '}')
                return false;

            // Remove outer braces
            var inner = s.Substring(1, s.Length - 2).Trim();
            if (inner.Length == 0)
                return true; // Empty object

            // Parse each property: "key": value, separated by commas
            var i = 0;
            while (true)
            {
                SkipWhitespace(inner, ref i);
                // Expect a quoted string for key
                if (i >= inner.Length || inner[i] != '"')
                    return false;

                if (!ReadString(inner, ref i))
                    return false;

                SkipWhitespace(inner, ref i);
                if (i >= inner.Length || inner[i] != ':')
                    return false;
                i++; // skip ':'

                SkipWhitespace(inner, ref i);
                // Now parse value
                if (!ParseValue(inner, ref i))
                    return false;

                SkipWhitespace(inner, ref i);
                // If we are at the end => success
                if (i == inner.Length)
                    return true;

                // Otherwise, expect a comma
                if (inner[i] != ',')
                    return false;
                i++;
            }
        }

        private static bool ParseArray(string s)
        {
            // Expect [ ... ]
            if (s.Length < 2 || s[^1] != ']')
                return false;

            var inner = s.Substring(1, s.Length - 2).Trim();
            if (inner.Length == 0)
                return true; // Empty array

            var i = 0;
            while (true)
            {
                SkipWhitespace(inner, ref i);
                if (!ParseValue(inner, ref i))
                    return false;

                SkipWhitespace(inner, ref i);
                if (i == inner.Length)
                    return true; // no more elements

                if (inner[i] != ',')
                    return false;
                i++;
            }
        }

        private static bool ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length)
                return false;

            // 1) Object
            if (s[i] == '{')
            {
                var start = i;
                var block = GetBracketSubstring(s, start);
                if (string.IsNullOrEmpty(block))
                    return false;

                i += block.Length;
                return IsWellFormedJson(block);
            }

            // 2) Array
            if (s[i] == '[')
            {
                var start = i;
                var block = GetBracketSubstring(s, start);
                if (string.IsNullOrEmpty(block))
                    return false;

                i += block.Length;
                return IsWellFormedJson(block);
            }

            // 3) String
            if (s[i] == '"')
                return ReadString(s, ref i);

            // 4) Boolean or null (very simplistic):
            if (CheckKeyword(s, ref i, "true")) return true;
            if (CheckKeyword(s, ref i, "false")) return true;
            if (CheckKeyword(s, ref i, "null")) return true;

            // 5) Number
            if (char.IsDigit(s[i]) || s[i] == '-')
                return ParseNumber(s, ref i);

            // If none matched, fail
            return false;
        }

        private static bool ParseNumber(string s, ref int i)
        {
            var start = i;
            var hasDigit = false;

            if (s[i] == '-')
                i++;

            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.'))
            {
                hasDigit = true;
                i++;
            }

            if (!hasDigit)
                return false;

            // Possibly skip exponent part if you'd like (e.g. e+10, etc.)
            // For a minimal approach, skip that.

            // After the number, we consider the value done. We don't strictly enforce
            // whether next char is ',' or '}' here, because the outer parse loop 
            // will check that. Just return true.
            return true;
        }

        private static bool CheckKeyword(string s, ref int i, string keyword)
        {
            var end = i + keyword.Length;
            if (end > s.Length)
                return false;

            var candidate = s.Substring(i, keyword.Length);
            if (candidate == keyword)
            {
                i += keyword.Length;
                return true;
            }
            return false;
        }


        private static bool ReadString(string s, ref int i)
        {
            // We expect s[i] == '"'
            i++;
            var escaped = false;
            while (i < s.Length)
            {
                var c = s[i];
                if (!escaped && c == '\\')
                {
                    escaped = true;
                    i++;
                    continue;
                }
                if (!escaped && c == '"')
                {
                    i++;
                    return true;
                }
                escaped = false;
                i++;
            }
            return false;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
        }
    }
}
