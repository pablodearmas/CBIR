using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CBIR.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Hash1 = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false, comment: "Perceptual Hash"),
                    Hash2 = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false, comment: "Color Moment Hash"),
                    ExternalFile = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryImage",
                columns: table => new
                {
                    CategoriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImagesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryImage", x => new { x.CategoriesId, x.ImagesId });
                    table.ForeignKey(
                        name: "FK_CategoryImage_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryImage_Images_ImagesId",
                        column: x => x.ImagesId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryImage_ImagesId",
                table: "CategoryImage",
                column: "ImagesId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ExternalFile",
                table: "Images",
                column: "ExternalFile");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Hash1",
                table: "Images",
                column: "Hash1");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Hash2",
                table: "Images",
                column: "Hash2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryImage");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}
