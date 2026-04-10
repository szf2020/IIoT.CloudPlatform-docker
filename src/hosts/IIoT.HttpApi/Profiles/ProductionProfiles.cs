using AutoMapper;
using IIoT.ProductionService.Commands.Capacities;
using IIoT.Services.Common.Events;

namespace IIoT.HttpApi.Profiles;

public class ProductionProfiles : Profile
{
    public ProductionProfiles()
    {
        CreateMap<ReceiveHourlyCapacityCommand, HourlyCapacityReceivedEvent>();
    }
}