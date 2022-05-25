using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOTC.Entities.Directus.WebDto
{
	internal class DirectusRoot<T>
	{
		public class Meta
		{
			public int filter_count { get; set; }
		}
		public T data { get; set; }
		public Meta meta { get; set; }
	}
}
