using WaFoLo.Utilities;

namespace WaFoLo.Tests
{
    public class TimestampParserFactoryTests
    {
        [Fact]
        public void Create_ReturnsTimestampParserInstance()
        {
            var factory = new TimestampParserFactory();
            var parser = factory.Create("yyyy-MM-dd HH:mm:ss");

            Assert.NotNull(parser);
            Assert.IsType<TimestampParser>(parser);
        }

        [Fact]
        public void Create_ReturnsDifferentInstancesOnEachCall()
        {
            var factory = new TimestampParserFactory();
            var parser1 = factory.Create("yyyy-MM-dd HH:mm:ss");
            var parser2 = factory.Create("yyyy-MM-dd HH:mm:ss");

            Assert.NotSame(parser1, parser2);
        }

        [Fact]
        public void Create_ParserIsUsable()
        {
            var factory = new TimestampParserFactory();
            var parser = factory.Create("yyyy-MM-dd HH:mm:ss");

            var result = parser.ExtractTimestamp("2024-01-01 00:00:00 startup");

            Assert.NotNull(result);
        }
    }
}
