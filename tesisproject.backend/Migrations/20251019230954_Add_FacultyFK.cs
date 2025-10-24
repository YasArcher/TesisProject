using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Add_FacultyFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_MemberRoleTypes_MemberRoleId",
                table: "GroupMembers");

            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "MemberRoleId",
                table: "GroupMembers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "Projects");

            migrationBuilder.AlterColumn<int>(
                name: "MemberRoleId",
                table: "GroupMembers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_MemberRoleTypes_MemberRoleId",
                table: "GroupMembers",
                column: "MemberRoleId",
                principalTable: "MemberRoleTypes",
                principalColumn: "Id");
        }
    }
}
