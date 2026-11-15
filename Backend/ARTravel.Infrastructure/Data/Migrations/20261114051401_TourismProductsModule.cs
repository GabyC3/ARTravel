using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ARTravel.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TourismProductsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductDestinations",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    DestinationId = table.Column<int>(type: "integer", nullable: false),
                    VisitOrder = table.Column<int>(type: "integer", nullable: false),
                    IsMainDestination = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDestinations", x => new { x.ProductId, x.DestinationId });
                    table.ForeignKey(
                        name: "FK_ProductDestinations_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductDestinations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "ProductDestinations"
                    ("ProductId", "DestinationId", "VisitOrder", "IsMainDestination", "Notes")
                SELECT "Id", "DestinationId", 1, TRUE, NULL
                FROM "Products";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Destinations_DestinationId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_DestinationId",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationPolicy",
                table: "Products",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurationNights",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Excludes",
                table: "Products",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Includes",
                table: "Products",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaximumPassengers",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumPassengers",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                table: "Products",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductDepartures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    DepartureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailableSeats = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDepartures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDepartures_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourismProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    AltText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourismProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourismProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsPublished",
                table: "Products",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDepartures_ProductId_DepartureAt",
                table: "ProductDepartures",
                columns: new[] { "ProductId", "DepartureAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductDestinations_DestinationId",
                table: "ProductDestinations",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDestinations_ProductId_VisitOrder",
                table: "ProductDestinations",
                columns: new[] { "ProductId", "VisitOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TourismProductImages_ProductId_DisplayOrder",
                table: "TourismProductImages",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.Sql(
                """
                UPDATE "Products"
                SET "Code" = 'LEGACY-' || "Id"::text
                WHERE "Code" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "DestinationId",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Products" AS product
                SET "DestinationId" = selected."DestinationId"
                FROM (
                    SELECT DISTINCT ON ("ProductId")
                        "ProductId", "DestinationId"
                    FROM "ProductDestinations"
                    ORDER BY "ProductId", "IsMainDestination" DESC, "VisitOrder", "DestinationId"
                ) AS selected
                WHERE product."Id" = selected."ProductId";
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Products" WHERE "DestinationId" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot revert TourismProductsModule while a product has no destination.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "DestinationId",
                table: "Products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "ProductDepartures");

            migrationBuilder.DropTable(
                name: "ProductDestinations");

            migrationBuilder.DropTable(
                name: "TourismProductImages");

            migrationBuilder.DropIndex(
                name: "IX_Products_Code",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsPublished",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CancellationPolicy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DurationNights",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Excludes",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Includes",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaximumPassengers",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinimumPassengers",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_DestinationId",
                table: "Products",
                column: "DestinationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Destinations_DestinationId",
                table: "Products",
                column: "DestinationId",
                principalTable: "Destinations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
