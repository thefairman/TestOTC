using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestOTC.Services.Selenium
{
	internal class BrowserManager
	{
		private readonly IWebDriver _driver;
		private const string CookieFile = "cookies.txt";
		public BrowserManager()
		{
			_driver = new ChromeDriver();
		}

		public void Init()
		{
			_driver.Navigate().GoToUrl("https://www.binance.com/ru/convert");
			Thread.Sleep(2000);
			LoadCookies();
			_driver.Navigate().GoToUrl("https://www.binance.com/ru/convert");
		}

		public void SaveCookies()
		{
			var cookies = _driver.Manage()?.Cookies?.AllCookies;
			if (cookies == null || !cookies.Any())
				return;
			File.WriteAllText(CookieFile, JsonSerializer.Serialize(cookies));
		}

		public void LoadCookies()
		{
			if (!File.Exists(CookieFile))
				return;
			//List<Cookie> cookies = new List<Cookie>();
			var tmpCookies = JsonSerializer.Deserialize<IEnumerable<TmpCookie>>(File.ReadAllText(CookieFile));
			if (tmpCookies == null || !tmpCookies.Any())
				return;
			foreach (var item in tmpCookies)
			{
				_driver.Manage().Cookies.AddCookie(new Cookie(item.Name, item.Value, item.Domain, item.Path, item.Expiry, item.Secure, item.IsHttpOnly, item.SameSite));
				//cookies.Add(new Cookie(item.Name, item.Value, item.Domain, item.Path, item.Expiry, item.Secure, item.IsHttpOnly, item.SameSite));
			}
		}

		internal class TmpCookie
		{
			public string Name { get; set; }
			public string Value { get; set; }
			public string Domain { get; set; }
			public string Path { get; set; }
			public bool Secure { get; set; }
			public bool IsHttpOnly { get; set; }
			public string SameSite { get; set; }
			public DateTime Expiry { get; set; }
		}
	}

	
}
