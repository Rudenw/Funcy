using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Funcy.Data.Migrations
{
    /// <inheritdoc />
    public partial class onmodelcreating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId",
                table: "FunctionAppSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId",
                table: "Functions");

            migrationBuilder.AddColumn<long>(
                name: "FunctionAppId1",
                table: "Functions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FunctionAppId1",
                table: "FunctionAppSlots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Functions_FunctionAppId1",
                table: "Functions",
                column: "FunctionAppId1");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionAppSlots_FunctionAppId1",
                table: "FunctionAppSlots",
                column: "FunctionAppId1");

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId",
                table: "FunctionAppSlots",
                column: "FunctionAppId",
                principalTable: "FunctionApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId1",
                table: "FunctionAppSlots",
                column: "FunctionAppId1",
                principalTable: "FunctionApps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId",
                table: "Functions",
                column: "FunctionAppId",
                principalTable: "FunctionApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId1",
                table: "Functions",
                column: "FunctionAppId1",
                principalTable: "FunctionApps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId",
                table: "FunctionAppSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId1",
                table: "FunctionAppSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId",
                table: "Functions");

            migrationBuilder.DropForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId1",
                table: "Functions");

            migrationBuilder.DropIndex(
                name: "IX_Functions_FunctionAppId1",
                table: "Functions");

            migrationBuilder.DropIndex(
                name: "IX_FunctionAppSlots_FunctionAppId1",
                table: "FunctionAppSlots");

            migrationBuilder.DropColumn(
                name: "FunctionAppId1",
                table: "Functions");

            migrationBuilder.DropColumn(
                name: "FunctionAppId1",
                table: "FunctionAppSlots");

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId",
                table: "FunctionAppSlots",
                column: "FunctionAppId",
                principalTable: "FunctionApps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId",
                table: "Functions",
                column: "FunctionAppId",
                principalTable: "FunctionApps",
                principalColumn: "Id");
        }
    }
}
