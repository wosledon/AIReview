using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIReview.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "token_usage_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReviewRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    LLMConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PromptTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletionTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    PromptCost = table.Column<decimal>(type: "decimal(18, 8)", nullable: false),
                    CompletionCost = table.Column<decimal>(type: "decimal(18, 8)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18, 8)", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "INTEGER", nullable: true),
                    IsCached = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token_usage_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_token_usage_records_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                        column: x => x.LLMConfigurationId,
                        principalTable: "LLMConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_token_usage_records_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_token_usage_records_review_requests_ReviewRequestId",
                        column: x => x.ReviewRequestId,
                        principalTable: "review_requests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_token_usage_records_LLMConfigurationId",
                table: "token_usage_records",
                column: "LLMConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_token_usage_records_ProjectId",
                table: "token_usage_records",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_token_usage_records_ReviewRequestId",
                table: "token_usage_records",
                column: "ReviewRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_token_usage_records_UserId",
                table: "token_usage_records",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "token_usage_records");
        }
    }
}
