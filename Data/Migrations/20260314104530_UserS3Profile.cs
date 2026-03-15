using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AqlaAwsS3Manager.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserS3Profile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserS3Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BucketName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccessKeyEncrypted = table.Column<string>(type: "TEXT", nullable: false),
                    SecretKeyEncrypted = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserS3Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserS3Profiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserS3Profiles_UserId_DisplayName",
                table: "UserS3Profiles",
                columns: new[] { "UserId", "DisplayName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserS3Profiles");
        }
    }
}
