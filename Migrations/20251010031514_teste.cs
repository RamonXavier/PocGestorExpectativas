using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PocGestorExpectativas.Migrations
{
    /// <inheritdoc />
    public partial class teste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpectationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expectations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalizedBeneficiary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NextExpectedPaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnalysisMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HistoryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expectations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificationField = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BeneficiaryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedBeneficiary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Pago = table.Column<bool>(type: "bit", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ExpectationId",
                table: "AuditLogs",
                column: "ExpectationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PaymentId",
                table: "AuditLogs",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Expectations_NextExpectedPaymentDate",
                table: "Expectations",
                column: "NextExpectedPaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Expectations_NormalizedBeneficiary",
                table: "Expectations",
                column: "NormalizedBeneficiary");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_IdentificationField",
                table: "Payments",
                column: "IdentificationField");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_NormalizedBeneficiary",
                table: "Payments",
                column: "NormalizedBeneficiary");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Pago",
                table: "Payments",
                column: "Pago");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Expectations");

            migrationBuilder.DropTable(
                name: "Payments");
        }
    }
}
