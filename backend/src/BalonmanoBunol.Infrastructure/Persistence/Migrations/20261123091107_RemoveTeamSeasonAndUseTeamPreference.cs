using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalonmanoBunol.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTeamSeasonAndUseTeamPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE teams SET \"Category\" = 'Senior' WHERE \"Category\" IN ('Senior A', 'Senior B');");

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredTeamId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AspNetUsers" AS account
                SET "PreferredTeamId" = COALESCE(
                    (SELECT team."Id" FROM teams AS team WHERE team."IsActive" AND team."Name" = account."PreferredTeam" ORDER BY team."Name" LIMIT 1),
                    (SELECT team."Id" FROM teams AS team
                     WHERE team."IsActive" AND team."Category" = CASE account."PreferredTeam"
                         WHEN 'Senior A' THEN 'Senior'
                         WHEN 'Senior B' THEN 'Senior'
                         ELSE account."PreferredTeam"
                     END
                     ORDER BY team."IsActive" DESC, team."Name" LIMIT 1))
                WHERE account."PreferredTeam" <> '';
                """);

            migrationBuilder.DropIndex(
                name: "IX_teams_Season_Category",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "PreferredTeam",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_teams_Category_Name",
                table: "teams",
                columns: new[] { "Category", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PreferredTeamId",
                table: "AspNetUsers",
                column: "PreferredTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_teams_PreferredTeamId",
                table: "AspNetUsers",
                column: "PreferredTeamId",
                principalTable: "teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_teams_PreferredTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_teams_Category_Name",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PreferredTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredTeamId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "Season",
                table: "teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreferredTeam",
                table: "AspNetUsers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Senior A");

            migrationBuilder.CreateIndex(
                name: "IX_teams_Season_Category",
                table: "teams",
                columns: new[] { "Season", "Category" });
        }
    }
}
