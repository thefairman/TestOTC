using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestOTC.Entities.Directus;
using TestOTC.Entities.Directus.WebDto;

namespace TestOTC.Mappings
{
	internal class DirectusProfile : Profile
	{
		public DirectusProfile()
		{
			SourceMemberNamingConvention = new LowerUnderscoreNamingConvention();
			DestinationMemberNamingConvention = new PascalCaseNamingConvention();

			CreateMap<BookOrderDto, BookOrderEntity>()
				.ForMember(dest => dest.LocalTime, opt => opt.MapFrom(src => src.local_timestamp))
				.ForMember(dest => dest.BestBidPrice, opt => opt.MapFrom(src => src.best_bid_price))
				.ForMember(dest => dest.BestBidQuantity, opt => opt.MapFrom(src => src.best_bid_quantity))
				.ForMember(dest => dest.BestAskPrice, opt => opt.MapFrom(src => src.best_ask_price))
				.ForMember(dest => dest.BestAskQuantity, opt => opt.MapFrom(src => src.best_ask_quantity));
			CreateMap<BookOrderEntity, BookOrderDto>()
				.ForMember(dest => dest.local_timestamp, opt => opt.MapFrom(src => src.LocalTime))
				.ForMember(dest => dest.best_bid_price, opt => opt.MapFrom(src => src.BestBidPrice))
				.ForMember(dest => dest.best_bid_quantity, opt => opt.MapFrom(src => src.BestBidQuantity))
				.ForMember(dest => dest.best_ask_price, opt => opt.MapFrom(src => src.BestAskPrice))
				.ForMember(dest => dest.best_ask_quantity, opt => opt.MapFrom(src => src.BestAskQuantity));

			CreateMap<WatcherDto, WatcherEntity>()
				.ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.start_timestamp))
				.ForMember(dest => dest.From, opt => opt.MapFrom(src => src.c_from))
				.ForMember(dest => dest.To, opt => opt.MapFrom(src => src.c_to))
				.ForMember(dest => dest.QuotePrice, opt => opt.MapFrom(src => src.quote_price))
				.ForMember(dest => dest.InversePrice, opt => opt.MapFrom(src => src.inverse_price))
				.ForMember(dest => dest.ExpireTime, opt => opt.MapFrom(src => src.expire_timestamp));
			CreateMap<WatcherEntity, WatcherDto>()
				.ForMember(dest => dest.start_timestamp, opt => opt.MapFrom(src => src.StartTime))
				.ForMember(dest => dest.c_from, opt => opt.MapFrom(src => src.From))
				.ForMember(dest => dest.c_to, opt => opt.MapFrom(src => src.To))
				.ForMember(dest => dest.quote_price, opt => opt.MapFrom(src => src.QuotePrice))
				.ForMember(dest => dest.inverse_price, opt => opt.MapFrom(src => src.InversePrice))
				.ForMember(dest => dest.expire_timestamp, opt => opt.MapFrom(src => src.ExpireTime));

			CreateMap<WatcherBookOrderEntity, WatcherBookOrderDto>()
				.ForMember(dest => dest.watchers_id, opt => opt.MapFrom(src => src.WatchersId))
				.ForMember(dest => dest.book_orders_id, opt => opt.MapFrom(src => src.BookOrdersId));
		}
	}
}
