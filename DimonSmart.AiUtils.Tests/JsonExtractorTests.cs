namespace DimonSmart.AiUtils.Tests
{
    public class JsonExtractorTests
    {
        [Theory]
        [InlineData("{\"key\":\"value\"}", "{\"key\":\"value\"}")]
        [InlineData("Some text... {\"key\": 123} ... Some text", "{\"key\": 123}")]
        [InlineData("Prefix text [1, 2, 3, 4] Suffix text", "[1, 2, 3, 4]")]
        [InlineData("jusnk text {junk text text [inside \"{notAJson\": \"stillNot\"} ] string\"} here {\"valid\": true}", "{\"valid\": true}")]
        [InlineData("Some text... {\"key\":\"He said: \\\"Hello\\\"\"} end.", "{\"key\":\"He said: \\\"Hello\\\"\"}")]
        [InlineData("Lorem {\"outer\": {\"inner\": [1, 2, 3]}} ipsum", "{\"outer\": {\"inner\": [1, 2, 3]}}")]
        public void ExtractJson_ReturnsValidJson_WhenCorrectFragmentExists(string input, string expected)
        {
            var result = JsonExtractor.ExtractJson(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("This is not valid JSON: { text only")]
        public void ExtractJson_ReturnsEmptyString_WhenJsonIsNotFound(string input)
        {
            var result = JsonExtractor.ExtractJson(input);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ExtractJson_ReturnsFirstJson_WhenMultipleJsonsExist()
        {
            var firstJson = "{\"first\": true}";
            var secondJson = "{\"second\": false}";
            var input = $"prefix {firstJson} middle {secondJson} suffix";

            var result = JsonExtractor.ExtractJson(input);

            Assert.Equal(firstJson, result);
        }
    }
}
