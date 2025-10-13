using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIReview.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "improvement_suggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReviewRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    StartLine = table.Column<int>(type: "INTEGER", nullable: true),
                    EndLine = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalCode = table.Column<string>(type: "TEXT", nullable: true),
                    SuggestedCode = table.Column<string>(type: "TEXT", nullable: true),
                    Reasoning = table.Column<string>(type: "TEXT", nullable: true),
                    ExpectedBenefits = table.Column<string>(type: "TEXT", nullable: true),
                    ImplementationComplexity = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    ImpactAssessment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsAccepted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsIgnored = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    UserFeedback = table.Column<string>(type: "TEXT", nullable: true),
                    AIModelVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_improvement_suggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_improvement_suggestions_review_requests_ReviewRequestId",
                        column: x => x.ReviewRequestId,
                        principalTable: "review_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pr_change_summaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReviewRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangeType = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    DetailedDescription = table.Column<string>(type: "TEXT", nullable: true),
                    KeyChanges = table.Column<string>(type: "TEXT", nullable: true),
                    ImpactAnalysis = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessImpact = table.Column<int>(type: "INTEGER", nullable: false),
                    TechnicalImpact = table.Column<int>(type: "INTEGER", nullable: false),
                    BreakingChangeRisk = table.Column<int>(type: "INTEGER", nullable: false),
                    TestingRecommendations = table.Column<string>(type: "TEXT", nullable: true),
                    DeploymentConsiderations = table.Column<string>(type: "TEXT", nullable: true),
                    DependencyChanges = table.Column<string>(type: "TEXT", nullable: true),
                    PerformanceImpact = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityImpact = table.Column<string>(type: "TEXT", nullable: true),
                    BackwardCompatibility = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentationRequirements = table.Column<string>(type: "TEXT", nullable: true),
                    ChangeStatistics = table.Column<string>(type: "TEXT", nullable: true),
                    AIModelVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pr_change_summaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pr_change_summaries_review_requests_ReviewRequestId",
                        column: x => x.ReviewRequestId,
                        principalTable: "review_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "risk_assessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReviewRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallRiskScore = table.Column<double>(type: "REAL", nullable: false),
                    ComplexityRisk = table.Column<double>(type: "REAL", nullable: false),
                    SecurityRisk = table.Column<double>(type: "REAL", nullable: false),
                    PerformanceRisk = table.Column<double>(type: "REAL", nullable: false),
                    MaintainabilityRisk = table.Column<double>(type: "REAL", nullable: false),
                    TestCoverageRisk = table.Column<double>(type: "REAL", nullable: false),
                    ChangedFilesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangedLinesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskDescription = table.Column<string>(type: "TEXT", nullable: true),
                    MitigationSuggestions = table.Column<string>(type: "TEXT", nullable: true),
                    AIModelVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_risk_assessments_review_requests_ReviewRequestId",
                        column: x => x.ReviewRequestId,
                        principalTable: "review_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_improvement_suggestions_ReviewRequestId",
                table: "improvement_suggestions",
                column: "ReviewRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_pr_change_summaries_ReviewRequestId",
                table: "pr_change_summaries",
                column: "ReviewRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_risk_assessments_ReviewRequestId",
                table: "risk_assessments",
                column: "ReviewRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "improvement_suggestions");

            migrationBuilder.DropTable(
                name: "pr_change_summaries");

            migrationBuilder.DropTable(
                name: "risk_assessments");
        }
    }
}
