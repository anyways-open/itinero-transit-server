using Itinero.Transit.Api.Logic.Search;
using Xunit;

namespace Itinero.Transit.API.Tests
{
    public class SmallTrieTests
    {
        [Fact]
        public void TestSmallTrie()
        {
            var trie = new SmallTrie<int>();

            trie.Add("foo", 5);
            trie.Add("bar", 6);
            trie.Add("force", 7);

            var v = trie.Find("foo");
            Assert.Equal(5, v);
            
            v = trie.Find("force");
            Assert.Equal(7, v);

            var f = trie.FindFuzzy("force", 5);
            Assert.Contains((7,0), f);


            f = trie.FindFuzzy("fo", 5);  
            Assert.Contains((7,3), f);
            Assert.Contains((5,1), f);

        }
    }
}