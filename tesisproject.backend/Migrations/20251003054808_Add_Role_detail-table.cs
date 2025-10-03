using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Add_Role_detailtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_MemberRoleType_MemberRoleId",
                table: "GroupMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MemberRoleType",
                table: "MemberRoleType");

            migrationBuilder.RenameTable(
                name: "MemberRoleType",
                newName: "MemberRoleTypes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemberRoleTypes",
                table: "MemberRoleTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_MemberRoleTypes_MemberRoleId",
                table: "GroupMembers",
                column: "MemberRoleId",
                principalTable: "MemberRoleTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_MemberRoleTypes_MemberRoleId",
                table: "GroupMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MemberRoleTypes",
                table: "MemberRoleTypes");

            migrationBuilder.RenameTable(
                name: "MemberRoleTypes",
                newName: "MemberRoleType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemberRoleType",
                table: "MemberRoleType",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_MemberRoleType_MemberRoleId",
                table: "GroupMembers",
                column: "MemberRoleId",
                principalTable: "MemberRoleType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
