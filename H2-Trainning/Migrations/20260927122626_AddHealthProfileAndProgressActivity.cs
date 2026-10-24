using System;
using H2_Trainning.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace H2_Trainning.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260927122626_AddHealthProfileAndProgressActivity")]
    public partial class AddHealthProfileAndProgressActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "AspNetUsers",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaistCm",
                table: "AspNetUsers",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ArmCm",
                table: "AspNetUsers",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThighCm",
                table: "AspNetUsers",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChestCm",
                table: "AspNetUsers",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "ExerciseLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Age", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "HeightCm", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "WaistCm", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "ArmCm", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "ThighCm", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "ChestCm", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "LastActivityAt", table: "ExerciseLogs");
        }
    }
}
