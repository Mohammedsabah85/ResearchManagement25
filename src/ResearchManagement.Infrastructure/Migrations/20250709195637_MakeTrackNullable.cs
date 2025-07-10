using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResearchManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTrackNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
           name: "Track",
           table: "Researches",
           type: "int",
           nullable: true, // السماح بقيم null
           oldClrType: typeof(int),
           oldType: "int");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                        name: "Track",
                        table: "Researches",
                        type: "int",
                        nullable: false,
                        oldClrType: typeof(int),
                        oldType: "int",
                        oldNullable: true);
        }
    }
}
