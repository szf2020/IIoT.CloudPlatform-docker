using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIoT.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeDataModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_capacity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    ok_count = table.Column<int>(type: "integer", nullable: false),
                    ng_count = table.Column<int>(type: "integer", nullable: false),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_capacity", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    log_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pass_data_injection",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pre_injection_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pre_injection_weight = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    post_injection_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    post_injection_weight = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    injection_volume = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cell_result = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    completed_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pass_data_injection", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_daily_capacity_device_date_shift",
                table: "daily_capacity",
                columns: new[] { "device_id", "date", "shift_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_logs_device_time",
                table: "device_logs",
                columns: new[] { "device_id", "log_time" });

            migrationBuilder.CreateIndex(
                name: "ix_device_logs_level",
                table: "device_logs",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_pass_data_injection_barcode",
                table: "pass_data_injection",
                column: "barcode");

            migrationBuilder.CreateIndex(
                name: "ix_pass_data_injection_device_id",
                table: "pass_data_injection",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_pass_data_injection_device_time",
                table: "pass_data_injection",
                columns: new[] { "device_id", "completed_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_capacity");

            migrationBuilder.DropTable(
                name: "device_logs");

            migrationBuilder.DropTable(
                name: "pass_data_injection");
        }
    }
}
