using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IIoT.Core.Production.Aggregates.Devices;

namespace IIoT.EntityFrameworkCore.Configuration.Production;

/// <summary>
/// 物理设备终端的 EF Core 数据库映射配置
/// </summary>
public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        // 映射物理表名 (蛇形命名法)
        builder.ToTable("devices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        // 🌟 新增：配置设备显示名称，限制长度防止恶意脏数据穿透
        builder.Property(d => d.DeviceName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("device_name");

        builder.Property(d => d.DeviceCode)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("device_code");

        builder.Property(d => d.MacAddress)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("mac_address");

        builder.Property(d => d.ProcessId)
            .IsRequired()
            .HasColumnName("process_id");

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        // ==========================================
        // 🌟 核心基建：物理索引配置
        // ==========================================

        // 1. 防伪强制约束：MAC 地址在全厂全表中必须绝对唯一
        builder.HasIndex(d => d.MacAddress)
            .IsUnique()
            .HasDatabaseName("ix_devices_mac_address");

        // 2. 业务高频查询加速：WPF 上位机和前端面板经常需要根据 ProcessId 聚合展示设备列表
        builder.HasIndex(d => d.ProcessId)
            .HasDatabaseName("ix_devices_process_id");
    }
}