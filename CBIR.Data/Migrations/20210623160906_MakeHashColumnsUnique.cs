using Microsoft.EntityFrameworkCore.Migrations;

namespace CBIR.Data.Migrations
{
    public partial class MakeHashColumnsUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_Hash1",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_Hash2",
                table: "Images");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Hash1",
                table: "Images",
                column: "Hash1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_Hash2",
                table: "Images",
                column: "Hash2",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_Hash1",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_Hash2",
                table: "Images");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Hash1",
                table: "Images",
                column: "Hash1");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Hash2",
                table: "Images",
                column: "Hash2");
        }
    }
}
