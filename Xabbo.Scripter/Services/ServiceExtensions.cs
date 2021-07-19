using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Xabbo.Scripter.Services
{
    internal static class ServiceExtensions
    {
        public static void AddHostedServiceSingleton<TService, THostedService>(this IServiceCollection services)
            where TService : class
            where THostedService : class, TService, IHostedService
        {
            services.AddSingleton<TService, THostedService>();
            services.AddHostedService(provider => (THostedService)provider.GetRequiredService<TService>());
        }
    }
}
