using IIoT.SharedKernel.Domain;

namespace IIoT.Core.Production.Aggregates.Devices;

/// <summary>
/// 聚合根：物理设备/上位机终端
/// </summary>
public class Device : IAggregateRoot
{
    protected Device()
    {
    }

    /// <summary>
    /// 领域构造工厂：强制要求名称、MAC地址和工序ID为必填项
    /// </summary>
    public Device(string deviceName, string macAddress, Guid processId)
    {
        Id = Guid.NewGuid();
        DeviceName = deviceName;
        MacAddress = macAddress;
        ProcessId = processId;
        IsActive = true;
    }

    public Guid Id { get; set; }

    /// <summary>
    /// 设备显示名称 (如: 1号叠片机，用于 UI 展示和报表)
    /// </summary>
    public string DeviceName { get; set; } = null!;

    /// <summary>
    /// 核心防伪标识：设备的物理 MAC 地址，用于开机向云端自证身份
    /// </summary>
    public string MacAddress { get; set; } = null!;

    /// <summary>
    /// 归属：这台机器属于哪个工序？
    /// </summary>
    public Guid ProcessId { get; set; }

    /// <summary>
    /// 设备状态 (true: 正常运行; false: 设备报废或停用)
    /// </summary>
    public bool IsActive { get; set; }
}