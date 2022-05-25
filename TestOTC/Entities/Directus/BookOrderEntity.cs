using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Entities.Directus
{
	internal class BookOrderEntity
	{
		public string Id { get; set; }
		public string Symbol { get; set; }
		public DateTime LocalTime { get; set; }
		public decimal BestBidPrice { get; set; }
		public decimal BestBidQuantity { get; set; }
		public decimal BestAskPrice { get; set; }
		public decimal BestAskQuantity { get; set; }
	}
}
