using AutoMapper;
using FreshRescue.Business.Features.Sales.Dtos;
using FreshRescue.Entities.Sales;

namespace FreshRescue.Business.Features.Sales;

/// <summary>Cần nạp kèm <c>SaleBatch.Store</c> và <c>SaleBatch.Offers</c>.</summary>
public class SalesProfile : Profile
{
    public SalesProfile()
    {
        CreateMap<Offer, OfferDto>();

        CreateMap<SaleBatch, SaleBatchDto>()
            .ForMember(d => d.StoreName, o => o.MapFrom(s => s.Store.Name))
            .ForMember(d => d.Offers, o => o.MapFrom(s => s.Offers.Where(x => !x.IsDeleted).OrderBy(x => x.Name)));

        CreateMap<SaleBatch, SaleBatchSummaryDto>()
            .ForMember(d => d.StoreName, o => o.MapFrom(s => s.Store.Name))
            .ForMember(d => d.OfferCount, o => o.MapFrom(s => s.Offers.Count(x => !x.IsDeleted)));
    }
}
