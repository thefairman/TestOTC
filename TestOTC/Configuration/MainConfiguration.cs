using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients;
using TestOTC.Services.ExternalServices.HttpServices.Directus;
using TestOTC.Services.Watcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestOTC.Services.Watcher;
using TestOTC.Services.Selenium;
using TestOTC.Services.TitaniumProxy;

namespace TestOTC.Configuration
{
	internal static class MainConfiguration
	{

		public static IHostBuilder CreateHostBuilder<TService>(string[] args) where TService : class, IHostedService =>
		   Host.CreateDefaultBuilder(args)
			.ConfigureHostConfiguration(c =>
			{
				c.SetBasePath(AppContext.BaseDirectory);
				c.AddJsonFile("appsettings.json", optional: false);
			})
			.ConfigureAppConfiguration((hostingContext, config) =>
			{
				config.SetBasePath(AppContext.BaseDirectory);
				var env = hostingContext.HostingEnvironment;
				// comment below
				config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
			})
			.ConfigureServices((hostContext, services) =>
			{
				var configurationRoot = hostContext.Configuration;
				services.Configure<TrancientDirectusServiceOptions>(
					configurationRoot.GetSection(nameof(TrancientDirectusServiceOptions)));
				services.Configure<TrancientWatcherServiceOptions>(
					configurationRoot.GetSection(nameof(TrancientWatcherServiceOptions)));

				services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

				services.AddHostedService<TitaniumProxyService>();
				services.AddHostedService<TService>();
				services.AddHttpClient<DirectusService>().ConfigureHttpClient(10000, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5));
				services.AddSingleton<WatcherService>();
				//services.AddSingleton<SeleniumService>();

				services.AddSingleton<IBinanceSocketClient, BinanceSocketClient>();
				services.AddTransient<IBinanceClient, BinanceClient>();

				services.AddSingleton<IBinanceDataProvider, BinanceDataProvider>();
			});
	}
}
