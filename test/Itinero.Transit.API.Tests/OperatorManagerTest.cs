using System.Collections.Generic;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Data;
using Xunit;

namespace Itinero.Transit.API.Tests
{
    public class OperatorManagerTest
    {
        [Fact]
        public static void GetView_FewQueries_ReturnsOperators()
        {
            var operatorManager = new OperatorManager(
                new List<Operator>
                {
                    new Operator("a", new TransitDb(0), null, 1000, new[] {"altA", "altAA"}, new[] {"tag", "tag0"}),
                    new Operator("b", new TransitDb(1), null, 5000, new[] {"altB", "altBB"}, new[] {"tag", "tag0"}),
                    new Operator("c", new TransitDb(2), null, 10000, new[] {"altC", "altCC"}, new[] {"tag", "tag1"}),
                    new Operator("d", new TransitDb(3), null, 1000, new[] {"altD", "altDD"}, new[] {"tag", "tag1"})
                }
            );

            var all = operatorManager.GetFullView();
            Assert.Equal(4, all.Operators.Count);


            var a = operatorManager.GetView("a");
            Assert.Single(a.Operators);
            Assert.Equal("a", a.Operators[0].Name);

            a = operatorManager.GetView("A"); // Upper case
            Assert.Single(a.Operators);
            Assert.Equal("a", a.Operators[0].Name);

            a = operatorManager.GetView("altA"); // Alt name
            Assert.Single(a.Operators);
            Assert.Equal("a", a.Operators[0].Name);


            var cd = operatorManager.GetView("tag1");
            Assert.Equal(2, cd.Operators.Count);
            Assert.Equal("c", cd.Operators[0].Name);
            Assert.Equal("d", cd.Operators[1].Name);

            var bcd = operatorManager.GetView("b;tag1");
            Assert.Equal(3, bcd.Operators.Count);
            Assert.Equal("b", bcd.Operators[0].Name);
            Assert.Equal("c", bcd.Operators[1].Name);
            Assert.Equal("d", bcd.Operators[2].Name);
        }

        [Fact]
        public static void GetView_FewQueries_ViewIsCached()
        {
            var operatorManager = new OperatorManager(
                new List<Operator>
                {
                    new Operator("a", new TransitDb(0), null, 1000, new[] {"altA", "altAA"}, new[] {"tag", "tag0"}),
                    new Operator("b", new TransitDb(1), null, 5000, new[] {"altB", "altBB"}, new[] {"tag", "tag0"}),
                    new Operator("c", new TransitDb(2), null, 10000, new[] {"altC", "altCC"}, new[] {"tag", "tag1"}),
                    new Operator("d", new TransitDb(3), null, 1000, new[] {"altD", "altDD"}, new[] {"tag", "tag1"})
                }
            );

            var a0 = operatorManager.GetView("a");
            var a1 = operatorManager.GetView("altA");

            // Important: we use '==' to check for REFERENCE equality, not for content equality
            // It has to be the same object!
            Assert.True(a0 == a1);

            a0 = operatorManager.GetView("a");
            a1 = operatorManager.GetView("altA;nonExisting");

            Assert.True(a0 == a1);


            var tag0 = operatorManager.GetView("tag0");
            var ab = operatorManager.GetView("a;b");
            
            Assert.True(tag0 == ab);
        }
    }
}