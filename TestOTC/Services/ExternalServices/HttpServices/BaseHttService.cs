using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestOTC.Services.ExternalServices.HttpServices
{
	internal class BaseHttpServise
	{
		protected readonly HttpClient _httpClient;
		public BaseHttpServise(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		protected static async Task<string> ReadResponseAndEnsureIsOk(HttpResponseMessage response)
		{
			string content = await response.Content.ReadAsStringAsync();
			if (response.IsSuccessStatusCode)
				return content;
			throw new Exception(JsonSerializer.Serialize(new { error = new { code = $"{response.StatusCode} {response.ReasonPhrase}", body = content } }));
		}
	}
}
