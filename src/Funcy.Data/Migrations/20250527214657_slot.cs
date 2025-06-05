using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Funcy.Data.Migrations
{
    /// <inheritdoc />
    public partial class slot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedTimeUtc",
                table: "FunctionApps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FunctionAppSlots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    FunctionAppId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionAppSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId",
                        column: x => x.FunctionAppId,
                        principalTable: "FunctionApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionAppSlots_FunctionAppId",
                table: "FunctionAppSlots",
                column: "FunctionAppId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FunctionAppSlots");

            migrationBuilder.DropColumn(
                name: "LastModifiedTimeUtc",
                table: "FunctionApps");
        }
    }
}
