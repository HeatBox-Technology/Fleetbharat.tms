using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetBharat.TMSService.Migrations
{
    public partial class AddIsDeletedToRouteMaster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "TMS",
                table: "mst_route",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "TMS",
                table: "mst_route");
        }
    }
}
