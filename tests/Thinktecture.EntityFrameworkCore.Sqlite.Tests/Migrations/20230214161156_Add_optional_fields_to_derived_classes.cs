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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherKey",
                table: "TestEntitiesWithBaseClass",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestEntities_Id",
                table: "TestEntities",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TestEntities_Id",
                table: "TestEntities");

            migrationBuilder.DropColumn(
                name: "Optional",
                table: "TestEntitiesWithBaseClass");

            migrationBuilder.DropColumn(
                name: "OtherKey",
                table: "TestEntitiesWithBaseClass");
        }
    }
}
