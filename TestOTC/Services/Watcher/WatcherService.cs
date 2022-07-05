using AutoMapper;
using Binance.Net.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TestOTC.Entities.Directus;
using TestOTC.Services.ExternalServices.HttpServices.Directus;
using TestOTC.Services.Selenium;
using TestOTC.Services.TitaniumProxy;

namespace TestOTC.Services.Watcher
{
	internal class WatcherService
	{
		private readonly DirectusService _directusServise;
		private readonly ILogger _logger;
		private readonly TrancientWatcherServiceOptions _options;
		private readonly IBinanceDataProvider _dataProvider;
		//private readonly ManualResetEventSlim _manualResetEvent;
		private readonly SeleniumService _browserFirstWay;
		private readonly SeleniumService _browserSecondWay;
		private readonly List<BookOrderEntity> _orderBook;
		private readonly IMapper _mapper;

		public WatcherService(DirectusService directusServise,
						ILogger<WatcherService> logger,
						IOptions<TrancientWatcherServiceOptions> options,
						IBinanceDataProvider dataProvider,
						IMapper mapper,
						TitaniumProxyService titaniumProxyService)
		{
			_directusServise = directusServise;
			_logger = logger;
			_dataProvider = dataProvider;
			_mapper = mapper;
			_options = options.Value;
			//_manualResetEvent = new ManualResetEventSlim();
			_dataProvider.OnBookTickerData += OnBookTickerData;
			//_browserFirstWay = new SeleniumService(_options.From, _options.To, titaniumProxyService);
			//_browserSecondWay = new SeleniumService(_options.To, _options.From, titaniumProxyService);
			_browserFirstWay = new SeleniumService(titaniumProxyService);
			_browserSecondWay = new SeleniumService(titaniumProxyService);
			_orderBook = new List<BookOrderEntity>();
		}

		private static object lockList = new object();
		private void OnBookTickerData(IBinanceBookPrice bookPrice)
		{
			lock (lockList)
			{
				var last = _orderBook.LastOrDefault();
				if (last != null && bookPrice.BestBidPrice == last.BestBidPrice && bookPrice.BestAskPrice == last.BestAskPrice)
					return;
				_orderBook.Add(_mapper.Map<BookOrderEntity>(bookPrice));
			}
			//_orderBook.Enqueue(_mapper.Map<BookOrderEntity>(bookPrice));
			//var options = new JsonSerializerOptions { WriteIndented = true };
			//_logger.LogInformation(JsonSerializer.Serialize(bookPrice, options));
		}

		public async Task RunAsync(CancellationToken cancellationToken)
		{

			_browserFirstWay.LaunchBrowser();
			_browserSecondWay.LaunchBrowser();
			// only for test, move this stuff to MainService or remove or edit or not?
			while (true)
			{
				lock (lockList)
				{
					_orderBook.Clear();
				}
				var fq = _browserFirstWay.GetQuote();
				var firTimeStart = DateTime.UtcNow;
				var sq = _browserSecondWay.GetQuote();
				var secTimeStart = DateTime.UtcNow;
				//_manualResetEvent.Wait();
				var deelay = (sq.ExpiredTime != DateTime.MinValue ? sq.ExpiredTime.AddSeconds(1) : secTimeStart.AddSeconds(6)) - DateTime.UtcNow;
				if (deelay > TimeSpan.Zero)
					Thread.Sleep(deelay);
				var iterationOrders = new List<BookOrderEntity>(_orderBook);

				//var orders = _orderBook.ToList();
				// todo: check for profitability
				//await _directusServise.CreateOrderBooks(orders, cancellationToken);
				string fWatcherId = Guid.NewGuid().ToString();
				string sWatcherId = Guid.NewGuid().ToString();
				await _directusServise.CreateOrderBooks(iterationOrders, cancellationToken);
				await _directusServise.CreateWatchers(new List<WatcherEntity> {
					new WatcherEntity
					{
						Id = fWatcherId,
						From =  fq.FromCoin!,
						To = fq.ToCoin!,
						StartTime = firTimeStart,
						Symbol = _options.Symbol,
						ExpireTime = fq.ExpiredTime,
						InversePrice = fq.InversePrice,
						QuotePrice = fq.QuotePrice,
					},
					new WatcherEntity
					{
						Id = sWatcherId,
						From =  sq.FromCoin!,
						To = sq.ToCoin!,
						StartTime = secTimeStart,
						Symbol = _options.Symbol,
						ExpireTime = sq.ExpiredTime,
						InversePrice = sq.InversePrice,
						QuotePrice = sq.QuotePrice,
					}
				}, cancellationToken);
				List<WatcherBookOrderEntity> watcherbookordres = new();
				foreach (var item in iterationOrders)
				{
					watcherbookordres.Add(new WatcherBookOrderEntity
					{
						BookOrdersId = item.Id,
						WatchersId = fWatcherId
					});
					watcherbookordres.Add(new WatcherBookOrderEntity
					{
						BookOrdersId = item.Id,
						WatchersId = sWatcherId
					});
				}
				await _directusServise.CreateWatcherBookOrders(watcherbookordres, cancellationToken);
				await Task.Delay(TimeSpan.FromSeconds(40));
			}
		}


	}
}
