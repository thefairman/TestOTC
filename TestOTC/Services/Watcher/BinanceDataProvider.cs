using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Services.Watcher
{
	public interface IBinanceDataProvider
	{
		IBinanceBookPrice LastBookTicker { get; }
		Action<IBinanceBookPrice> OnBookTickerData { get; set; }

		Task Start();
		Task Stop();
	}

	public class BinanceDataProvider : IBinanceDataProvider
	{
		private readonly IBinanceSocketClient _socketClient;
		private UpdateSubscription _subscription;
		private readonly TrancientWatcherServiceOptions _options;


		public IBinanceBookPrice LastBookTicker { get; private set; }
		public Action<IBinanceBookPrice> OnBookTickerData { get; set; }

		public BinanceDataProvider(IBinanceSocketClient socketClient, IOptions<TrancientWatcherServiceOptions> options)
		{
			_socketClient = socketClient;
			_options = options.Value;

			//Start().Wait(); // Probably want to do this in some initialization step at application startup
		}

		public async Task Start()
		{
			//var subResult = await _socketClient.SpotStreams.SubscribeToKlineUpdatesAsync("BTCUSDT", KlineInterval.FifteenMinutes, data =>
			//{
			//	LastKline = data.Data;
			//	OnKlineData?.Invoke(data.Data);
			//});
			var subResult = await _socketClient.SpotStreams.SubscribeToAllBookTickerUpdatesAsync(data =>
			{
				var reData = data.Data;
				if (reData.Symbol != _options.Symbol)
					return;
				OnBookTickerData?.Invoke(data.Data);
			});

			if (subResult.Success)
				_subscription = subResult.Data;
		}

		public async Task Stop()
		{
			await _socketClient.UnsubscribeAsync(_subscription);
		}
	}
}
