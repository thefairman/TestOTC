using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Configuration
{
	internal static class HttpClientConfiguration
	{
		public static IHttpClientBuilder ConfigureHttpClient(this IHttpClientBuilder builder, int limitCount, TimeSpan limitTime, TimeSpan timeOut)
		{
			return builder
			.SetHandlerLifetime(TimeSpan.FromMinutes(5))
			.AddTransientHttpErrorPolicy(
				   x => x.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt))))
			.AddHttpMessageHandler(() =>
				new RateLimitHttpMessageHandler(
					limitCount: limitCount,
					limitTime: limitTime))
			.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(timeOut));
		}
	}

	public class RateLimitHttpMessageHandler : DelegatingHandler
	{
		private readonly List<DateTimeOffset> _callLog =
			new();
		private readonly TimeSpan _limitTime;
		private readonly int _limitCount;

		public RateLimitHttpMessageHandler(int limitCount, TimeSpan limitTime)
		{
			_limitCount = limitCount;
			_limitTime = limitTime;
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			//request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			var now = DateTimeOffset.UtcNow;

			lock (_callLog)
			{
				_callLog.Add(now);

				while (_callLog.Count > _limitCount)
					_callLog.RemoveAt(0);
			}

			await LimitDelay(now);

			return await base.SendAsync(request, cancellationToken);
		}

		private async Task LimitDelay(DateTimeOffset now)
		{
			if (_callLog.Count < _limitCount)
				return;

			var limit = now.Add(-_limitTime);

			var lastCall = DateTimeOffset.MinValue;
			var shouldLock = false;

			lock (_callLog)
			{
				lastCall = _callLog.FirstOrDefault();
				shouldLock = _callLog.Count(x => x >= limit) >= _limitCount;
			}

			var delayTime = shouldLock && (lastCall > DateTimeOffset.MinValue) // todo: check condition
				? (lastCall - limit)
				: TimeSpan.Zero;

			if (delayTime > TimeSpan.Zero)
				await Task.Delay(delayTime);
		}
	}
}
