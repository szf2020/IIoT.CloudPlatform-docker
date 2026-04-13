namespace IIoT.Core.Production.Contracts.PassStation;

public sealed record InjectionWriteModel(
    Guid Id,
    Guid DeviceId,
    string CellResult,
    DateTime CompletedTime,
    DateTime ReceivedAt,
    string Barcode,
    DateTime PreInjectionTime,
    decimal PreInjectionWeight,
    DateTime PostInjectionTime,
    decimal PostInjectionWeight,
    decimal InjectionVolume
) : IPassStationWriteModel;
