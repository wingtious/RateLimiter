using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RateLimiterCore.Test
{
    public static class ServiceProviderExtensions
    {
        private static ServiceProvider _serviceProvider;

        /// <summary>Creates the scope.</summary>
        /// <returns>IServiceScope.</returns>
        public static IServiceScope CreateScope()
        {
            if (_serviceProvider == null)
            {
                throw new Exception("serviceProvider is null");
            }

            return _serviceProvider.CreateScope();
        }

        /// <summary>Sets the service provider.</summary>
        /// <param name="serviceCollection">The service collection.</param>
        public static void SetServiceProvider(IServiceCollection serviceCollection)
        {
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }
    }
}
