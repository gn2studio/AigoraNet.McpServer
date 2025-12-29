using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AigoraNet.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenPromptMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenPromptMappings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    TokenId = table.Column<string>(type: "char(36)", nullable: false),
                    PromptTemplateId = table.Column<string>(type: "char(36)", nullable: false),
                    Condition_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition_UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Condition_DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Condition_RegistDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    Condition_LastUpdate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getutcdate()"),
                    Condition_DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getutcdate()"),
                    Condition_Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    Condition_IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPromptMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenPromptMappings_PromptTemplates_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalTable: "PromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TokenPromptMappings_Tokens_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenPromptMappings_PromptTemplateId",
                table: "TokenPromptMappings",
                column: "PromptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPromptMappings_TokenId_PromptTemplateId",
                table: "TokenPromptMappings",
                columns: new[] { "TokenId", "PromptTemplateId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenPromptMappings");
        }
    }
}
