using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Projello.Api.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsOnProjectsSoftDeleteAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Payments move from clients to projects. Keep any amounts recorded on a client
            // by adding them to that client's first project, so nothing is lost.
            migrationBuilder.Sql(@"
                UPDATE ""Projects"" p
                SET ""TotalPaid"" = COALESCE(p.""TotalPaid"", 0) + c.""TotalPaid"",
                    ""Outstanding"" = COALESCE(p.""Outstanding"", 0) + c.""Outstanding""
                FROM ""Clients"" c
                WHERE p.""ClientID"" = c.""ClientID""
                  AND (c.""TotalPaid"" <> 0 OR c.""Outstanding"" <> 0)
                  AND p.""ProjectID"" = (SELECT MIN(p2.""ProjectID"") FROM ""Projects"" p2 WHERE p2.""ClientID"" = c.""ClientID"");");

            // Empty project payments become 0 before the columns are made required
            migrationBuilder.Sql(@"
                UPDATE ""Projects"" SET ""TotalPaid"" = 0 WHERE ""TotalPaid"" IS NULL;
                UPDATE ""Projects"" SET ""Outstanding"" = 0 WHERE ""Outstanding"" IS NULL;");

            migrationBuilder.DropColumn(
                name: "Outstanding",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "TotalPaid",
                table: "Clients");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPaid",
                table: "Projects",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Outstanding",
                table: "Projects",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedById",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedById",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Clients");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPaid",
                table: "Projects",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Outstanding",
                table: "Projects",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "Outstanding",
                table: "Clients",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPaid",
                table: "Clients",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
