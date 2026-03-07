using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Funcy.Data.Migrations
{
    /// <inheritdoc />
    public partial class add_function_app_tags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "System",
                table: "FunctionApps");

            migrationBuilder.CreateTable(
                name: "FunctionAppTags",
                columns: table => new
                {
                    FunctionAppId = table.Column<long>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionAppTags", x => new { x.FunctionAppId, x.Key });
                    table.ForeignKey(
                        name: "FK_FunctionAppTags_FunctionApps_FunctionAppId",
                        column: x => x.FunctionAppId,
                        principalTable: "FunctionApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FunctionAppTags");

            migrationBuilder.AddColumn<string>(
                name: "System",
                table: "FunctionApps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
