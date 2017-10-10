using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceFabric.Mocks;

namespace ServiceFabric.Dictionary.Tests
{
    /// <summary>
    /// Tests a stateful service, by using a Mock Statemanager.
    /// </summary>
    [TestClass]
    public class DictionaryServiceTests
    {
        [TestMethod]
        public async Task Test_DictionaryService_Lookup()
        {
            //arrange
            var serviceInstance = new DictionaryService.DictionaryService(MockStatefulServiceContextFactory.Default, new MockReliableStateManager());
            const string testmeaning = "testmeaning";
            const string testword = "testword";

            //act
            await serviceInstance.Add(testword, testmeaning).ConfigureAwait(false);
            var actual = await serviceInstance.Lookup(testword).ConfigureAwait(false);

            //assert
            Assert.AreEqual(testmeaning, actual);
        }
    }
}
