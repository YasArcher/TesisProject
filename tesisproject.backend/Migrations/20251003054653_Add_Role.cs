using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tesisproject.backend.Migrations
{
    /// <inheritdoc />
    public partial class Add_Role : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberRole",
                table: "GroupMembers");

            migrationBuilder.AddColumn<int>(
                name: "MemberRoleId",
                table: "GroupMembers",
                type: "int",
                maxLength: 60,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MemberRoleType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberRoleType", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_MemberRoleId",
                table: "GroupMembers",
                column: "MemberRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_MemberRoleType_MemberRoleId",
                table: "GroupMembers",
                column: "MemberRoleId",
                principalTable: "MemberRoleType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_MemberRoleType_MemberRoleId",
                table: "GroupMembers");

            migrationBuilder.DropTable(
                name: "MemberRoleType");

            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_MemberRoleId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "MemberRoleId",
                table: "GroupMembers");

            migrationBuilder.AddColumn<string>(
                name: "MemberRole",
                table: "GroupMembers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }
    }
}
