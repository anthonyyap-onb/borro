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

            // Create index on Wishlists.ItemId if not exists
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Wishlists_ItemId"" ON ""Wishlists"" (""ItemId"")");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Wishlists""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""Description""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""ImageUrls""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""InstantBookEnabled""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""Location""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS handover_options");
        }
    }
}
