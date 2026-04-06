using IIoT.Core.Production.Aggregates.Capacities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.EntityFrameworkCore.Configuration.Production;

public class HourlyCapacityConfiguration : IEntityTypeConfiguration<HourlyCapacity>
{
    public void Configure(EntityTypeBuilder<HourlyCapacity> builder)
    {
        builder.ToTable("hourly_capacity");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.DeviceId).IsRequired().HasColumnName("device_id");
        builder.Property(c => c.Date).IsRequired().HasColumnName("date");
        builder.Property(c => c.ShiftCode).IsRequired().HasMaxLength(20).HasColumnName("shift_code");
        builder.Property(c => c.Hour).IsRequired().HasColumnName("hour");
        builder.Property(c => c.Minute).IsRequired().HasColumnName("minute");
        builder.Property(c => c.TimeLabel).IsRequired().HasMaxLength(20).HasColumnName("time_label");
        builder.Property(c => c.TotalCount).IsRequired().HasColumnName("total_count");
        builder.Property(c => c.OkCount).IsRequired().HasColumnName("ok_count");
        builder.Property(c => c.NgCount).IsRequired().HasColumnName("ng_count");

        // PlcName 可空，存量数据为 null，迁移时加列默认值为 NULL
        builder.Property(c => c.PlcName).HasMaxLength(100).HasColumnName("plc_name").IsRequired(false);

        builder.Property(c => c.ReportedAt).IsRequired().HasColumnName("reported_at");

        builder.HasIndex(c => new { c.DeviceId, c.Date, c.Hour, c.Minute, c.ShiftCode })
            .IsUnique()
            .HasDatabaseName("ix_hourly_capacity_device_date_hour_minute_shift");

        // 辅助索引：加速按 plc_name 过滤的查询（可选，按实际查询量决定是否启用）
        builder.HasIndex(c => new { c.DeviceId, c.Date, c.PlcName })
            .HasDatabaseName("ix_hourly_capacity_device_date_plcname");
    }
}