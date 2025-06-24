namespace DimonSmart.AiUtils.Tests
{
    public class ThinkTagParserTests
    {
        [Fact]
        public void ExtractThinkAnswer_WithSingleThinkTag_ReturnsSeparatedContent()
        {
            // Arrange
            var input = "Before text <think>This is a thought</think> after text.";

            // Act
            var result = ThinkTagParser.ExtractThinkAnswer(input);

            // Assert
            Assert.Equal("This is a thought", result.Thoughts);
            Assert.Equal(new[] { "This is a thought" }, result.ThoughtSegments);
            Assert.Equal("Before text after text.", result.Answer);
        }

        [Fact]
        public void ExtractThinkAnswer_WithoutThinkTag_ReturnsEmptyThoughts()
        {
            // Arrange
            var input = "Text without think tag.";

            // Act
            var result = ThinkTagParser.ExtractThinkAnswer(input);

            // Assert
            Assert.Equal(string.Empty, result.Thoughts);
            Assert.Empty(result.ThoughtSegments);
            Assert.Equal("Text without think tag.", result.Answer);
        }

        [Fact]
        public void ExtractThinkAnswer_WithMultipleThinkTags_ReturnsCombinedThoughtsAndCleanedAnswer()
        {
            // Arrange
            var input = "Start <think>First thought</think> middle <think>Second thought</think> end.";

            // Act
            var result = ThinkTagParser.ExtractThinkAnswer(input);

            // Assert
            Assert.Equal("First thought\nSecond thought", result.Thoughts);
            Assert.Equal(new[] { "First thought", "Second thought" }, result.ThoughtSegments);
            Assert.Equal("Start middle end.", result.Answer);
        }

        [Fact]
        public void ExtractThinkAnswer_WithNewLines_PreservesNewLines()
        {
            // Arrange
            var input = "First line\n<think>Some thought</think>\nSecond line\nThird line";

            // Act
            var result = ThinkTagParser.ExtractThinkAnswer(input);

            // Assert
            Assert.Equal("Some thought", result.Thoughts);
            Assert.Equal("First line\nSecond line\nThird line", result.Answer);
        }

        [Fact]
        public void ExtractThinkAnswer_WithMultipleNewLinesAndThinkTags_PreservesStructure()
        {
            // Arrange
            var input = "Line 1\n\n<think>Thought 1</think>\n\nLine 2\n<think>Thought 2</think>\nLine 3";

            // Act
            var result = ThinkTagParser.ExtractThinkAnswer(input);

            // Assert
            Assert.Equal("Thought 1\nThought 2", result.Thoughts);
            Assert.Equal("Line 1\n\nLine 2\nLine 3", result.Answer);
        }
    }
}
