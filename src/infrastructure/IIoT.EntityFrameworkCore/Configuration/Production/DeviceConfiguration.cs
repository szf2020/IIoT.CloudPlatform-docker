using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IIoT.Core.Production.Aggregates.Devices;

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

        builder.HasIndex(d => d.MacAddress)
            .IsUnique()
            .HasDatabaseName("ix_devices_mac_address");

        builder.HasIndex(d => d.ProcessId)
            .HasDatabaseName("ix_devices_process_id");
    }
}