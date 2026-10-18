using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFCore.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelegramId = table.Column<long>(type: "INTEGER", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WireGuardPeers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WireGuardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PublicKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WireGuardPeers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WireGuardUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelegramUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WireGuardUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WireGuardUsers_TelegramUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WireGuardUsers_WireGuardPeers_PeerId",
                        column: x => x.PeerId,
                        principalTable: "WireGuardPeers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUsers_TelegramId",
                table: "TelegramUsers",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WireGuardPeers_WireGuardId",
                table: "WireGuardPeers",
                column: "WireGuardId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WireGuardUsers_PeerId",
                table: "WireGuardUsers",
                column: "PeerId");

            migrationBuilder.CreateIndex(
                name: "IX_WireGuardUsers_TelegramUserId_PeerId",
                table: "WireGuardUsers",
                columns: new[] { "TelegramUserId", "PeerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WireGuardUsers");

            migrationBuilder.DropTable(
                name: "TelegramUsers");

            migrationBuilder.DropTable(
                name: "WireGuardPeers");
        }
    }
}
