using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_FullSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Items: shrink Category to varchar(50) if still varchar(100) ────────
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Items'
                          AND column_name = 'Category'
                          AND character_maximum_length = 100
                    ) THEN
                        ALTER TABLE ""Items"" ALTER COLUMN ""Category"" TYPE character varying(50);
                    END IF;
                END
                $$");

            // ── Items: add missing columns (idempotent) ───────────────────────────
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""Description"" character varying(2000) NOT NULL DEFAULT ''");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""ImageUrls"" text[] NOT NULL DEFAULT '{}'::text[]");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""InstantBookEnabled"" boolean NOT NULL DEFAULT FALSE");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""Location"" character varying(200) NOT NULL DEFAULT ''");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""OwnerId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS handover_options character varying(500) NOT NULL DEFAULT ''");

            // ── Items: clean up zero-UUID OwnerId rows before adding FK ──────────
            migrationBuilder.Sql(@"DELETE FROM ""Items"" WHERE ""OwnerId"" = '00000000-0000-0000-0000-000000000000'");

            // ── Items: OwnerId index ───────────────────────────────────────────────
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Items_OwnerId"" ON ""Items"" (""OwnerId"")");

            // ── Items: OwnerId FK ──────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'FK_Items_Users_OwnerId'
                          AND table_name = 'Items'
                    ) THEN
                        ALTER TABLE ""Items"" ADD CONSTRAINT ""FK_Items_Users_OwnerId""
                            FOREIGN KEY (""OwnerId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE;
                    END IF;
                END
                $$");

            // ── ItemBlockedDates table ─────────────────────────────────────────────
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""ItemBlockedDates"" (
                    ""Id"" uuid NOT NULL,
                    ""ItemId"" uuid NOT NULL,
                    ""DateUtc"" timestamp with time zone NOT NULL,
                    ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_ItemBlockedDates"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_ItemBlockedDates_Items_ItemId""
                        FOREIGN KEY (""ItemId"") REFERENCES ""Items"" (""Id"") ON DELETE CASCADE
                )");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_ItemBlockedDates_ItemId_DateUtc"" ON ""ItemBlockedDates"" (""ItemId"", ""DateUtc"")");

            // ── Wishlists table ────────────────────────────────────────────────────
            // Drop old composite-PK version if it exists without an Id column, then
            // recreate with Id PK and unique index on (UserId, ItemId).
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables WHERE table_name = 'Wishlists'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Wishlists' AND column_name = 'Id'
                    ) THEN
                        DROP TABLE ""Wishlists"";
                    END IF;
                END
                $$");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Wishlists"" (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""ItemId"" uuid NOT NULL,
                    ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Wishlists"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Wishlists_Users_UserId""
                        FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Wishlists_Items_ItemId""
                        FOREIGN KEY (""ItemId"") REFERENCES ""Items"" (""Id"") ON DELETE CASCADE
                )");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Wishlists_ItemId"" ON ""Wishlists"" (""ItemId"")");
            migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Wishlists_UserId_ItemId"" ON ""Wishlists"" (""UserId"", ""ItemId"")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP CONSTRAINT IF EXISTS ""FK_Items_Users_OwnerId""");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ItemBlockedDates""");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Wishlists""");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Items_OwnerId""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""Description""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""ImageUrls""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""InstantBookEnabled""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""Location""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""OwnerId""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS handover_options");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ALTER COLUMN ""Category"" TYPE character varying(100)");
        }
    }
}
