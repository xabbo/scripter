using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MaterialDesignThemes.Wpf;

using Xabbo.Messages;
using Xabbo.Interceptor;
using Xabbo.GEarth;

using Xabbo.Core.Game;

using Xabbo.Scripter.Services;
using Xabbo.Scripter.View;
using Xabbo.Scripter.Engine;

namespace Xabbo.Scripter
{
    public partial class App : Application
    {
        private static readonly Dictionary<string, string> _switchMappings = new()
        {
            ["-i"] = "Xabbo:Interceptor:Service",
            ["-p"] = "Xabbo:Interceptor:Port",
            ["-c"] = "Xabbo:Interceptor:Cookie",
            ["-f"] = "Xabbo:Interceptor:File"
        };

        private IHost _host = null!;
        private Mutex? _mutex;

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

            GEarthOptions gEarthOptions = _host.Services.GetRequiredService<GEarthOptions>();

            _mutex = new Mutex(false, $"Xabbo.Scripter:{gEarthOptions.Port}");

            if (_mutex.WaitOne(0, false))
            {
                _host.Start();
            }
            else
            {
                MessageBox.Show(
                    $"An instance of the scripter is already running for port {gEarthOptions.Port}.",
                    "xabbo scripter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _mutex?.Close();
            _mutex = null;
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
            services.AddSingleton<IUiContext, WpfContext>();
            services.AddSingleton(Dispatcher);
            services.AddSingleton<ILoggerProvider, ObservableLoggerProvider>();
            services.AddSingleton<ISnackbarMessageQueue, SnackbarMessageQueue>();

            // Interceptor
            string interceptorService = context.Configuration.GetValue("Xabbo:Interceptor:Service", "G-Earth").ToLower();
            
            switch (interceptorService)
            {
                case "g-earth":
                    services.AddSingleton(GEarthOptions.Default
                        .WithInformationalVersion()
                        .WithName("xabbo scripter")
                        .WithDescription("C# scripting interface")
                        .WithAuthor("b7")
                        .WithConfiguration(context.Configuration)
                    );
                    services.AddSingleton<ScripterExtension>();
                    services.AddSingleton<IInterceptor>(provider => provider.GetRequiredService<ScripterExtension>());
                    services.AddSingleton<IRemoteInterceptor>(provider => provider.GetRequiredService<ScripterExtension>());
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

            foreach (Type type in GameStateManager.GetManagerTypes())
            {
                Debug.WriteLine($"Registering game state manager: {type.Name}");
                services.AddSingleton(type);
            }

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
