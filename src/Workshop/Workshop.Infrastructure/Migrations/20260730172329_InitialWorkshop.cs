using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkshop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workshop");

            migrationBuilder.CreateTable(
                name: "budgets",
                schema: "workshop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    total_price_cents = table.Column<int>(type: "int", nullable: false),
                    is_approved = table.Column<bool>(type: "bit", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    approved_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budgets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_orders",
                schema: "workshop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    os_code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vehicle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    created_by_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "budget_consumables",
                schema: "workshop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    consumable_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price_cents = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    budget_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_consumables", x => x.id);
                    table.ForeignKey(
                        name: "FK_budget_consumables_budgets_budget_id",
                        column: x => x.budget_id,
                        principalSchema: "workshop",
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "budget_jobs",
                schema: "workshop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price_cents = table.Column<int>(type: "int", nullable: false),
                    is_executed = table.Column<bool>(type: "bit", nullable: false),
                    executed_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    budget_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_budget_jobs_budgets_budget_id",
                        column: x => x.budget_id,
                        principalSchema: "workshop",
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "budget_parts",
                schema: "workshop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    part_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price_cents = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    budget_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_parts", x => x.id);
                    table.ForeignKey(
                        name: "FK_budget_parts_budgets_budget_id",
                        column: x => x.budget_id,
                        principalSchema: "workshop",
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_order_histories",
                schema: "workshop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    changed_by_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    service_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_order_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_order_histories_service_orders_service_order_id",
                        column: x => x.service_order_id,
                        principalSchema: "workshop",
                        principalTable: "service_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_order_jobs",
                schema: "workshop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    job_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    service_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_order_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_order_jobs_service_orders_service_order_id",
                        column: x => x.service_order_id,
                        principalSchema: "workshop",
                        principalTable: "service_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_order_parts",
                schema: "workshop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    part_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    service_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_order_parts", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_order_parts_service_orders_service_order_id",
                        column: x => x.service_order_id,
                        principalSchema: "workshop",
                        principalTable: "service_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_budget_consumables_budget_id",
                schema: "workshop",
                table: "budget_consumables",
                column: "budget_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_jobs_budget_id",
                schema: "workshop",
                table: "budget_jobs",
                column: "budget_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_parts_budget_id",
                schema: "workshop",
                table: "budget_parts",
                column: "budget_id");

            migrationBuilder.CreateIndex(
                name: "IX_budgets_service_order_id",
                schema: "workshop",
                table: "budgets",
                column: "service_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_order_histories_service_order_id",
                schema: "workshop",
                table: "service_order_histories",
                column: "service_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_order_jobs_service_order_id",
                schema: "workshop",
                table: "service_order_jobs",
                column: "service_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_order_parts_service_order_id",
                schema: "workshop",
                table: "service_order_parts",
                column: "service_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_orders_status",
                schema: "workshop",
                table: "service_orders",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_consumables",
                schema: "workshop");

            migrationBuilder.DropTable(
                name: "budget_jobs",
                schema: "workshop");

            migrationBuilder.DropTable(
                name: "budget_parts",
                schema: "workshop");

            migrationBuilder.DropTable(
                name: "service_order_histories",
                schema: "workshop");

            migrationBuilder.DropTable(
                name: "service_order_jobs",
                schema: "workshop");

            migrationBuilder.DropTable(
                name: "service_order_parts",
                schema: "workshop");

            migrationBuilder.DropTable(
                name: "budgets",
                schema: "workshop");

            migrationBuilder.DropTable(
                name: "service_orders",
                schema: "workshop");
        }
    }
}
