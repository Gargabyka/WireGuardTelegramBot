using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFCore.Migrations
{
    /// <inheritdoc />
    public partial class FixHostingReminderScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderDueDate",
                table: "HostingReminderStates");

            migrationBuilder.AddColumn<int>(
                name: "RemindersSentCount",
                table: "HostingReminderStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemindersSentCount",
                table: "HostingReminderStates");

            migrationBuilder.AddColumn<string>(
                name: "LastReminderDueDate",
                table: "HostingReminderStates",
                type: "TEXT",
                nullable: true);
        }
    }
}
