using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using ServiceFabric.Dictionary.DictionaryService;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace ServiceFabric.Dictionary.IndexApiService.Controllers
{
    [Route("api/[controller]")]
    public class IndexController : Controller
    {
        private readonly IHashingHelper _hashingHelper;
        private readonly IServiceProxyFactory _dictionaryServiceProxyFactory;
        private static readonly Uri DictionaryServiceUri = new Uri("fabric:/ServiceFabric.Dictionary/ServiceFabric.Dictionary.DictionaryService");

        public IndexController(IServiceProxyFactory dictionaryServiceProxyFactory, IHashingHelper hashingHelper)
        {
            _hashingHelper = hashingHelper ?? throw new ArgumentNullException(nameof(hashingHelper));
            _dictionaryServiceProxyFactory = dictionaryServiceProxyFactory ?? throw new ArgumentNullException(nameof(dictionaryServiceProxyFactory));
        }

        [HttpGet]
        public async Task<IActionResult> Get(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return BadRequest("Querystring parameter 'word' is missing");
            long partitionKey = PartitionAddressFromWord(word);
            var proxy = _dictionaryServiceProxyFactory.CreateServiceProxy<IDictionaryService>(DictionaryServiceUri, new ServicePartitionKey(partitionKey), TargetReplicaSelector.Default, DictionaryServiceListenerSettings.RemotingListenerName);
            string meaning = await proxy.Lookup(word).ConfigureAwait(true);
            return Ok(new
            {
                word,
                meaning
            });
        }

        [HttpPost]
        public async Task<IActionResult> Post(string word, string meaning)
        {
            if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(meaning))
                return BadRequest("Querystring parameter 'word' or 'meaning' is missing");

            long partitionKey = PartitionAddressFromWord(word);
            var proxy = _dictionaryServiceProxyFactory.CreateServiceProxy<IDictionaryService>(DictionaryServiceUri, new ServicePartitionKey(partitionKey), TargetReplicaSelector.Default, DictionaryServiceListenerSettings.RemotingListenerName);
            await proxy.Add(word, meaning).ConfigureAwait(true);
            return Accepted(new
            {
                word,
                meaning
            });
        }

        /// <summary>
        /// Uses a hashing algorithm with a good distribution to generate an int64 from a string.
        /// This int64 can then be used to address a partition.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private long PartitionAddressFromWord(string word)
        {
            return _hashingHelper.HashString(word);
        }
    }
}
