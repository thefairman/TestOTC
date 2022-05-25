﻿using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TestOTC.Entities.Selenium;

namespace TestOTC.Services.Selenium
{
	internal class SeleniumService
	{
		class TmpCookie
		{
			public string Name { get; set; }
			public string Value { get; set; }
			public string Domain { get; set; }
			public string Path { get; set; }
			public bool Secure { get; set; }
			public bool IsHttpOnly { get; set; }
			public string SameSite { get; set; }
			//public DateTime Expiry { get; set; }
		}

		private IWebDriver _driver;
		private const string CookieFile = "cookies.txt";
		private readonly string _from;
		private readonly string _to;

		public string From => _from;

		public string To => _to;

		public SeleniumService(string from, string to)
		{

			_from = from;
			_to = to;
		}

		public void LaunchBrowser()
		{
			ChromeOptions options = new ChromeOptions();
			var proxy = new Proxy();
			proxy.Kind = ProxyKind.Manual;
			proxy.IsAutoDetect = false;
			proxy.HttpProxy =
			proxy.SslProxy = "127.0.0.1:8000";
			options.Proxy = proxy;
			options.AddArgument("ignore-certificate-errors");
			//var chromedriver = new ChromeDriver(options);

			_driver = new ChromeDriver(options);
			//_driver = new ChromeDriver();
			//IWebDriver driver = new ChromeDriver();
			_driver.Navigate().GoToUrl("https://www.binance.com/ru/convert");
			Thread.Sleep(2000);
			LoadCookies();
			_driver.Navigate().GoToUrl("https://www.binance.com/ru/convert");
			Console.WriteLine("Enteer OK when login on the site");
			while (true)
			{
				if (Console.ReadLine() == "OK")
					break;
			}
			SaveCookies();
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
				_driver.Manage().Cookies.AddCookie(new Cookie(item.Name, item.Value, item.Domain, item.Path, DateTime.Now.AddYears(1), item.Secure, item.IsHttpOnly, item.SameSite));
				//cookies.Add(new Cookie(item.Name, item.Value, item.Domain, item.Path, item.Expiry, item.Secure, item.IsHttpOnly, item.SameSite));
			}
		}

		public QuoteItem GetQuote()
		{
			InitQuote();
			//var fromBy = By.XPath($"//div[contains(text(),'1 {From} =')]");
			//var toBy = By.XPath($"//div[contains(text(),'1 {To} =')]");
			var fromBy = By.XPath($"//div[contains(text(),'Цена')]/following::div");
			var toBy = By.XPath($"//div[contains(text(),'братный курс')]/following::div");

			var elemsFrom = _driver.FindElements(fromBy);
			var elemsTo = _driver.FindElements(toBy);

			if (elemsFrom.Count == 0 || elemsTo.Count == 0)
				throw new Exception("Can't find from or to element!");

			return new QuoteItem
			{
				FromRate = GetRate(elemsFrom.First().Text),
				ToRate = GetRate(elemsTo.First().Text)
			};
		}


		private void InitQuote()
		{
			var refreshBy = By.XPath("//button[contains(text(),'Обновить')]");
			var waiterDivBy = By.XPath("//div[@class='css-kn9hza']");

			//var receivedTexts = _driver.FindElements(waiterDivBy);
			//if (receivedTexts.Count > 0)
			//{
			//	IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
			//	//js.ExecuteScript("arguments[0].textContent='0';", receivedTexts.First());
			//	js.ExecuteScript("arguments[0].innerText='0';", receivedTexts.First());
			//}

			var refresh = _driver.FindElements(refreshBy);
			if (refresh.Count == 0)
				throw new Exception("Don't find refresh button!");

			refresh.First().Click();
			Thread.Sleep(100);

			var waiterDivs = _driver.FindElements(waiterDivBy);
			TimeOutLoop(
				() => 
				{ 
					waiterDivs = _driver.FindElements(waiterDivBy);
				},
				() => waiterDivs.Count == 0,
				TimeSpan.FromSeconds(6),
				"Waiter div doesn't disapear!");


			//if (receivedTexts.Count == 0)
			//{
			//	TimeOutLoop(
			//		() => receivedTexts = _driver.FindElements(waiterDivBy),
			//		() => receivedTexts.Count > 0,
			//		TimeSpan.FromSeconds(6),
			//		"Don't find div with received earns!");
			//}
			//else
			//{
			//	TimeOutLoop(
			//		null,
			//		() => !receivedTexts.First().Text.Contains("0"),
			//		TimeSpan.FromSeconds(6),
			//		"Error div doesn't set a message!");
			//}
		}
		//private void InitQuote()
		//{
		//	var refreshBy = By.XPath("//button[contains(text(),'Обновить')]");
		//	var receivedTextBy = By.XPath("//div[contains(text(),'Вы получите')]/following::div");

		//	var receivedTexts = _driver.FindElements(receivedTextBy);
		//	if (receivedTexts.Count > 0)
		//	{
		//		IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
		//		//js.ExecuteScript("arguments[0].textContent='0';", receivedTexts.First());
		//		js.ExecuteScript("arguments[0].innerText='0';", receivedTexts.First());
		//	}

		//	var refresh = _driver.FindElements(refreshBy);
		//	if (refresh.Count == 0)
		//		throw new Exception("Don't find refresh button!");

		//	refresh.First().Click();

		//	if (receivedTexts.Count == 0)
		//	{
		//		TimeOutLoop(
		//			() => receivedTexts = _driver.FindElements(receivedTextBy),
		//			() => receivedTexts.Count > 0,
		//			TimeSpan.FromSeconds(6),
		//			"Don't find div with received earns!");
		//	}
		//	else
		//	{
		//		TimeOutLoop(
		//			null,
		//			() => !receivedTexts.First().Text.Contains("0"),
		//			TimeSpan.FromSeconds(6),
		//			"Error div doesn't set a message!");
		//	}
		//}

		private static void TimeOutLoop(Action? action, Func<bool> condition, TimeSpan ts, string? errorMsg = null)
		{
			var execEndTime = DateTime.UtcNow + ts;
			while (DateTime.UtcNow < execEndTime)
			{
				if (condition.Invoke())
					return;
				Thread.Sleep(10);
				action?.Invoke();
				//errorMsg = _driver.FindElements(errorMsgBy);
			}
			if (errorMsg != null)
				throw new Exception(errorMsg);
		}

		private decimal GetRate(string str)
		{
			decimal rate = 0;
			SetFromLineInRegex(@"=\s*([\.\d]+)\s*", str, x => decimal.TryParse(x, out rate));
			return rate;
		}

		private static bool SetFromLineInRegex(string pattern, string line, Action<string> action)
		{
			if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(line))
				return false;
			var matches = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
			if (matches.Success)
			{
				if (matches.Groups.Count > 1)
				{
					action?.Invoke(matches.Groups[1].Value);
					return true;
				}
			}
			return false;
		}
	}
}
