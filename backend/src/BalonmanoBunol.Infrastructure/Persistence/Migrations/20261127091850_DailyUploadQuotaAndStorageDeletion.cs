using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BalonmanoBunol.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DailyUploadQuotaAndStorageDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "storage_deletion_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_deletion_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "upload_daily_usage",
                columns: table => new
                {
                    usage_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_bytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upload_daily_usage", x => x.usage_date);
                });

            migrationBuilder.CreateIndex(
                name: "ix_storage_deletion_jobs_available_at",
                table: "storage_deletion_jobs",
                column: "available_at");

            migrationBuilder.CreateIndex(
                name: "ix_storage_deletion_jobs_kind_key",
                table: "storage_deletion_jobs",
                columns: new[] { "storage_kind", "storage_key" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO storage_deletion_jobs ("Id", storage_kind, storage_key, attempts, available_at)
                SELECT gen_random_uuid(), 'feed', attachment."StorageKey", 0, now()
                FROM post_attachments AS attachment
                INNER JOIN posts AS post ON post."Id" = attachment."PostId"
                WHERE post."IsPublished" = FALSE OR post."DeletedAt" IS NOT NULL
                ON CONFLICT (storage_kind, storage_key) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "storage_deletion_jobs");

            migrationBuilder.DropTable(
                name: "upload_daily_usage");
        }
    }
}
