using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeNotificationPolicyTimingOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ReminderAfterHours",
                table: "NotificationPolicies",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "EscalationAfterHours",
                table: "NotificationPolicies",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "NotificationPolicies"
                SET "ReminderAfterHours" = 24
                WHERE "ReminderAfterHours" IS NULL;

                UPDATE "NotificationPolicies"
                SET "EscalationAfterHours" = 72
                WHERE "EscalationAfterHours" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ReminderAfterHours",
                table: "NotificationPolicies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EscalationAfterHours",
                table: "NotificationPolicies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
