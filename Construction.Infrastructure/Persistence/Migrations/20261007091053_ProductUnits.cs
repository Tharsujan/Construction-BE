using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name",
                table: "Units",
                column: "Name",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Units" ("Id", "Name", "IsActive", "CreatedAt")
                VALUES
                ('11111111-1111-1111-1111-111111111111', 'Bag', TRUE, NOW()),
                ('22222222-2222-2222-2222-222222222222', 'Ton', TRUE, NOW());
                """);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "CementProducts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "CementProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "CementProducts"
                SET "UnitId" = '22222222-2222-2222-2222-222222222222'
                WHERE "Unit" = 'Ton';

                UPDATE "CementProducts"
                SET "UnitId" = '11111111-1111-1111-1111-111111111111'
                WHERE "UnitId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                table: "CementProducts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "CementProducts");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "CementProducts");

            migrationBuilder.CreateIndex(
                name: "IX_CementProducts_UnitId",
                table: "CementProducts",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_CementProducts_Units_UnitId",
                table: "CementProducts",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CementProducts_Units_UnitId",
                table: "CementProducts");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropIndex(
                name: "IX_CementProducts_UnitId",
                table: "CementProducts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CementProducts");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "CementProducts");

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "CementProducts",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "CementProducts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
