using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STL.DbContexts.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role_level",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role_level",
                table: "users");
        }
    }
}
