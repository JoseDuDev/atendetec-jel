using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atendefy.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_tenant_users_tenants_TenantId",
                schema: "public",
                table: "tenant_users",
                column: "TenantId",
                principalSchema: "public",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_users_tenants_TenantId",
                schema: "public",
                table: "tenant_users");
        }
    }
}
