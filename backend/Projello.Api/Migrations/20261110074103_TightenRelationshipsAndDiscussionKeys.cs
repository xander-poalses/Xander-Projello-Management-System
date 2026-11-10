using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projello.Api.Migrations
{
    /// <inheritdoc />
    public partial class TightenRelationshipsAndDiscussionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Companies_CompanyID",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Clients_ClientID",
                table: "Projects");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateReactions_UpdateId",
                table: "UpdateReactions",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateReactions_UserId",
                table: "UpdateReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateComments_UpdateId",
                table: "UpdateComments",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateComments_UserId",
                table: "UpdateComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUpdates_ProjectId",
                table: "ProjectUpdates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUpdates_UserId",
                table: "ProjectUpdates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyInvites_Code",
                table: "CompanyInvites",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Companies_CompanyID",
                table: "Clients",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Clients_ClientID",
                table: "Projects",
                column: "ClientID",
                principalTable: "Clients",
                principalColumn: "ClientID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUpdates_AspNetUsers_UserId",
                table: "ProjectUpdates",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUpdates_Projects_ProjectId",
                table: "ProjectUpdates",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateComments_AspNetUsers_UserId",
                table: "UpdateComments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateComments_ProjectUpdates_UpdateId",
                table: "UpdateComments",
                column: "UpdateId",
                principalTable: "ProjectUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateReactions_AspNetUsers_UserId",
                table: "UpdateReactions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateReactions_ProjectUpdates_UpdateId",
                table: "UpdateReactions",
                column: "UpdateId",
                principalTable: "ProjectUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Companies_CompanyID",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Clients_ClientID",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUpdates_AspNetUsers_UserId",
                table: "ProjectUpdates");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectUpdates_Projects_ProjectId",
                table: "ProjectUpdates");

            migrationBuilder.DropForeignKey(
                name: "FK_UpdateComments_AspNetUsers_UserId",
                table: "UpdateComments");

            migrationBuilder.DropForeignKey(
                name: "FK_UpdateComments_ProjectUpdates_UpdateId",
                table: "UpdateComments");

            migrationBuilder.DropForeignKey(
                name: "FK_UpdateReactions_AspNetUsers_UserId",
                table: "UpdateReactions");

            migrationBuilder.DropForeignKey(
                name: "FK_UpdateReactions_ProjectUpdates_UpdateId",
                table: "UpdateReactions");

            migrationBuilder.DropIndex(
                name: "IX_UpdateReactions_UpdateId",
                table: "UpdateReactions");

            migrationBuilder.DropIndex(
                name: "IX_UpdateReactions_UserId",
                table: "UpdateReactions");

            migrationBuilder.DropIndex(
                name: "IX_UpdateComments_UpdateId",
                table: "UpdateComments");

            migrationBuilder.DropIndex(
                name: "IX_UpdateComments_UserId",
                table: "UpdateComments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectUpdates_ProjectId",
                table: "ProjectUpdates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectUpdates_UserId",
                table: "ProjectUpdates");

            migrationBuilder.DropIndex(
                name: "IX_CompanyInvites_Code",
                table: "CompanyInvites");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Companies_CompanyID",
                table: "Clients",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "CompanyID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Clients_ClientID",
                table: "Projects",
                column: "ClientID",
                principalTable: "Clients",
                principalColumn: "ClientID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
