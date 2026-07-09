using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace STL.DbContexts.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_tokens_user_id",
                table: "user_tokens",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_tokens_users_user_id",
                table: "user_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_tokens_users_user_id",
                table: "user_tokens");

            migrationBuilder.DropIndex(
                name: "IX_users_username",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_user_tokens_user_id",
                table: "user_tokens");
        }
    }
}
