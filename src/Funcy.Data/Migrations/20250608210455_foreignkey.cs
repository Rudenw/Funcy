using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Funcy.Data.Migrations
{
    /// <inheritdoc />
    public partial class foreignkey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId1",
                table: "FunctionAppSlots");

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

            migrationBuilder.AlterColumn<long>(
                name: "FunctionAppId",
                table: "Functions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "FunctionAppId",
                table: "FunctionAppSlots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "FunctionAppId",
                table: "Functions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<long>(
                name: "FunctionAppId1",
                table: "Functions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "FunctionAppId",
                table: "FunctionAppSlots",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

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
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId1",
                table: "FunctionAppSlots",
                column: "FunctionAppId1",
                principalTable: "FunctionApps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId1",
                table: "Functions",
                column: "FunctionAppId1",
                principalTable: "FunctionApps",
                principalColumn: "Id");
        }
    }
}
