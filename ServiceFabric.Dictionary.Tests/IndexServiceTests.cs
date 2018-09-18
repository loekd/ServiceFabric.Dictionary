using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceFabric.Mocks;

namespace ServiceFabric.Dictionary.Tests
{
    /// <summary>
    /// Tests a stateless service, by using a Mock ServiceProxyFactory.
    /// </summary>
    [TestClass]
    public partial class IndexServiceTests
    {
        /// <summary>
        /// Prepare: open IndexService, put breakpoint at 'Add' and 'Lookup'.
        /// Debug this test, to show the code works in cluster and unit tests, without changes to the service.
        /// This is because the proxy factory and the state manager are mocked and injected.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_IndexService_Lookup()
        {
            const string testmeaning = "testmeaning";
            const string testword = "testword";
            //arrange

            //fake dictionary svc
            var dictionaryServiceInstance = new DictionaryService.DictionaryService(
                MockStatefulServiceContextFactory.Default, 
                new MockReliableStateManager());
            var dictionaryServiceUri = new Uri("fabric:/ServiceFabric.Dictionary/ServiceFabric.Dictionary.DictionaryService");

            //register dictionary service under its cluster uri with mock
            var mockServiceProxyFactory = new MockServiceProxyFactory();
            mockServiceProxyFactory.RegisterService(dictionaryServiceUri, dictionaryServiceInstance);

            //inject mock proxy factory into index service
            var indexServiceInstance = new IndexService.IndexService(
                MockStatelessServiceContextFactory.Default, 
                mockServiceProxyFactory, new MockHashingHelper());

            //act
            await indexServiceInstance.Add(testword, testmeaning);
            var actual = await indexServiceInstance.Lookup(testword);

            //assert
            Assert.AreEqual(testmeaning, actual);
        }
    }
}