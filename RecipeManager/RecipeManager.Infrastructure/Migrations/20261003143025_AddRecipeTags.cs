using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "Recipes",
                type: "character varying(40)[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Recipes_Tags_Count",
                table: "Recipes",
                sql: "cardinality(\"Tags\") <= 20");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Recipes_Tags_Count",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Recipes");
        }
    }
}
