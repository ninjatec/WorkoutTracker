using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutTrackerWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpPagesModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlossaryTerm",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Term = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Example = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTerm", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HelpCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpCategory_HelpCategory_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "HelpCategory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GlossaryTermRelatedTerms",
                columns: table => new
                {
                    GlossaryTermId = table.Column<int>(type: "int", nullable: false),
                    RelatedTermsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTermRelatedTerms", x => new { x.GlossaryTermId, x.RelatedTermsId });
                    table.ForeignKey(
                        name: "FK_GlossaryTermRelatedTerms_GlossaryTerm_GlossaryTermId",
                        column: x => x.GlossaryTermId,
                        principalTable: "GlossaryTerm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GlossaryTermRelatedTerms_GlossaryTerm_RelatedTermsId",
                        column: x => x.RelatedTermsId,
                        principalTable: "GlossaryTerm",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HelpArticle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HelpCategoryId = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    HasVideo = table.Column<bool>(type: "bit", nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrintable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpArticle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpArticle_HelpCategory_HelpCategoryId",
                        column: x => x.HelpCategoryId,
                        principalTable: "HelpCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HelpArticleRelatedArticles",
                columns: table => new
                {
                    HelpArticleId = table.Column<int>(type: "int", nullable: false),
                    RelatedArticlesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpArticleRelatedArticles", x => new { x.HelpArticleId, x.RelatedArticlesId });
                    table.ForeignKey(
                        name: "FK_HelpArticleRelatedArticles_HelpArticle_HelpArticleId",
                        column: x => x.HelpArticleId,
                        principalTable: "HelpArticle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HelpArticleRelatedArticles_HelpArticle_RelatedArticlesId",
                        column: x => x.RelatedArticlesId,
                        principalTable: "HelpArticle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTermRelatedTerms_RelatedTermsId",
                table: "GlossaryTermRelatedTerms",
                column: "RelatedTermsId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpArticle_HelpCategoryId",
                table: "HelpArticle",
                column: "HelpCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpArticleRelatedArticles_RelatedArticlesId",
                table: "HelpArticleRelatedArticles",
                column: "RelatedArticlesId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpCategory_ParentCategoryId",
                table: "HelpCategory",
                column: "ParentCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlossaryTermRelatedTerms");

            migrationBuilder.DropTable(
                name: "HelpArticleRelatedArticles");

            migrationBuilder.DropTable(
                name: "GlossaryTerm");

            migrationBuilder.DropTable(
                name: "HelpArticle");

            migrationBuilder.DropTable(
                name: "HelpCategory");
        }
    }
}
