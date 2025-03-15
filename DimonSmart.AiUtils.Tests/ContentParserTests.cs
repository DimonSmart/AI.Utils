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
            Assert.Equal("Start middle end.", result.Answer);
        }
    }
}
