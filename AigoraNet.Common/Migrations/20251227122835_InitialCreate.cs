using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AigoraNet.Common.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Xml = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileMasters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    FileLength = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    RegistDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileBlob = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(512)", nullable: true),
                    PublicURL = table.Column<string>(type: "nvarchar(512)", nullable: true),
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
                    table.PrimaryKey("PK_FileMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogEvent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", nullable: false),
                    IsEmailConfirm = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EmailConfirmDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)10),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NickName = table.Column<string>(type: "nvarchar(80)", nullable: true),
                    Photo = table.Column<string>(type: "varchar(255)", nullable: true),
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
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Members_Email",
                table: "Members",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "FileMasters");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "Members");
        }
    }
}
