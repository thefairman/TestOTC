using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Entities.Selenium
{

	public class QuoteDto
	{
		public string code { get; set; }
		public object message { get; set; }
		public object messageDetail { get; set; }
		public Quote data { get; set; }
		public bool success { get; set; }
	}

	public class Quote
	{
		public string quoteId { get; set; }
		public string quotePrice { get; set; }
		public string inversePrice { get; set; }
		public int expireTime { get; set; }
		public long expireTimestamp { get; set; }
		public string fromCoin { get; set; }
		public string toCoin { get; set; }
		public string toCoinAmount { get; set; }
		public string fromCoinAmount { get; set; }
		public string requestCoin { get; set; }
		public string requestAmount { get; set; }
		public string fromCoinAsset { get; set; }
		public string message { get; set; }
	}

}
