using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NIU.ACH_AI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationCountersToExperimentState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedEvaluations",
                table: "ExperimentState",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalEvaluations",
                table: "ExperimentState",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedEvaluations",
                table: "ExperimentState");

            migrationBuilder.DropColumn(
                name: "TotalEvaluations",
                table: "ExperimentState");
        }
    }
}
