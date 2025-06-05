using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Funcy.Data.Migrations
{
    /// <inheritdoc />
    public partial class azureid : Migration
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

            migrationBuilder.DropColumn(
                name: "LastModifiedTimeUtc",
                table: "FunctionApps");

            migrationBuilder.AlterColumn<long>(
                name: "FunctionAppId",
                table: "Functions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "AzureId",
                table: "Functions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "FunctionAppId",
                table: "FunctionAppSlots",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "AzureId",
                table: "FunctionAppSlots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "FunctionAppSlots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AzureId",
                table: "FunctionApps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId",
                table: "FunctionAppSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId",
                table: "Functions");

            migrationBuilder.DropColumn(
                name: "AzureId",
                table: "Functions");

            migrationBuilder.DropColumn(
                name: "AzureId",
                table: "FunctionAppSlots");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "FunctionAppSlots");

            migrationBuilder.DropColumn(
                name: "AzureId",
                table: "FunctionApps");

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

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedTimeUtc",
                table: "FunctionApps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FunctionAppSlots_FunctionApps_FunctionAppId",
                table: "FunctionAppSlots",
                column: "FunctionAppId",
                principalTable: "FunctionApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Functions_FunctionApps_FunctionAppId",
                table: "Functions",
                column: "FunctionAppId",
                principalTable: "FunctionApps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
