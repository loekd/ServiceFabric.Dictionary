using System;
using System.Collections.Generic;
using System.Fabric;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.Dictionary.DictionaryService;

[assembly: FabricTransportServiceRemotingProvider(RemotingListener = RemotingListener.V2Listener, RemotingClient = RemotingClient.V2Client)]

namespace ServiceFabric.Dictionary.IndexService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class IndexService : StatelessService, IIndexService
    {
        private readonly IHashingHelper _hashingHelper;
        private readonly IServiceProxyFactory _dictionaryServiceProxyFactory;
        private static readonly Uri DictionaryServiceUri = new Uri("fabric:/ServiceFabric.Dictionary/ServiceFabric.Dictionary.DictionaryService");

        public IndexService(StatelessServiceContext context, IServiceProxyFactory dictionaryServiceProxyFactory, IHashingHelper hashingHelper)
            : base(context)
        {
            _hashingHelper = hashingHelper ?? throw new ArgumentNullException(nameof(hashingHelper));
            _dictionaryServiceProxyFactory = dictionaryServiceProxyFactory ?? throw new ArgumentNullException(nameof(dictionaryServiceProxyFactory));
        }

        /// <summary>
        /// Create remoting listeners.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
        
        /// <inheritdoc />
        public Task Add(string word, string meaning)
        {
            long partitionKey = PartitionAddressFromWord(word);
            var proxy = _dictionaryServiceProxyFactory.CreateServiceProxy<IDictionaryService>(DictionaryServiceUri, new ServicePartitionKey(partitionKey), TargetReplicaSelector.PrimaryReplica, DictionaryServiceListenerSettings.RemotingListenerName);
            return proxy.Add(word, meaning);
        }
        
        /// <inheritdoc />
        public Task<string> Lookup(string word)
        {
            long partitionKey = PartitionAddressFromWord(word);
            var proxy = _dictionaryServiceProxyFactory.CreateServiceProxy<IDictionaryService>(DictionaryServiceUri, new ServicePartitionKey(partitionKey), TargetReplicaSelector.PrimaryReplica, DictionaryServiceListenerSettings.RemotingListenerName);
            return proxy.Lookup(word);
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

    public interface IIndexService : IService
    {
        /// <summary>
        /// Adds a word to the dictionary.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="meaning"></param>
        /// <returns></returns>
        Task Add(string word, string meaning);

        /// <summary>
        /// Returns meaning of word.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        Task<string> Lookup(string word);
    }
}
