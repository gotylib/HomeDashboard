using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Home.Api.Migrations
{
    /// <inheritdoc />
    public partial class StoreWallpaperInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WallpaperContentType",
                table: "Settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "WallpaperData",
                table: "Settings",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WallpaperUpdatedAt",
                table: "Settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "WallpaperContentType", "WallpaperData", "WallpaperUpdatedAt" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WallpaperContentType",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "WallpaperData",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "WallpaperUpdatedAt",
                table: "Settings");
        }
    }
}
