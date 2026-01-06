using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIU.ACH_AI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSagaCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExperimentState",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CurrentStepIndex = table.Column<int>(type: "int", nullable: false),
                    SerializedConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SerializedInput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SerializedResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HypothesisStepExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefinedHypothesisStepExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvidenceStepExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentState", x => x.CorrelationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperimentState");
        }
    }
}
