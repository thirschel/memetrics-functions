using AutoMapper;
using MeMetrics.Updater.Application.Objects.PersonalCapital;

namespace MeMetrics.Updater.Application.Profiles
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            CreateMap<Transaction, Objects.MeMetrics.Transaction>()
                .ForMember(dest => dest.TransactionId, source => source.MapFrom(x => x.UserTransactionId))
                .ForMember(dest => dest.AccountId, source => source.MapFrom(x => x.AccountId))
                .ForMember(dest => dest.AccountName, source => source.MapFrom(x => x.AccountName))
                .ForMember(dest => dest.MerchantId, source => source.MapFrom(x => x.MerchantId))
                .ForMember(dest => dest.Amount, source => source.MapFrom(x => (decimal) x.Amount))
                .ForMember(dest => dest.Description, source => source.MapFrom(x => x.Description))
                .ForMember(dest => dest.IsCashIn, source => source.MapFrom(x => x.IsCashIn))
                .ForMember(dest => dest.IsCashOut, source => source.MapFrom(x => x.IsCashOut))
                .ForMember(dest => dest.CategoryId, source => source.MapFrom(x => x.CategoryId))
                .ForMember(dest => dest.OccurredDate, source => source.MapFrom(x => x.TransactionDate));
        }
    }
}