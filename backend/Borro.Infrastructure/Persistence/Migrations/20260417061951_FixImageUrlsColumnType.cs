using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixImageUrlsColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ImageUrls was created as `text` by the ExpandItemDomain migration instead of `text[]`.
            // The Phase2_ItemsAndWishlist migration's `ADD COLUMN IF NOT EXISTS` was silently skipped.
            // Must drop the DEFAULT first — PostgreSQL cannot auto-cast ''::text default to text[].
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ALTER COLUMN ""ImageUrls"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Items""
                ALTER COLUMN ""ImageUrls"" TYPE text[]
                USING CASE
                    WHEN ""ImageUrls"" IS NULL OR ""ImageUrls"" = '' THEN ARRAY[]::text[]
                    ELSE ARRAY[""ImageUrls""]
                END;
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ALTER COLUMN ""ImageUrls"" SET DEFAULT '{}';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert text[] back to text (takes first element or empty string)
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ALTER COLUMN ""ImageUrls"" DROP DEFAULT;");
            migrationBuilder.Sql(@"
                ALTER TABLE ""Items""
                ALTER COLUMN ""ImageUrls"" TYPE text
                USING CASE
                    WHEN ""ImageUrls"" IS NULL OR array_length(""ImageUrls"", 1) IS NULL THEN ''
                    ELSE ""ImageUrls""[1]
                END;
            ");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ALTER COLUMN ""ImageUrls"" SET DEFAULT '';");
        }
    }
}
