using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Customers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "customers");

            migrationBuilder.CreateTable(
                name: "clients",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    cpf = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    address_street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    address_city = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    address_state = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    address_country = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    address_zipcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    client_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    license_plate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    mark = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    color = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    year_fabrication = table.Column<int>(type: "int", nullable: false),
                    year_model = table.Column<int>(type: "int", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "FK_vehicles_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "customers",
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clients_cnpj",
                schema: "customers",
                table: "clients",
                column: "cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_clients_cpf",
                schema: "customers",
                table: "clients",
                column: "cpf");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_client_id",
                schema: "customers",
                table: "vehicles",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_license_plate",
                schema: "customers",
                table: "vehicles",
                column: "license_plate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "clients",
                schema: "customers");
        }
    }
}
