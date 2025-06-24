using System.Text.RegularExpressions;

namespace DimonSmart.AiUtils
{
    public static class ThinkTagParser
    {
        // Compiled regex to match think tags with surrounding newlines
        private static readonly Regex ThinkContentRegex = new Regex(@"<think>(.*?)<\/think>", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ThinkWithSurroundingNewlinesRegex = new Regex(@"(\n*)(<think>.*?<\/think>)(\n*)", RegexOptions.Singleline | RegexOptions.Compiled);

        // Compiled regex to collapse multiple spaces (but not newlines) into a single space.
        private static readonly Regex MultipleSpacesRegex = new Regex(@"[ \t]{2,}", RegexOptions.Compiled);

        /// <summary>
        /// Represents the result of parsing the input.
        /// Provides two fields: 'Thoughts' (joined content from all <think> tags) and 'Answer' (the cleaned remaining text).
        /// </summary>
        public class ThinkAnswer
        {
            // Raw list of thought segments extracted from <think> tags.
            private readonly List<string> _thoughtSegments;
            // Raw list of answer lines derived from the input after removing <think> tags.
            private readonly List<string> _answerLines;

            public ThinkAnswer(List<string> thoughtSegments, List<string> answerLines)
            {
                _thoughtSegments = thoughtSegments;
                _answerLines = answerLines;
            }

            /// <summary>
            /// Gets the combined thought content joined by newline.
            /// </summary>
            public string Thoughts => string.Join("\n", _thoughtSegments);

            /// <summary>
            /// Gets the individual thought segments as an immutable list.
            /// </summary>
            public IReadOnlyList<string> ThoughtSegments => _thoughtSegments;

            /// <summary>
            /// Gets the combined answer text joined by newline.
            /// </summary>
            public string Answer => string.Join("\n", _answerLines);
        }

        /// <summary>
        /// Extracts all content within <think> tags from the input string.
        /// This method is specifically designed to separate the <think> parts.
        /// It returns the extracted thought content (as a joined string) and the cleaned answer text (as a joined string).
        /// </summary>
        /// <param name="input">Input string that may contain one or more <think> tags.</param>
        /// <returns>An instance of ThinkAnswer containing the parsed thoughts and answer.</returns>
        public static ThinkAnswer ExtractThinkAnswer(string input)
        {
            // Find all <think> tag matches.
            var thoughtMatches = ThinkContentRegex.Matches(input);
            var thoughtSegments = new List<string>();
            foreach (Match match in thoughtMatches)
            {
                if (match.Success)
                {
                    var thought = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(thought))
                        thoughtSegments.Add(thought);
                }
            }

            // Replace think tags with intelligent newline handling
            var answerText = ThinkWithSurroundingNewlinesRegex.Replace(input, match =>
            {
                var newlinesBefore = match.Groups[1].Value;
                var thinkTag = match.Groups[2].Value;
                var newlinesAfter = match.Groups[3].Value;

                // Count newlines before and after
                var beforeCount = newlinesBefore.Length;
                var afterCount = newlinesAfter.Length;

                // If think tag is inline (no newlines around), replace with space
                if (beforeCount == 0 && afterCount == 0)
                {
                    return " ";
                }

                // Keep the larger of the two newline groups, but if both are significant (>1), 
                // preserve the structure by returning the max count
                var keepNewlines = Math.Max(beforeCount, afterCount);

                // Но если у нас есть переводы строк с обеих сторон, то нам нужно сохранить
                // достаточно переводов строк чтобы maintain the spacing structure
                if (beforeCount > 1 && afterCount > 1)
                {
                    // Сохраняем максимальное количество минус 1 (так как удаляем тег)
                    keepNewlines = Math.Max(beforeCount, afterCount);
                }
                else if (beforeCount > 0 && afterCount > 0)
                {
                    // Если есть переводы строк с обеих сторон, но не больше 1
                    keepNewlines = Math.Max(beforeCount, afterCount);
                }
                else
                {
                    // Если переводы строк только с одной стороны
                    keepNewlines = beforeCount + afterCount;
                }

                return new string('\n', keepNewlines);
            });

            // Collapse multiple spaces (but not newlines)
            var answerCleaned = MultipleSpacesRegex.Replace(answerText, " ").Trim();

            // Split by lines
            var answerLines = answerCleaned
                .Split(new[] { '\n' }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .ToList();

            // Remove empty lines only from the beginning and end
            while (answerLines.Count > 0 && string.IsNullOrEmpty(answerLines[0]))
                answerLines.RemoveAt(0);
            while (answerLines.Count > 0 && string.IsNullOrEmpty(answerLines[answerLines.Count - 1]))
                answerLines.RemoveAt(answerLines.Count - 1);

            return new ThinkAnswer(thoughtSegments, answerLines);
        }
    }
}
