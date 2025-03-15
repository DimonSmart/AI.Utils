using System.Text.RegularExpressions;

namespace DimonSmart.AiUtils
{
    public static class ThinkTagParser
    {
        // Compiled regex to extract content inside <think> tags for performance optimization.
        private static readonly Regex ThinkContentRegex = new Regex(@"<think>(.*?)<\/think>", RegexOptions.Singleline | RegexOptions.Compiled);

        // Compiled regex to collapse multiple whitespace characters into a single space.
        private static readonly Regex MultipleSpacesRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);

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

            // Replace all <think> sections with a space.
            var answerWithExtraSpaces = ThinkContentRegex.Replace(input, " ");
            // Collapse multiple spaces and trim the result.
            var answerCleaned = MultipleSpacesRegex.Replace(answerWithExtraSpaces, " ").Trim();
            // Split the cleaned answer text by newline characters.
            // If there are no newline characters, this will result in a single element.
            var answerLines = answerCleaned
                .Split(new[] { '\n' }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .ToList();

            return new ThinkAnswer(thoughtSegments, answerLines);
        }
    }
}
