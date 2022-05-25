using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Entities.Directus
{
	internal class WatcherEntity
	{
		public string Id { get; set; }
		public string Symbol { get; set; }
		public DateTime StartTime { get; set; }
		public decimal Quote { get; set; }
		public string From { get; set; }
		public string To { get; set; }
	}
}
