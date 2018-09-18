using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceFabric.Mocks;

namespace ServiceFabric.Dictionary.Tests
{
    /// <summary>
    /// Tests a stateful service, by using a Mock State manager.
    /// </summary>
    [TestClass]
    public class DictionaryServiceTests
    {
        [TestMethod]
        public async Task Test_DictionaryService_Lookup()
        {
            const string testmeaning = "testmeaning";
            const string testword = "testword";

            //arrange
            var serviceInstance = new DictionaryService.DictionaryService(
                MockStatefulServiceContextFactory.Default, 
                new MockReliableStateManager());
            
            //act
            await serviceInstance.Add(testword, testmeaning);
            var actual = await serviceInstance.Lookup(testword);

            //assert
            Assert.AreEqual(testmeaning, actual);
        }
    }
}
