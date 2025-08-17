using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddedStateToFormAndFixedCasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Completed",
                table: "Forms");

            migrationBuilder.RenameColumn(
                name: "urgency",
                table: "Forms",
                newName: "Urgency");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "Forms",
                newName: "Category");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Forms",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "Forms");

            migrationBuilder.RenameColumn(
                name: "Urgency",
                table: "Forms",
                newName: "urgency");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Forms",
                newName: "category");

            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "Forms",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
