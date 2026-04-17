using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropSpuriousOwnerIdColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP CONSTRAINT IF EXISTS ""FK_Items_Users_OwnerId""");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Items_OwnerId""");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""OwnerId""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
