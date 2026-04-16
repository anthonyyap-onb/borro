using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemFullSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "Items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int[]>(
                name: "HandoverOptions",
                table: "Items",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<List<string>>(
                name: "ImageUrls",
                table: "Items",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "InstantBookEnabled",
                table: "Items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Items",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ItemBlockedDates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemBlockedDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemBlockedDates_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wishlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wishlists_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Wishlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemBlockedDates_ItemId_DateUtc",
                table: "ItemBlockedDates",
                columns: new[] { "ItemId", "DateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_ItemId",
                table: "Wishlists",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_UserId_ItemId",
                table: "Wishlists",
                columns: new[] { "UserId", "ItemId" },
                unique: true);

            // GIN index on the JSONB Attributes column for efficient JSON path queries.
            migrationBuilder.Sql(
                "CREATE INDEX IX_Items_Attributes_Gin ON \"Items\" USING gin (\"Attributes\" jsonb_path_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS IX_Items_Attributes_Gin;");

            migrationBuilder.DropTable(
                name: "ItemBlockedDates");

            migrationBuilder.DropTable(
                name: "Wishlists");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "HandoverOptions",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "InstantBookEnabled",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
