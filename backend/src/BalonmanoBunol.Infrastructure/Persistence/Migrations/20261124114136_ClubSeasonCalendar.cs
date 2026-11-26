using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalonmanoBunol.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClubSeasonCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "team_events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "team_weekly_training_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    IsEmpty = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_weekly_training_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_weekly_training_plans_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_weekly_training_plans_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_weekly_training_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Venue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_weekly_training_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_weekly_training_slots_team_weekly_training_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "team_weekly_training_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_training_exceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Venue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_training_exceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_training_exceptions_team_weekly_training_slots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "team_weekly_training_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_events_TeamId_IsCancelled_StartsAt",
                table: "team_events",
                columns: new[] { "TeamId", "IsCancelled", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_team_training_exceptions_SlotId_Date",
                table: "team_training_exceptions",
                columns: new[] { "SlotId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_weekly_training_plans_CreatedByUserId",
                table: "team_weekly_training_plans",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_weekly_training_plans_TeamId_EffectiveFrom",
                table: "team_weekly_training_plans",
                columns: new[] { "TeamId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_weekly_training_slots_PlanId_DayOfWeek_StartsAt",
                table: "team_weekly_training_slots",
                columns: new[] { "PlanId", "DayOfWeek", "StartsAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_training_exceptions");

            migrationBuilder.DropTable(
                name: "team_weekly_training_slots");

            migrationBuilder.DropTable(
                name: "team_weekly_training_plans");

            migrationBuilder.DropIndex(
                name: "IX_team_events_TeamId_IsCancelled_StartsAt",
                table: "team_events");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "team_events");
        }
    }
}
