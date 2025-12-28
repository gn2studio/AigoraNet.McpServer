using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AigoraNet.Common.Migrations
{
    /// <inheritdoc />
    public partial class handlersettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    BoardMasterId = table.Column<string>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardMasters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    MasterCode = table.Column<string>(type: "varchar(255)", nullable: false),
                    BoardType = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    Title = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    OwnerId = table.Column<string>(type: "char(36)", nullable: true),
                    Section = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    Site = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Seq = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_BoardMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<string>(type: "char(36)", nullable: true),
                    OwnerId = table.Column<string>(type: "char(36)", nullable: false),
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
                    table.PrimaryKey("PK_Comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardContents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    MasterId = table.Column<string>(type: "char(36)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryId = table.Column<string>(type: "char(36)", nullable: false),
                    AnswerDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnwerOwnerId = table.Column<string>(type: "char(36)", nullable: true),
                    ReadCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    OwnerId = table.Column<string>(type: "char(36)", nullable: true),
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
                    table.PrimaryKey("PK_BoardContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardContents_BoardCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BoardCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardContents_BoardMasters_MasterId",
                        column: x => x.MasterId,
                        principalTable: "BoardMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CommentId = table.Column<string>(type: "char(36)", nullable: false),
                    OwnerId = table.Column<string>(type: "char(36)", nullable: false),
                    HistoryType = table.Column<byte>(type: "tinyint", nullable: false),
                    RegistDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentHistory_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardContents_CategoryId",
                table: "BoardContents",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardContents_MasterId",
                table: "BoardContents",
                column: "MasterId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMasters_MasterCode",
                table: "BoardMasters",
                column: "MasterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentHistory_CommentId_OwnerId",
                table: "CommentHistory",
                columns: new[] { "CommentId", "OwnerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardContents");

            migrationBuilder.DropTable(
                name: "CommentHistory");

            migrationBuilder.DropTable(
                name: "BoardCategories");

            migrationBuilder.DropTable(
                name: "BoardMasters");

            migrationBuilder.DropTable(
                name: "Comments");
        }
    }
}
