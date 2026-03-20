using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaliphAuctionBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPreRegisteredToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPreRegistered",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPreRegistered",
                table: "Users");
        }
    }
}
