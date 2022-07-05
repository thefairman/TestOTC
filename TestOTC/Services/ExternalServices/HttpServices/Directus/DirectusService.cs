using AutoMapper;
using TestOTC.Entities.Directus.WebDto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TestOTC.Entities.Directus;

namespace TestOTC.Services.ExternalServices.HttpServices.Directus
{
	internal class DirectusService : BaseHttpServise
	{
		private const string OrdersCollection = "book_orders";
		private const string WatchersCollection = "watchers";
		private const string WatcherBookOrderCollection = "watchers_book_orders";

		private readonly ILogger _logger;
		private readonly IMapper _mapper;
		private readonly TrancientDirectusServiceOptions _options;
		private readonly JsonSerializerOptions _jsonOptions;
		public DirectusService(HttpClient httpClient, ILogger<DirectusService> logger, IMapper mapper, IOptions<TrancientDirectusServiceOptions> options)
			: base(httpClient)
		{
			_options = options.Value;
			// todo: uncomment bellow
			//_httpClient.BaseAddress = new Uri("https://miners-staging.wattum.pro");
			_httpClient.BaseAddress = new Uri(_options.BaseUrl);
			//_httpClient.Timeout = TimeSpan.FromSeconds(60); i suppose i configure this value via configurations
			_httpClient.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", _options.SecretKey);
			_logger = logger;
			_mapper = mapper;

			_jsonOptions = new JsonSerializerOptions();
			_jsonOptions.Converters.Add(new DateTimeConverterUsingDateTimeParse());

		}

		public class DateTimeConverterUsingDateTimeParse : JsonConverter<DateTime>
		{
			public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				var date = DateTime.Parse(reader.GetString() ?? string.Empty);
				return DateTime.SpecifyKind(date, DateTimeKind.Utc);
			}

			public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
			{
				writer.WriteStringValue(value.ToString("o"));
			}
		}

		private async Task<(List<T> Data, int? Count)> GetItems<T>(
			string collection,
			int? offset = null,
			int? limit = null,
			string sort = null,
			Dictionary<string, string> restrictions = null,
			IEnumerable<string> fields = null,
			Dictionary<string, IEnumerable<object>>? inFilters = null)
		{
			string url = $"items/{collection}?meta=filter_count&";

			if (limit != null) url += $"limit={limit}&";
			if (offset != null) url += $"offset={offset}&";
			if (sort != null) url += $"sort={sort}&";
			if (restrictions != null && restrictions.Count > 0)
			{
				foreach (var restr in restrictions)
					url += $"filter[{restr.Key}][_eq]={restr.Value}&";
			}
			if (fields != null && fields.Any())
			{
				url += $"fields={string.Join(",", fields)}&";
			}
			if (inFilters != null && inFilters.Count > 0)
			{
				foreach (var inFilter in inFilters)
					url += $"filter[{inFilter.Key}][_in]={string.Join(",", inFilter.Value)}&";
			}
			var resp = await _httpClient.GetAsync(url);
			var httpResponseBody = await ReadResponseAndEnsureIsOk(resp, offset);
			//var data = await _httpClient.GetFromJsonAsync<DirectusRoot<List<T>>>(url, _jsonOptions);
			var data = JsonSerializer.Deserialize<DirectusRoot<List<T>>>(httpResponseBody);
			return (data?.data, data?.meta?.filter_count);
		}

		private async Task<T> CreateItem<T>(string collection, T item, int? limit = null, int? successfullOffset = null)
		{
			//var wc = newWebClient();
			var url = $"items/{collection}?";
			url += $"limit={(limit ?? -1)}&";
			var resp = await _httpClient.PostAsJsonAsync(url, item);

			var httpResponseBody = await ReadResponseAndEnsureIsOk(resp, successfullOffset);

			//string resp = await wc.UploadStringTaskAsync($"{adrs}/items/{collection}",
			//		   JsonSerializer.Serialize(item));
			var data = JsonSerializer.Deserialize<DirectusRoot<T>>(httpResponseBody, _jsonOptions);
			return data.data;
		}

		private async Task CreateItemNoReurtn<T>(string collection, T item, CancellationToken cancellationToken, int? successfullOffset = null)
		{
			var url = $"items/{collection}?limit=0";
			var resp = await _httpClient.PostAsJsonAsync(url, item, /*_jsonOptions,*/ cancellationToken);
			//var tmp = (item as List<BookOrderDto>)?[0].local_timestamp.ToString("o");
			var httpResponseBody = await ReadResponseAndEnsureIsOk(resp, successfullOffset);
			var data = JsonSerializer.Deserialize<DirectusRoot<T>>(httpResponseBody, _jsonOptions);
		}

		private async Task<T> UpdateItem<T>(
			string collection,
			T item,
			string? id = null,
			JsonConverter? jsonConverter = null,
			int? successfullOffset = null)
		{
			string url = $"items/{collection}/{id ?? (item as BaseDirectusDto)?.id}";
			var serializedDoc = JsonSerializer.Serialize(item);
			var requestContent = new StringContent(serializedDoc, Encoding.UTF8, "application/json");
			var resp = await _httpClient.PatchAsync(url, requestContent);
			var httpResponseBody = await ReadResponseAndEnsureIsOk(resp, successfullOffset);
			var options = _jsonOptions;
			if (jsonConverter != null)
			{
				options = new JsonSerializerOptions();
				options.Converters.Add(jsonConverter);
				options.Converters.Add(new DateTimeConverterUsingDateTimeParse());
			}
			var data = JsonSerializer.Deserialize<DirectusRoot<T>>(httpResponseBody, options);
			return data.data;
		}

		private async Task UpdateItemNoReturn<T>(
			string collection,
			T item,
			string? id = null,
			JsonConverter? jsonConverter = null,
			CancellationToken cancellationToken = default,
			int? successfullOffset = null)
		{
			string url = $"items/{collection}/{id ?? (item as BaseDirectusDto)?.id}?limit=0";
			JsonSerializerOptions? options = null;
			if (jsonConverter != null)
			{
				options = new JsonSerializerOptions();
				options.Converters.Add(jsonConverter);
			}
			var serializedDoc = JsonSerializer.Serialize(item, options);
			var requestContent = new StringContent(serializedDoc, Encoding.UTF8, "application/json");
			var resp = await _httpClient.PatchAsync(url, requestContent, cancellationToken);
			await ReadResponseAndEnsureIsOk(resp, successfullOffset);
		}

		private static async Task<string> ReadResponseAndEnsureIsOk(HttpResponseMessage response, int? successfullOffset)
		{
			string content = await response.Content.ReadAsStringAsync();
			if (response.IsSuccessStatusCode)
				return content;
			string message = "directus error";
			DirectusExceptionExtensions? extensions = null;
			try
			{
				if (!string.IsNullOrWhiteSpace(content))
				{
					var jsonElem = JsonSerializer.Deserialize<JsonElement>(content);
					if (jsonElem.TryGetProperty("errors", out var errorElem))
					{
						var firstError = errorElem.EnumerateArray().FirstOrDefault();
						message = firstError.GetProperty("message").ToString() ?? string.Empty;
						var options = new JsonSerializerOptions
						{
							PropertyNameCaseInsensitive = true
						};
						extensions = firstError.GetProperty("extensions").Deserialize<DirectusExceptionExtensions>(options);
					}
				}
			}
			catch (Exception)
			{
			}
			if (extensions != null)
				throw new DirectusException(message, extensions, successfullOffset);
			throw new Exception(JsonSerializer.Serialize(new { error = new { code = $"{response.StatusCode} {response.ReasonPhrase}", body = content } }));
			//throw new Exception(JsonSerializer.Serialize(new { error = new { code = $"{response.StatusCode} {response.ReasonPhrase}", body = $"\"{content.Trim('"')}\"" } }));
		}

		private async IAsyncEnumerable<Tout> GetAllItems<Tout, Tin>(
			string collection,
			int limit = -1,
			Dictionary<string, string> restrictions = null!,
			IEnumerable<string> fields = null!,
			Dictionary<string, IEnumerable<object>> inFilters = null)
		{
			foreach (var curInFilter in GetBatchFilters(inFilters, limit))
			{
				int offset = 0;
				int totalCount;
				do
				{
					var (Data, Count) = await GetItems<Tin>(collection,
												offset: offset,
												limit: limit,
												restrictions: restrictions,
												fields: fields,
												inFilters: curInFilter);
					totalCount = Count ?? 0;
					if (Data == null || !Data.Any())
						yield break;
					var mapped = _mapper.Map<List<Tout>>(Data);
					foreach (var item in mapped)
					{
						yield return item;
					}
					if (limit <= 0)
						offset = totalCount;
					else
						offset += limit;
				}
				while (offset < totalCount);
			}
		}

		//todo: set to private
		public static List<Dictionary<string, IEnumerable<object>>?> GetBatchFilters(Dictionary<string, IEnumerable<object>>? inFilters, int limit = -1)
		{
			var filters = new List<Dictionary<string, IEnumerable<object>>?>();

			int totalCount = inFilters?.Values.Sum(x => x?.Count()) ?? 0;
			if (limit <= 0 || inFilters == null || inFilters.Count == 0 || totalCount <= limit)
			{
				filters.Add(inFilters);
				return filters;
			}

			filters.Add(new Dictionary<string, IEnumerable<object>>());
			int curPos = 0;
			int curTotalPos = 0;
			List<object> list = new List<object>();
			foreach (var filter in inFilters)
			{
				var valCount = filter.Value.Count();
				if (valCount < limit - curPos)
				{
					filters.Last()[filter.Key] = filter.Value;
					curPos += valCount;
					curTotalPos += valCount;
					continue;
				}

				foreach (var filterItem in filter.Value)
				{
					list.Add(filterItem);
					curPos++;
					curTotalPos++;
					if (curPos >= limit)
					{
						filters.Last()[filter.Key] = list;
						list = new List<object>();
						if (totalCount > curTotalPos)
							filters.Add(new Dictionary<string, IEnumerable<object>>());
						curPos = 0;
					}
				}
				if (list.Count > 0)
				{
					filters.Last()[filter.Key] = list;
					list = new List<object>();
				}
			}
			return filters;
		}

		private async IAsyncEnumerable<Tout> Createtems<Tout, Tin>(string collection, List<Tout> items, int limit = -1)
		{
			if (items == null)
				yield break;
			var inItems = _mapper.Map<List<Tin>>(items);
			int offset = 0;
			if (limit <= 0)
				limit = -1;
			do
			{
				var curRes = await CreateItem(collection, (limit <= 0 ? inItems : inItems.Skip(offset).Take(limit).ToList()), limit, offset);
				if (curRes == null)
					yield break;
				var mapped = _mapper.Map<List<Tout>>(curRes);
				foreach (var item in mapped)
				{
					yield return item;
				}
				offset += limit;
			}
			while (limit > 0 && offset < items.Count);
		}

		private async Task CreatetemsNoReturn<Tout, Tin>(string collection, List<Tout> items, int limit = -1, CancellationToken cancellationToken = default)
		{
			if (items == null)
				return;
			var inItems = _mapper.Map<List<Tin>>(items);
			int offset = 0;
			if (limit <= 0)
				limit = -1;
			do
			{
				cancellationToken.ThrowIfCancellationRequested();
				await CreateItemNoReurtn(collection, (limit <= 0 ? inItems : inItems.Skip(offset).Take(limit).ToList()), cancellationToken, offset);
				offset += limit;
			}
			while (limit > 0 && offset < items.Count);
		}

		public async Task CreateOrderBooks(List<BookOrderEntity> orders, CancellationToken cancellationToken = default, int batchSize = -1)
		{
			_logger.LogInformation("started creating the bookr oreders");
			await CreatetemsNoReturn<BookOrderEntity, BookOrderDto>(OrdersCollection, orders, batchSize, cancellationToken);
			_logger.LogInformation("finished creating the bookr oreders");
		}

		public async Task CreateWatchers(List<WatcherEntity> watchers, CancellationToken cancellationToken = default, int batchSize = -1)
		{
			_logger.LogInformation("started creating the watchers");
			await CreatetemsNoReturn<WatcherEntity, WatcherDto>(WatchersCollection, watchers, batchSize, cancellationToken);
			_logger.LogInformation("finished creating the watchers");
		}

		public async Task CreateWatcherBookOrders(List<WatcherBookOrderEntity> watcherBookorders, CancellationToken cancellationToken = default, int batchSize = -1)
		{
			_logger.LogInformation("started creating the WatcherBookOrder");
			await CreatetemsNoReturn<WatcherBookOrderEntity, WatcherBookOrderDto>(WatcherBookOrderCollection, watcherBookorders, batchSize, cancellationToken);
			_logger.LogInformation("finished creating the WatcherBookOrder");
		}
	}
}
