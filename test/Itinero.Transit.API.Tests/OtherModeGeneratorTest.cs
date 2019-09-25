using Itinero.Transit.Api.Logic.Transfers;
using Xunit;

namespace Itinero.Transit.API.Tests
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