using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.GEarth;

using Xabbo.Scripter.Services;
using Xabbo.Scripter.View;
using Xabbo.Scripter.Engine;

namespace Xabbo.Scripter
{
    public partial class App : Application
    {
        private static readonly Dictionary<string, string> _switchMappings = new()
        {
            ["-p"] = "Xabbo:Interceptor:Port",
            ["-s"] = "Xabbo:Interceptor:Service"
        };

        private IHost _host = null!;

        public App() { }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) => {
                    ConfigureAppConfiguration(context, config);
                    config.AddCommandLine(e.Args, _switchMappings);
                })
                .ConfigureServices(ConfigureServices)
                .Build();

            _host.Start();
        }

        private void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder config)
        {
            
        }

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // Application
            services.AddSingleton<IHostLifetime, WpfLifetime>();
            services.AddSingleton<Application>(this);
            services.AddSingleton<Window, MainWindow>();
            services.AddSingleton<IUIContext, WpfContext>();
            services.AddSingleton(Dispatcher);
            services.AddSingleton<ILoggerProvider, ObservableLoggerProvider>();

            // Interceptor
            string interceptorService = context.Configuration.GetValue("Xabbo:Interceptor:Service", "g-earth").ToLower();

            switch (interceptorService)
            {
                case "g-earth":
                    services.AddSingleton(new GEarthOptions
                    {
                        Author = "b7",
                        Title = "xabbo scripter",
                        Description = "C# scripting interface",
                        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?"
                    });
                    services.AddSingleton<GEarthExtension>();
                    services.AddSingleton<IInterceptor>(provider => provider.GetRequiredService<GEarthExtension>());
                    services.AddSingleton<IRemoteInterceptor>(provider => provider.GetRequiredService<GEarthExtension>());
                    break;
                default:
                    throw new Exception($"Unknown interceptor service: '{interceptorService}'.");
            }

            // Web
            services.AddSingleton<IUriProvider<HabboEndpoints>, HabboUriProvider>();
            services.AddHttpClient("Xabbo")
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", context.Configuration.GetValue<string>("Web:UserAgent"));
                });

            // Game management
            services.AddSingleton<IMessageManager, UnifiedMessageManager>();
            services.AddSingleton<IGameDataManager, GameDataManager>();
            services.AddSingleton<IGameManager, GameManager>();

            // Scripting
            services.AddSingleton<IScriptHost, ScriptHost>();
            services.AddSingleton<ScriptEngine>();

            // View managers
            Type[] localAssemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in localAssemblyTypes.Where(
                x => x.Namespace == "Xabbo.Scripter.ViewModel" && x.Name.EndsWith("ViewManager")
            ))
            {
                Debug.WriteLine($"Registering view manager: {type.Name}");
                services.AddSingleton(type);
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }
    }
}
