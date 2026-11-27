using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseApi.Migrations
{
    /// <inheritdoc />
    public partial class FixCaseActivityName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_acase_activity_cases_case_id",
                table: "acase_activity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_acase_activity",
                table: "acase_activity");

            migrationBuilder.RenameTable(
                name: "acase_activity",
                newName: "case_activity");

            migrationBuilder.RenameIndex(
                name: "IX_acase_activity_case_id",
                table: "case_activity",
                newName: "IX_case_activity_case_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_case_activity",
                table: "case_activity",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_case_activity_cases_case_id",
                table: "case_activity",
                column: "case_id",
                principalTable: "cases",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_case_activity_cases_case_id",
                table: "case_activity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_case_activity",
                table: "case_activity");

            migrationBuilder.RenameTable(
                name: "case_activity",
                newName: "acase_activity");

            migrationBuilder.RenameIndex(
                name: "IX_case_activity_case_id",
                table: "acase_activity",
                newName: "IX_acase_activity_case_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_acase_activity",
                table: "acase_activity",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_acase_activity_cases_case_id",
                table: "acase_activity",
                column: "case_id",
                principalTable: "cases",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
