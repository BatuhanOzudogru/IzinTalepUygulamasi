using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IzinTalepUygulamasi.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: false),
                    ProcessedByManagerId = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessingDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalLogs_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalLogs_Users_ProcessedByManagerId",
                        column: x => x.ProcessedByManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalLogs_LeaveRequestId",
                table: "ApprovalLogs",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalLogs_ProcessedByManagerId",
                table: "ApprovalLogs",
                column: "ProcessedByManagerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalLogs");
        }
    }
}
