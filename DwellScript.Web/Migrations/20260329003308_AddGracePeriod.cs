using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DwellScript.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGracePeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GracePeriodEndsAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GracePeriodEndsAt",
                table: "AspNetUsers");
        }
    }
}
