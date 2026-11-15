using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ARTravel.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeparturesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DO $$ BEGIN " +
                "IF EXISTS (SELECT 1 FROM \"ProductDepartures\") THEN " +
                "RAISE EXCEPTION 'ProductDepartures contains legacy rows; migrate or archive them before applying DeparturesModule.'; " +
                "END IF; END $$;");

            migrationBuilder.DropTable(
                name: "ProductDepartures");

            migrationBuilder.AddColumn<int>(
                name: "DepartureId",
                table: "Trips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ItineraryId = table.Column<int>(type: "integer", nullable: false),
                    DepartureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MeetingPoint = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MeetingTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    MinimumPassengers = table.Column<int>(type: "integer", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departures_Itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalTable: "Itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departures_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeparturePrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DepartureId = table.Column<int>(type: "integer", nullable: false),
                    PriceType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeparturePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeparturePrices_Departures_DepartureId",
                        column: x => x.DepartureId,
                        principalTable: "Departures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DepartureId",
                table: "Trips",
                column: "DepartureId");

            migrationBuilder.CreateIndex(
                name: "IX_DeparturePrices_DepartureId_PriceType",
                table: "DeparturePrices",
                columns: new[] { "DepartureId", "PriceType" });

            migrationBuilder.CreateIndex(
                name: "IX_Departures_ItineraryId",
                table: "Departures",
                column: "ItineraryId");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_ProductId_DepartureDate",
                table: "Departures",
                columns: new[] { "ProductId", "DepartureDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Departures_DepartureId",
                table: "Trips",
                column: "DepartureId",
                principalTable: "Departures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DO $$ BEGIN " +
                "IF EXISTS (SELECT 1 FROM \"Departures\") OR " +
                "EXISTS (SELECT 1 FROM \"DeparturePrices\") OR " +
                "EXISTS (SELECT 1 FROM \"Trips\" WHERE \"DepartureId\" IS NOT NULL) THEN " +
                "RAISE EXCEPTION 'Departure data exists; archive it before reverting DeparturesModule.'; " +
                "END IF; END $$;");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Departures_DepartureId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "DeparturePrices");

            migrationBuilder.DropTable(
                name: "Departures");

            migrationBuilder.DropIndex(
                name: "IX_Trips_DepartureId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DepartureId",
                table: "Trips");

            migrationBuilder.CreateTable(
                name: "ProductDepartures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AvailableSeats = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepartureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ReturnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ProductDepartures_ProductId_DepartureAt",
                table: "ProductDepartures",
                columns: new[] { "ProductId", "DepartureAt" });
        }
    }
}
