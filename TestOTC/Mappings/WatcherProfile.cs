using AutoMapper;
using Binance.Net.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestOTC.Entities.Directus;

namespace TestOTC.Mappings
{
	internal class WatcherProfile : Profile
	{
		public WatcherProfile()
		{
			CreateMap<IBinanceBookPrice, BookOrderEntity>()
				.ForMember(dest => dest.LocalTime, opt => opt.MapFrom(src => DateTime.UtcNow))
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid().ToString()));
		}
	}
}
