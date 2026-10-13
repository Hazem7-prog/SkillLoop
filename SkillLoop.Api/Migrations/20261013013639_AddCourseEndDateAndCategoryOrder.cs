using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillLoop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseEndDateAndCategoryOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Coding", 2 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "SortOrder",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "SortOrder",
                value: 6);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Icon", "Name", "SortOrder" },
                values: new object[] { "trending-up", "Marketing", 4 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "SortOrder",
                value: 7);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "SortOrder",
                value: 8);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                column: "SortOrder",
                value: 9);

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Icon", "Name", "SortOrder" },
                values: new object[] { 9, "sparkles", "AI", 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Categories");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Programming");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Icon", "Name" },
                values: new object[] { "briefcase", "Business" });
        }
    }
}
