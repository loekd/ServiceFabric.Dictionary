using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ServiceFabric.Dictionary.IndexApiService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class IndexApiService : StatelessService
    {
        private readonly IHashingHelper _hashingHelper;
        private readonly IServiceProxyFactory _dictionaryServiceProxyFactory;

        public IndexApiService(StatelessServiceContext context, IServiceProxyFactory dictionaryServiceProxyFactory,
            IHashingHelper hashingHelper)
            : base(context)
        {
            _hashingHelper = hashingHelper ?? throw new ArgumentNullException(nameof(hashingHelper));
            _dictionaryServiceProxyFactory = dictionaryServiceProxyFactory ?? throw new ArgumentNullException(nameof(dictionaryServiceProxyFactory));
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services =>
                                        {
                                            services.AddSingleton(serviceContext);
                                            services.AddSingleton(_ => _dictionaryServiceProxyFactory);
                                            services.AddSingleton(_ => _hashingHelper);
                                        })
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };
        }
    }
}
