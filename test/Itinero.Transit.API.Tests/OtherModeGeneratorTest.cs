using Itinero.Transit.Api.Logic;
using Xunit;

namespace Test
{
    public class OtherModeGeneratorTest
    {

        [Fact]
        public void BuilderTest()
        {
            var builder = new OtherModeBuilder();
            builder.SupportedUrls();

        }
        
    }
}