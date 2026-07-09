using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STL.DbContexts.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserUsernameToEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "username",
                table: "users",
                newName: "email");

            migrationBuilder.RenameIndex(
                name: "IX_users_username",
                table: "users",
                newName: "IX_users_email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "email",
                table: "users",
                newName: "username");

            migrationBuilder.RenameIndex(
                name: "IX_users_email",
                table: "users",
                newName: "IX_users_username");
        }
    }
}
