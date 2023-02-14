using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thinktecture.Migrations
{
    /// <inheritdoc />
    public partial class Addoptionalfieldstoderivedclasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Optional",
                table: "TestEntitiesWithBaseClass",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherKey",
                table: "TestEntitiesWithBaseClass",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Optional",
                table: "TestEntitiesWithBaseClass");

            migrationBuilder.DropColumn(
                name: "OtherKey",
                table: "TestEntitiesWithBaseClass");
        }
    }
}
