using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acase_activity",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    case_id = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acase_activity", x => x.id);
                    table.CheckConstraint("activity_message_not_blank", "length(trim(message)) > 0");
                    table.ForeignKey(
                        name: "FK_acase_activity_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acase_activity_case_id",
                table: "acase_activity",
                column: "case_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acase_activity");
        }
    }
}
