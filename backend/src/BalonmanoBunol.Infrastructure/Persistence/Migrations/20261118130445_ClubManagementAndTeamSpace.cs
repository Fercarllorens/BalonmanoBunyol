using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalonmanoBunol.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClubManagementAndTeamSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarStorageKey",
                table: "AspNetUsers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "character varying(79)",
                maxLength: 79,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreferredTeam",
                table: "AspNetUsers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Senior A");

            migrationBuilder.Sql("UPDATE \"AspNetUsers\" SET \"FirstName\" = left(split_part(\"DisplayName\", ' ', 1), 80), \"LastName\" = left(btrim(substr(\"DisplayName\", length(split_part(\"DisplayName\", ' ', 1)) + 1)), 79) WHERE \"DisplayName\" <> '';");

            migrationBuilder.CreateTable(
                name: "administration_audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdministratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administration_audit_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_administration_audit_entries_AspNetUsers_AdministratorId",
                        column: x => x.AdministratorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_administration_audit_entries_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_attachments_posts_PostId",
                        column: x => x.PostId,
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_assignments_AspNetUsers_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_assignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_assignments_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_board_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_board_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_board_posts_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_board_posts_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Venue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_events_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_events_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Opponent = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Venue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Round = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_matches_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_matches_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_player_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JerseyNumber = table.Column<int>(type: "integer", nullable: true),
                    Position = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    MinutesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Goals = table.Column<int>(type: "integer", nullable: false),
                    Saves = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_player_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_player_memberships_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_player_memberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_player_memberships_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_board_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_board_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_board_attachments_team_board_posts_BoardPostId",
                        column: x => x.BoardPostId,
                        principalTable: "team_board_posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_callups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalledUpByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Attendance = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_callups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_callups_AspNetUsers_CalledUpByUserId",
                        column: x => x.CalledUpByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_callups_AspNetUsers_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_callups_team_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "team_matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_posts_AuthorId_IsPublished_CreatedAt",
                table: "posts",
                columns: new[] { "AuthorId", "IsPublished", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_posts_IsPublished_CreatedAt",
                table: "posts",
                columns: new[] { "IsPublished", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_administration_audit_entries_AdministratorId",
                table: "administration_audit_entries",
                column: "AdministratorId");

            migrationBuilder.CreateIndex(
                name: "IX_administration_audit_entries_CreatedAt",
                table: "administration_audit_entries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_administration_audit_entries_TargetUserId",
                table: "administration_audit_entries",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_post_attachments_PostId",
                table: "post_attachments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_team_assignments_AssignedByUserId",
                table: "team_assignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_assignments_TeamId",
                table: "team_assignments",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_team_assignments_UserId_TeamId",
                table: "team_assignments",
                columns: new[] { "UserId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_board_attachments_BoardPostId",
                table: "team_board_attachments",
                column: "BoardPostId");

            migrationBuilder.CreateIndex(
                name: "IX_team_board_posts_AuthorId",
                table: "team_board_posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_team_board_posts_TeamId_CreatedAt",
                table: "team_board_posts",
                columns: new[] { "TeamId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_team_callups_CalledUpByUserId",
                table: "team_callups",
                column: "CalledUpByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_callups_MatchId_PlayerId",
                table: "team_callups",
                columns: new[] { "MatchId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_callups_PlayerId",
                table: "team_callups",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_team_events_CreatedByUserId",
                table: "team_events",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_events_TeamId_StartsAt",
                table: "team_events",
                columns: new[] { "TeamId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_team_matches_CreatedByUserId",
                table: "team_matches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_matches_TeamId_StartsAt",
                table: "team_matches",
                columns: new[] { "TeamId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_team_player_memberships_AddedByUserId",
                table: "team_player_memberships",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_player_memberships_TeamId_UserId",
                table: "team_player_memberships",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_player_memberships_UserId",
                table: "team_player_memberships",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_posts_AspNetUsers_AuthorId",
                table: "posts",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_posts_AspNetUsers_AuthorId",
                table: "posts");

            migrationBuilder.DropTable(
                name: "administration_audit_entries");

            migrationBuilder.DropTable(
                name: "post_attachments");

            migrationBuilder.DropTable(
                name: "team_assignments");

            migrationBuilder.DropTable(
                name: "team_board_attachments");

            migrationBuilder.DropTable(
                name: "team_callups");

            migrationBuilder.DropTable(
                name: "team_events");

            migrationBuilder.DropTable(
                name: "team_player_memberships");

            migrationBuilder.DropTable(
                name: "team_board_posts");

            migrationBuilder.DropTable(
                name: "team_matches");

            migrationBuilder.DropIndex(
                name: "IX_posts_AuthorId_IsPublished_CreatedAt",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_posts_IsPublished_CreatedAt",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "AvatarStorageKey",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredTeam",
                table: "AspNetUsers");
        }
    }
}
