using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineTrack.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCollectionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FollowedActors_UserId_FollowedAt",
                table: "FollowedActors",
                columns: new[] { "UserId", "FollowedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteMovies_UserId_AddedAt",
                table: "FavoriteMovies",
                columns: new[] { "UserId", "AddedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FollowedActors_UserId_FollowedAt",
                table: "FollowedActors");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteMovies_UserId_AddedAt",
                table: "FavoriteMovies");
        }
    }
}
