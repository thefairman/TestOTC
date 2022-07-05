using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace TestOTC.Services.TitaniumProxy
{
	interface ITitaniumProxyService
	{
		public int MyProperty { get; set; }
	}
	internal class TitaniumProxyService : IHostedService
	{
		private readonly ProxyServer? _proxyServer;
		private readonly ILogger _logger;
		public TitaniumProxyService(ILogger<TitaniumProxyService> logger)
		{
			_logger = logger;
			_proxyServer = new ProxyServer();
		}
		public Task StartAsync(CancellationToken cancellationToken)
		{
			var proxyServer = _proxyServer;
			// locally trust root certificate used by this proxy 
			//proxyServer.CertificateManager.TrustRootCertificate(true);

			proxyServer.CertificateManager.CertificateEngine = Titanium.Web.Proxy.Network.CertificateEngine.DefaultWindows;
			proxyServer.CertificateManager.EnsureRootCertificate();

			proxyServer.BeforeRequest += OnRequest;
			proxyServer.BeforeResponse += OnResponse;

			var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true)
			{
				// Use self-issued generic certificate on all https requests
				// Optimizes performance by not creating a certificate for each https-enabled domain
				// Useful when certificate trust is not required by proxy clients
				//GenericCertificate = new X509Certificate2(Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genericcert.pfx"), "password")
			};

			// An explicit endpoint is where the client knows about the existence of a proxy
			// So client sends request in a proxy friendly manner
			proxyServer.AddEndPoint(explicitEndPoint);

			proxyServer.Start();

			foreach (var endPoint in proxyServer.ProxyEndPoints)
				_logger.LogInformation("Listening on '{0}' endpoint at Ip {1} and port: {2} ",
					endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port);

			return Task.CompletedTask;
		}

		public event Action<string> OnQuoteRequest;
		public event Action<string?, string> OnQuoteResponse;

		public async Task OnRequest(object sender, SessionEventArgs e)
		{
			var method = e.HttpClient.Request.Method.ToUpper();
			if (e.HttpClient.Request.RequestUri.AbsoluteUri == @"https://www.binance.com/bapi/margin/v1/private/new-otc/get-quote"
				&& method == "POST")
			{
				//// Get/Set request body bytes
				//byte[] bodyBytes = await e.GetRequestBody();
				//e.SetRequestBody(bodyBytes);

				//// Get/Set request body as string
				//string bodyString = await e.GetRequestBodyAsString();
				//e.SetRequestBodyString(bodyString);

				// store request 
				// so that you can find it from response handler
				var guid = Guid.NewGuid().ToString();
				e.UserData = guid;
				OnQuoteRequest?.Invoke(guid);
			}
			//Console.WriteLine(e.HttpClient.Request.Url);

			//// read request headers
			//var requestHeaders = e.HttpClient.Request.Headers;

			//var method = e.HttpClient.Request.Method.ToUpper();
			//if ((method == "POST" || method == "PUT" || method == "PATCH"))
			//{
			//	// Get/Set request body bytes
			//	byte[] bodyBytes = await e.GetRequestBody();
			//	e.SetRequestBody(bodyBytes);

			//	// Get/Set request body as string
			//	string bodyString = await e.GetRequestBodyAsString();
			//	e.SetRequestBodyString(bodyString);

			//	// store request 
			//	// so that you can find it from response handler 
			//	e.UserData = e.HttpClient.Request;
			//}

			//// To cancel a request with a custom HTML content
			//// Filter URL
			//if (e.HttpClient.Request.RequestUri.AbsoluteUri.Contains("google.com"))
			//{
			//	e.Ok("<!DOCTYPE html>" +
			//		"<html><body><h1>" +
			//		"Website Blocked" +
			//		"</h1>" +
			//		"<p>Blocked by titanium web proxy.</p>" +
			//		"</body>" +
			//		"</html>");
			//}

			//// Redirect example
			//if (e.HttpClient.Request.RequestUri.AbsoluteUri.Contains("wikipedia.org"))
			//{
			//	e.Redirect("https://www.paypal.com");
			//}
		}

		public async Task OnResponse(object sender, SessionEventArgs e)
		{
			if (e.HttpClient.Request.RequestUri.AbsoluteUri != @"https://www.binance.com/bapi/margin/v1/private/new-otc/get-quote"
				|| e.HttpClient.Request.Method != "POST") return;
			// read response headers
			//var responseHeaders = e.HttpClient.Response.Headers;

			////if (!e.ProxySession.Request.Host.Equals("medeczane.sgk.gov.tr")) return;

			if (e.HttpClient.Response.StatusCode == 200)
			{
				if (e.HttpClient.Response.ContentType != null && e.HttpClient.Response.ContentType.Trim().ToLower().Contains("application/json"))
				{
					string body = await e.GetResponseBodyAsString();
					OnQuoteResponse?.Invoke(e.UserData as string, body);
				}
			}


			//if (e.UserData != null)
			//{
			//	// access request from UserData property where we stored it in RequestHandler
			//	var request = (Request)e.UserData;
			//}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_proxyServer.BeforeRequest -= OnRequest;
			_proxyServer.BeforeResponse -= OnResponse;
			_proxyServer.Stop();
			return Task.CompletedTask;
		}
	}
}
