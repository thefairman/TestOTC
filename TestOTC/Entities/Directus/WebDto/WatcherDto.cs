using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Entities.Directus.WebDto
{
	internal class WatcherDto
	{
		public string id { get; set; }
		public string symbol { get; set; }
		public DateTime start_timestamp { get; set; }
		public decimal quote { get; set; }
		public string c_from { get; set; }
		public string c_to { get; set; }
	}
}
