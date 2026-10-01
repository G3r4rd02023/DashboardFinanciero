using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashboardFinanciero.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                    table.UniqueConstraint("AK_Categorias_Id_Tipo", x => new { x.Id, x.Tipo });
                });

            migrationBuilder.CreateTable(
                name: "Transacciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CategoriaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transacciones_Categorias_CategoriaId_Tipo",
                        columns: x => new { x.CategoriaId, x.Tipo },
                        principalTable: "Categorias",
                        principalColumns: new[] { "Id", "Tipo" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_CategoriaId",
                table: "Transacciones",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_CategoriaId_Tipo",
                table: "Transacciones",
                columns: new[] { "CategoriaId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_Fecha",
                table: "Transacciones",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transacciones");

            migrationBuilder.DropTable(
                name: "Categorias");
        }
    }
}
