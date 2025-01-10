using System.Text.Json;
using Xunit.Abstractions;

namespace DimonSmart.AiUtils.Tests
{
    public class FuzzyJsonExtractorTests(ITestOutputHelper output)
    {
        private static readonly Random _rand = new Random();
        private readonly ITestOutputHelper _output = output;

        [Fact]
        [Trait("Category", "Manual")]
        public void FuzzyTest_ExtractJson()
        {
            for (var iteration = 0; iteration < 100000; iteration++)
            {
                var prefix = RandomString(Random.Shared.Next(32));
                var randomArray = new object[]
                {
                    _rand.Next(2) == 0,
                    RandomStringWithSpecials(Random.Shared.Next(32)),
                    Enumerable.Range(0, _rand.Next(1, 6))
                              .Select(_ => _rand.Next(-100, 101))
                              .ToArray()
                };
                var json = JsonSerializer.Serialize(randomArray);
                var suffix = RandomString(Random.Shared.Next(32));
                var input = prefix + json + suffix;


                var extractedJsons = JsonExtractor.ExtractAllJsons(input);
                var wrappedJson = $"[{json}]";

                _output.WriteLine($"Iteration: {iteration + 1}");
                _output.WriteLine($"Prefix:    {prefix}");
                _output.WriteLine($"JSON:      {json}");
                _output.WriteLine($"Wrapped:   {wrappedJson}");
                _output.WriteLine($"Suffix:    {suffix}");
                _output.WriteLine($"Extracted: {string.Join(", ", extractedJsons)}");

                Assert.Contains(extractedJsons, e => e == json || e == wrappedJson);
            }
        }

        private static string RandomString(int length)
        {
            const string chars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+= <>?:;,.{}[]\"\\/";
            return new string(Enumerable.Range(0, length)
                                        .Select(_ => chars[_rand.Next(chars.Length)])
                                        .ToArray());
        }

        private static string RandomStringWithSpecials(int length)
        {
            const string trickyChars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 \t\r\n\"'{}[]!@#$%^&*()\\/";
            return new string(Enumerable.Range(0, length)
                                        .Select(_ => trickyChars[_rand.Next(trickyChars.Length)])
                                        .ToArray());
        }
    }
}
