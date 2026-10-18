using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddWireGuardTrafficSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WireGuardTrafficSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WireGuardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransferRx = table.Column<long>(type: "INTEGER", nullable: false),
                    TransferTx = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WireGuardTrafficSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WireGuardTrafficSnapshots_WireGuardId_CapturedAtUtc",
                table: "WireGuardTrafficSnapshots",
                columns: new[] { "WireGuardId", "CapturedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WireGuardTrafficSnapshots");
        }
    }
}
