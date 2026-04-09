using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IIoT.EntityFrameworkCore.Identity;

namespace IIoT.EntityFrameworkCore.Configuration.Employee;

/// <summary>
/// 员工(操作员)实体的 EF Core 数据库映射配置
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Core.Employee.Aggregates.Employees.Employee>
{
    public void Configure(EntityTypeBuilder<Core.Employee.Aggregates.Employees.Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        // Employee.Id 是 ApplicationUser.Id 的外键,账号删除时级联删除员工档案
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Core.Employee.Aggregates.Employees.Employee>(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.EmployeeNo)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("employee_no");

        builder.Property(e => e.RealName)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("real_name");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.HasIndex(e => e.EmployeeNo)
            .IsUnique()
            .HasDatabaseName("ix_employees_employee_no");

        // 配置一对多导航属性：具体设备管辖权
        builder.HasMany(e => e.DeviceAccesses)
            .WithOne(eda => eda.Employee)
            .HasForeignKey(eda => eda.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}