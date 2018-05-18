using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

[assembly: FabricTransportServiceRemotingProvider(RemotingListener = RemotingListener.V2Listener, RemotingClient = RemotingClient.V2Client)]

namespace ServiceFabric.Dictionary.DictionaryService
{
    /// <summary>
    /// Stateful Dictionary service
    /// </summary>
    internal sealed class DictionaryService : StatefulService, IDictionaryService
    {
        private readonly IReliableStateManagerReplica2 _reliableStateManagerReplica;
        public const string MainDictionaryKey = nameof(MainDictionaryKey);

        /// <summary>
        /// Creates a new instance, using the provided <paramref name="context"/> and <paramref name="reliableStateManagerReplica"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reliableStateManagerReplica"></param>
        public DictionaryService(StatefulServiceContext context, IReliableStateManagerReplica2 reliableStateManagerReplica)
            : base(context, reliableStateManagerReplica)
        {
            _reliableStateManagerReplica = reliableStateManagerReplica;
        }

        /// <summary>
        /// Create V2 remoting listener
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            //return this.CreateServiceRemotingReplicaListeners();

            var listeners = new List<ServiceReplicaListener>();

            listeners.Add(new ServiceReplicaListener(serviceContext =>
                new KestrelCommunicationListener(serviceContext, (url, listener) =>
                {
                    ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                    return new WebHostBuilder()
                        .UseKestrel()
                        .ConfigureServices(
                            services =>
                            {
                                services.AddSingleton(serviceContext);
                                services.AddSingleton(_reliableStateManagerReplica);
                            })
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                        .UseUrls(url)
                        .Build();
                }), DictionaryServiceListenerSettings.WebListenerName));

            listeners.Add(new ServiceReplicaListener(serviceContext =>
                new FabricTransportServiceRemotingListener(serviceContext, this), DictionaryServiceListenerSettings.RemotingListenerName));

            return listeners;

        }

        /// <inheritdoc />
        public async Task Add(string word, string meaning)
        {
            if (string.IsNullOrWhiteSpace(word))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(word));
            if (string.IsNullOrWhiteSpace(meaning))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(meaning));

            var myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(MainDictionaryKey);

            using (var tx = StateManager.CreateTransaction())
            {
                await myDictionary.AddOrUpdateAsync(tx, word.ToUpperInvariant(), meaning, (key, old) => meaning);
                await tx.CommitAsync();
            }

            ServiceEventSource.Current.ServiceMessage(Context, $"{nameof(DictionaryService)} - Added or updated word '{word}' with meaning '{meaning}'.");
        }

        /// <inheritdoc />
        public async Task<string> Lookup(string word)
        {
            var myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(MainDictionaryKey);

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await myDictionary.TryGetValueAsync(tx, word.ToUpperInvariant());
                string meaning = result.HasValue ? result.Value : "<<<not found>>>";
                ServiceEventSource.Current.ServiceMessage(Context, $"{nameof(DictionaryService)} - Looked up word '{word}', meaning: '{meaning}'.");

                return meaning;
            }
        }
    }

    public interface IDictionaryService : IService
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
