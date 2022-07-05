using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Entities.Selenium
{
	internal class QuoteItem
	{
		public decimal QuotePrice { get; set; }
		public decimal InversePrice { get; set; }
		public string? ToCoin { get; set; }
		public string? FromCoin { get; set; }
		public DateTime ExpiredTime { get; set; }
	}
}
