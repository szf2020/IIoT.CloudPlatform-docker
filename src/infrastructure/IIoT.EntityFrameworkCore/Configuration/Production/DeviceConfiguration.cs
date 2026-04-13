using IIoT.Core.Production.Aggregates.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.EntityFrameworkCore.Configuration.Production;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.DeviceName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("device_name");

        // 值对象 ClientInstanceId → 两列(mac_address, client_code)
        builder.ComplexProperty(d => d.Instance, instance =>
        {
            instance.Property(i => i.MacAddress)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("mac_address");

            instance.Property(i => i.ClientCode)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("client_code");
        });

        builder.Property(d => d.ProcessId)
            .IsRequired()
            .HasColumnName("process_id");

        // 注意:(mac_address, client_code) 联合唯一索引不在这里建立。
        // 原因:EF Core 10 的 ComplexProperty 子成员不支持 Fluent API
        // 的 HasIndex 表达式(已知限制 dotnet/efcore#32578)。
        // 索引通过 MigrationWorkApp 启动期执行幂等 SQL 建立,
        // 和 RecordSchemaInitializer 同层次,不污染 EF 迁移快照,
        // 后续 migrations add 无需手动维护。

        builder.HasIndex(d => d.ProcessId)
            .HasDatabaseName("ix_devices_process_id");
    }
}