using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AigoraNet.Common.Migrations
{
    /// <inheritdoc />
    public partial class apisettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Locale = table.Column<string>(type: "nvarchar(10)", nullable: true),
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
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    TokenKey = table.Column<string>(type: "varchar(128)", nullable: false),
                    MemberId = table.Column<string>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tokens_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeywordPrompts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    IsRegex = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Description = table.Column<string>(type: "nvarchar(512)", nullable: true),
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
                    table.PrimaryKey("PK_KeywordPrompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeywordPrompts_PromptTemplates_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalTable: "PromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeywordPrompts_Keyword_Locale",
                table: "KeywordPrompts",
                columns: new[] { "Keyword", "Locale" },
                unique: true,
                filter: "[Locale] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KeywordPrompts_PromptTemplateId",
                table: "KeywordPrompts",
                column: "PromptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_Name_Version_Locale",
                table: "PromptTemplates",
                columns: new[] { "Name", "Version", "Locale" },
                unique: true,
                filter: "[Locale] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_MemberId",
                table: "Tokens",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_TokenKey",
                table: "Tokens",
                column: "TokenKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeywordPrompts");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "PromptTemplates");
        }
    }
}
