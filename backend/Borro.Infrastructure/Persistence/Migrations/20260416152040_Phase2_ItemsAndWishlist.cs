using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_ItemsAndWishlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns to Items (idempotent via IF NOT EXISTS)
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""Description"" character varying(2000) NOT NULL DEFAULT ''");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""ImageUrls"" text[] NOT NULL DEFAULT '{}'::text[]");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""InstantBookEnabled"" boolean NOT NULL DEFAULT FALSE");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""Location"" character varying(200) NOT NULL DEFAULT ''");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS ""OwnerId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ADD COLUMN IF NOT EXISTS handover_options character varying(500) NOT NULL DEFAULT ''");

            // Create Wishlists table if not exists
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Wishlists"" (
                    ""UserId"" uuid NOT NULL,
                    ""ItemId"" uuid NOT NULL,
                    ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Wishlists"" PRIMARY KEY (""UserId"", ""ItemId""),
                    CONSTRAINT ""FK_Wishlists_Items_ItemId"" FOREIGN KEY (""ItemId"") REFERENCES ""Items"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Wishlists_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
                )");

            // Create index on Items.OwnerId if not exists
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Items_OwnerId"" ON ""Items"" (""OwnerId"")");

            // Create index on Wishlists.ItemId if not exists
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Wishlists_ItemId"" ON ""Wishlists"" (""ItemId"")");

            // Delete any Items that have the zero-UUID OwnerId (dev/test data with no real owner)
            migrationBuilder.Sql(@"DELETE FROM ""Items"" WHERE ""OwnerId"" = '00000000-0000-0000-0000-000000000000'");

            // Add FK constraint on Items.OwnerId if not exists
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP CONSTRAINT IF EXISTS ""FK_Items_Users_OwnerId""");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Wishlists""");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Items_OwnerId""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""Description""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""ImageUrls""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""InstantBookEnabled""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""Location""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""OwnerId""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS handover_options");
        }
    }
}
