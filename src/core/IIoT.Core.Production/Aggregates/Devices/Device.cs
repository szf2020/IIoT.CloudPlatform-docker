using IIoT.SharedKernel.Domain;

namespace IIoT.Core.Production.Aggregates.Devices;

/// <summary>
/// 聚合根：物理设备/上位机终端
/// </summary>
public class Device : IAggregateRoot
{
    // 给 EF Core 预留的无参构造函数 (Protected级别，禁止业务层直接 new)
    protected Device()
    {
    }

    /// <summary>
    /// 领域构造工厂：强制要求名称、编号、MAC地址和工序ID为必填项
    /// </summary>
    public Device(string deviceName, string deviceCode, string macAddress, Guid processId)
    {
        Id = Guid.NewGuid();
        DeviceName = deviceName;
        DeviceCode = deviceCode;
        MacAddress = macAddress;
        ProcessId = processId;
        IsActive = true; // 默认注册时即为激活状态
    }

    /// <summary>
    /// 设备全局唯一标识 (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 🌟 新增：设备显示名称 (如: 1号叠片机，主要用于 UI 展示和报表)
    /// </summary>
    public string DeviceName { get; set; } = null!;

    /// <summary>
    /// 设备系统编号 (如: Stacker-01，可用于业务编码规则校验)
    /// </summary>
    public string DeviceCode { get; set; } = null!;

    /// <summary>
    /// 🌟 核心防伪标识：设备的物理 MAC 地址，用于开机向云端自证身份
    /// </summary>
    public string MacAddress { get; set; } = null!;

    /// <summary>
    /// 归属：这台机器属于哪个工序？
    /// ⚠️ 注意：这里只有 UUID，没有 MfgProcess 实体引用，实现了跨类库完美解耦！
    /// </summary>
    public Guid ProcessId { get; set; }

    /// <summary>
    /// 设备状态 (true: 正常运行; false: 设备报废或停用)
    /// </summary>
    public bool IsActive { get; set; }
}