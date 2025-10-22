using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIReview.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGitCredentialEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                table: "token_usage_records");

            migrationBuilder.DropForeignKey(
                name: "FK_token_usage_records_projects_ProjectId",
                table: "token_usage_records");

            migrationBuilder.DropForeignKey(
                name: "FK_token_usage_records_review_requests_ReviewRequestId",
                table: "token_usage_records");

            migrationBuilder.CreateTable(
                name: "git_credentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    EncryptedSecret = table.Column<string>(type: "TEXT", nullable: true),
                    EncryptedPrivateKey = table.Column<string>(type: "TEXT", nullable: true),
                    PublicKey = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_git_credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_git_credentials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_git_credentials_UserId",
                table: "git_credentials",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                table: "token_usage_records",
                column: "LLMConfigurationId",
                principalTable: "LLMConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_token_usage_records_projects_ProjectId",
                table: "token_usage_records",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_token_usage_records_review_requests_ReviewRequestId",
                table: "token_usage_records",
                column: "ReviewRequestId",
                principalTable: "review_requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                table: "token_usage_records");

            migrationBuilder.DropForeignKey(
                name: "FK_token_usage_records_projects_ProjectId",
                table: "token_usage_records");

            migrationBuilder.DropForeignKey(
                name: "FK_token_usage_records_review_requests_ReviewRequestId",
                table: "token_usage_records");

            migrationBuilder.DropTable(
                name: "git_credentials");

            migrationBuilder.AddForeignKey(
                name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                table: "token_usage_records",
                column: "LLMConfigurationId",
                principalTable: "LLMConfigurations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_token_usage_records_projects_ProjectId",
                table: "token_usage_records",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_token_usage_records_review_requests_ReviewRequestId",
                table: "token_usage_records",
                column: "ReviewRequestId",
                principalTable: "review_requests",
                principalColumn: "Id");
        }
    }
}
