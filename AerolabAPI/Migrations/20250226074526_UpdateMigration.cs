using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerolabAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaceEncoding",
                table: "Visitors",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstSeen",
                table: "Visitors",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceEncoding",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "FirstSeen",
                table: "Visitors");
        }
    }
}
