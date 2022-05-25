using TestOTC.Services.Watcher;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestOTC.Services.Selenium;

namespace TestOTC.Services
{
	internal class MainService
	{
		internal class MainWorkerService : IHostedService
		{
			private readonly ILogger _logger;
			private readonly WatcherService _watcherService;
			//private readonly SeleniumService _seleniumService;
			private readonly IBinanceDataProvider _dataProvider;

			public MainWorkerService(ILogger<MainWorkerService> logger, WatcherService watcherService, IBinanceDataProvider dataProvider/*, SeleniumService seleniumService*/)
			{
				_logger = logger;
				_watcherService = watcherService;
				_dataProvider = dataProvider;
				//_seleniumService = seleniumService;
			}

			public async Task StartAsync(CancellationToken cancellationToken)
			{
				try
				{
					//_seleniumService.LaunchBrowser();
					await _dataProvider.Start();
					await _watcherService.RunAsync(cancellationToken);
				}
				catch (Exception e)
				{
					_logger.LogCritical(e, e.Message);
				}
			}

			public Task StopAsync(CancellationToken cancellationToken) => _dataProvider.Stop();
		}
	}
}
