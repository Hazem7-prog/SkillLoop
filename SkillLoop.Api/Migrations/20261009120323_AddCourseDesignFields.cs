using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillLoop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseDesignFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourseId_StartTime",
                table: "Bookings");

            migrationBuilder.AddColumn<bool>(
                name: "AllowsOffline",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsOnline",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LearningOutcomes",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxStudentsPerSession",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "OfflineLocation",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourseId_StudentId_StartTime",
                table: "Bookings",
                columns: new[] { "CourseId", "StudentId", "StartTime" },
                unique: true,
                filter: "[Status] <> 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourseId_StudentId_StartTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AllowsOffline",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AllowsOnline",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "LearningOutcomes",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MaxStudentsPerSession",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "OfflineLocation",
                table: "Courses");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourseId_StartTime",
                table: "Bookings",
                columns: new[] { "CourseId", "StartTime" },
                unique: true,
                filter: "[Status] <> 2");
        }
    }
}
