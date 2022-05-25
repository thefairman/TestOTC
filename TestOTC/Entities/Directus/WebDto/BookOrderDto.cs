using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Entities.Directus.WebDto
{
	internal class BookOrderDto
	{
		public string id { get; set; }
		public string symbol { get; set; }
		public DateTime local_timestamp { get; set; }
		public decimal best_bid_price { get; set; }
		public decimal best_bid_quantity { get; set; }
		public decimal best_ask_price { get; set; }
		public decimal best_ask_quantity { get; set; }
	}
}
