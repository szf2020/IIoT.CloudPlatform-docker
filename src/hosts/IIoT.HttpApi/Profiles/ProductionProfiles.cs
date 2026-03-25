using AutoMapper;
using IIoT.ProductionService.Commands.PassStations;
using IIoT.ProductionService.Commands.Capacities;
using IIoT.Services.Common.Events;

namespace IIoT.HttpApi.Profiles;

public class ProductionProfiles : Profile
{
    public ProductionProfiles()
    {
        // Command → Event (在 Handler 里调用)
        CreateMap<ReceiveInjectionPassCommand, PassDataInjectionReceivedEvent>();
        CreateMap<ReceiveDailyCapacityCommand, DailyCapacityReceivedEvent>();
    }
}