using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ARTravel.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReservationsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DO $$ BEGIN " +
                "IF EXISTS (SELECT 1 FROM \"Reservations\") THEN " +
                "RAISE EXCEPTION 'Reservations contains legacy rows without departure, currency, and reservation number; reconcile them before applying ReservationsModule.'; " +
                "END IF; END $$;");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Passengers_PassengerId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations");

            migrationBuilder.AlterColumn<int>(
                name: "PassengerId",
                table: "Trips",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ReservationPassengerId",
                table: "Trips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Reservations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Reservations",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DepartureId",
                table: "Reservations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Reservations",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Reservations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPassengers",
                table: "Reservations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationDate",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReservationNumber",
                table: "Reservations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Reservations",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Reservations_Id_DepartureId",
                table: "Reservations",
                columns: new[] { "Id", "DepartureId" });

            migrationBuilder.CreateTable(
                name: "ReservationCancellations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReservationId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledByUserId = table.Column<int>(type: "integer", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationCancellations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationCancellations_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationCancellations_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationPassengers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReservationId = table.Column<int>(type: "integer", nullable: false),
                    PassengerId = table.Column<int>(type: "integer", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SpecialRequirements = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PriceType = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationPassengers", x => x.Id);
                    table.UniqueConstraint("AK_ReservationPassengers_Id_ReservationId", x => new { x.Id, x.ReservationId });
                    table.ForeignKey(
                        name: "FK_ReservationPassengers_Passengers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "Passengers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationPassengers_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservationPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReservationId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransactionReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationPayments_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ReservationId_DepartureId",
                table: "Trips",
                columns: new[] { "ReservationId", "DepartureId" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ReservationPassengerId",
                table: "Trips",
                column: "ReservationPassengerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ReservationPassengerId_ReservationId",
                table: "Trips",
                columns: new[] { "ReservationPassengerId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CreatedByUserId",
                table: "Reservations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId_ReservationDate",
                table: "Reservations",
                columns: new[] { "CustomerId", "ReservationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DepartureId_Status",
                table: "Reservations",
                columns: new[] { "DepartureId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ReservationNumber",
                table: "Reservations",
                column: "ReservationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCancellations_CancelledByUserId",
                table: "ReservationCancellations",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCancellations_ReservationId",
                table: "ReservationCancellations",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPassengers_PassengerId",
                table: "ReservationPassengers",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPassengers_Reservation_Document",
                table: "ReservationPassengers",
                columns: new[] { "ReservationId", "DocumentType", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPayments_ReservationId_CreatedAt",
                table: "ReservationPayments",
                columns: new[] { "ReservationId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Departures_DepartureId",
                table: "Reservations",
                column: "DepartureId",
                principalTable: "Departures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_CreatedByUserId",
                table: "Reservations",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Passengers_PassengerId",
                table: "Trips",
                column: "PassengerId",
                principalTable: "Passengers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_ReservationPassengers_ReservationPassengerId",
                table: "Trips",
                column: "ReservationPassengerId",
                principalTable: "ReservationPassengers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_ReservationPassengers_ReservationPassengerId_Reservat~",
                table: "Trips",
                columns: new[] { "ReservationPassengerId", "ReservationId" },
                principalTable: "ReservationPassengers",
                principalColumns: new[] { "Id", "ReservationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Reservations_ReservationId_DepartureId",
                table: "Trips",
                columns: new[] { "ReservationId", "DepartureId" },
                principalTable: "Reservations",
                principalColumns: new[] { "Id", "DepartureId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DO $$ BEGIN " +
                "IF EXISTS (SELECT 1 FROM \"Reservations\") OR " +
                "EXISTS (SELECT 1 FROM \"ReservationPassengers\") OR " +
                "EXISTS (SELECT 1 FROM \"ReservationPayments\") OR " +
                "EXISTS (SELECT 1 FROM \"ReservationCancellations\") THEN " +
                "RAISE EXCEPTION 'Reservation data exists; archive it before reverting ReservationsModule.'; " +
                "END IF; END $$;");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Departures_DepartureId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Passengers_PassengerId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_ReservationPassengers_ReservationPassengerId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_ReservationPassengers_ReservationPassengerId_Reservat~",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Reservations_ReservationId_DepartureId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "ReservationCancellations");

            migrationBuilder.DropTable(
                name: "ReservationPassengers");

            migrationBuilder.DropTable(
                name: "ReservationPayments");

            migrationBuilder.DropIndex(
                name: "IX_Trips_ReservationId_DepartureId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_ReservationPassengerId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_ReservationPassengerId_ReservationId",
                table: "Trips");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Reservations_Id_DepartureId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CustomerId_ReservationDate",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_DepartureId_Status",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ReservationNumber",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReservationPassengerId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DepartureId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "NumberOfPassengers",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReservationDate",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReservationNumber",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Reservations");

            migrationBuilder.AlterColumn<int>(
                name: "PassengerId",
                table: "Trips",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Passengers_PassengerId",
                table: "Trips",
                column: "PassengerId",
                principalTable: "Passengers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
