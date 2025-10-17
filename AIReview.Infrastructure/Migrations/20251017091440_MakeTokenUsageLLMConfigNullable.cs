using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIReview.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTokenUsageLLMConfigNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                table: "token_usage_records");

            migrationBuilder.AlterColumn<int>(
                name: "LLMConfigurationId",
                table: "token_usage_records",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                table: "token_usage_records",
                column: "LLMConfigurationId",
                principalTable: "LLMConfigurations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                table: "token_usage_records");

            migrationBuilder.AlterColumn<int>(
                name: "LLMConfigurationId",
                table: "token_usage_records",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_token_usage_records_LLMConfigurations_LLMConfigurationId",
                table: "token_usage_records",
                column: "LLMConfigurationId",
                principalTable: "LLMConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
