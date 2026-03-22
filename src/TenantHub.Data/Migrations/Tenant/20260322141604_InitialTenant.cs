using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantHub.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class InitialTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenant_template");

            migrationBuilder.CreateTable(
                name: "Announcements",
                schema: "tenant_template",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileRecords",
                schema: "tenant_template",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                schema: "tenant_template",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                schema: "tenant_template",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ExpiresAt",
                schema: "tenant_template",
                table: "Announcements",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsPinned",
                schema: "tenant_template",
                table: "Announcements",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_FileRecords_UploadedAt",
                schema: "tenant_template",
                table: "FileRecords",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CreatedAt",
                schema: "tenant_template",
                table: "Notes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_AssignedTo",
                schema: "tenant_template",
                table: "TaskItems",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_DueDate",
                schema: "tenant_template",
                table: "TaskItems",
                column: "DueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements",
                schema: "tenant_template");

            migrationBuilder.DropTable(
                name: "FileRecords",
                schema: "tenant_template");

            migrationBuilder.DropTable(
                name: "Notes",
                schema: "tenant_template");

            migrationBuilder.DropTable(
                name: "TaskItems",
                schema: "tenant_template");
        }
    }
}
