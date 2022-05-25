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
						IMapper mapper)
		{
			_directusServise = directusServise;
			_logger = logger;
			_dataProvider = dataProvider;
			_mapper = mapper;
			_options = options.Value;
			//_manualResetEvent = new ManualResetEventSlim();
			_dataProvider.OnBookTickerData += OnBookTickerData;
			_browserFirstWay = new SeleniumService(_options.From, _options.To);
			_browserSecondWay = new SeleniumService(_options.To, _options.From);
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
				_orderBook.Clear();
				var fq = _browserFirstWay.GetQuote();
				var firTimeStart = DateTime.Now;
				var sq = _browserSecondWay.GetQuote();
				var secTimeStart = DateTime.Now;
				//_manualResetEvent.Wait();
				var deelay = secTimeStart.AddSeconds(6) - DateTime.Now;
				if (deelay > TimeSpan.Zero)
					Thread.Sleep(deelay);
				//var orders = _orderBook.ToList();
				// todo: check for profitability
				//await _directusServise.CreateOrderBooks(orders, cancellationToken);
				await _directusServise.CreateOrderBooks(_orderBook, cancellationToken);
				await _directusServise.CreateWatchers(new List<WatcherEntity> {
					new WatcherEntity
					{ 
						From =  _browserFirstWay.From,
						To = _browserFirstWay.To,
						Quote = fq.ToRate,
						StartTime = firTimeStart,
						Symbol = _options.Pair
					},
					new WatcherEntity
					{
						From =  _browserSecondWay.From,
						To = _browserSecondWay.To,
						Quote = sq.FromRate,
						StartTime = secTimeStart,
						Symbol = _options.Pair
					}
				}, cancellationToken);
			}
		}


	}
}
