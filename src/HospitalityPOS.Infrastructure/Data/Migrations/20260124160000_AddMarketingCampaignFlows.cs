using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalityPOS.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddMarketingCampaignFlows : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create CampaignFlows table
        migrationBuilder.CreateTable(
            name: "CampaignFlows",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Type = table.Column<int>(type: "int", nullable: false),
                Trigger = table.Column<int>(type: "int", nullable: false),
                TriggerDaysOffset = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                MinimumTier = table.Column<int>(type: "int", nullable: true),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                MaxEnrollmentsPerMember = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                CooldownDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignFlows", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignFlows_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create CampaignFlowSteps table
        migrationBuilder.CreateTable(
            name: "CampaignFlowSteps",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FlowId = table.Column<int>(type: "int", nullable: false),
                StepOrder = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                DelayDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                DelayHours = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                Channel = table.Column<int>(type: "int", nullable: false),
                Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                AwardPoints = table.Column<int>(type: "int", nullable: true),
                DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                DiscountValidDays = table.Column<int>(type: "int", nullable: true),
                ConditionType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ConditionValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignFlowSteps", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignFlowSteps_CampaignFlows_FlowId",
                    column: x => x.FlowId,
                    principalTable: "CampaignFlows",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create MemberFlowEnrollments table
        migrationBuilder.CreateTable(
            name: "MemberFlowEnrollments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MemberId = table.Column<int>(type: "int", nullable: false),
                FlowId = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                CurrentStepId = table.Column<int>(type: "int", nullable: true),
                NextStepScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                TriggerDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                ContextJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemberFlowEnrollments", x => x.Id);
                table.ForeignKey(
                    name: "FK_MemberFlowEnrollments_LoyaltyMembers_MemberId",
                    column: x => x.MemberId,
                    principalTable: "LoyaltyMembers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MemberFlowEnrollments_CampaignFlows_FlowId",
                    column: x => x.FlowId,
                    principalTable: "CampaignFlows",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MemberFlowEnrollments_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create FlowStepExecutions table
        migrationBuilder.CreateTable(
            name: "FlowStepExecutions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EnrollmentId = table.Column<int>(type: "int", nullable: false),
                StepId = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Channel = table.Column<int>(type: "int", nullable: false),
                ExternalMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                PointsAwarded = table.Column<int>(type: "int", nullable: true),
                DiscountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                DiscountExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                WasSkipped = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                SkipReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RenderedSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                RenderedContent = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FlowStepExecutions", x => x.Id);
                table.ForeignKey(
                    name: "FK_FlowStepExecutions_MemberFlowEnrollments_EnrollmentId",
                    column: x => x.EnrollmentId,
                    principalTable: "MemberFlowEnrollments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FlowStepExecutions_CampaignFlowSteps_StepId",
                    column: x => x.StepId,
                    principalTable: "CampaignFlowSteps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Create CampaignEmailTemplates table
        migrationBuilder.CreateTable(
            name: "CampaignEmailTemplates",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                TextBody = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignEmailTemplates", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignEmailTemplates_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create CampaignSmsTemplates table
        migrationBuilder.CreateTable(
            name: "CampaignSmsTemplates",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Content = table.Column<string>(type: "nvarchar(480)", maxLength: 480, nullable: false),
                Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignSmsTemplates", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignSmsTemplates_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create CampaignFlowConfigurations table
        migrationBuilder.CreateTable(
            name: "CampaignFlowConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StoreId = table.Column<int>(type: "int", nullable: true),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                EmailEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                SmsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                DefaultFromEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                DefaultFromName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                DefaultSmsFrom = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                MaxMessagesPerMemberPerDay = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                QuietHoursStart = table.Column<int>(type: "int", nullable: false, defaultValue: 21),
                QuietHoursEnd = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                WinBackInactivityDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                BirthdayFlowStartDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                PointsExpiryNotifyDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                MaxRetryAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                RetryDelayMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignFlowConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignFlowConfigurations_Stores_StoreId",
                    column: x => x.StoreId,
                    principalTable: "Stores",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes for CampaignFlows
        migrationBuilder.CreateIndex(
            name: "IX_CampaignFlows_Type",
            table: "CampaignFlows",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignFlows_Trigger",
            table: "CampaignFlows",
            column: "Trigger");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignFlows_Active_Enabled",
            table: "CampaignFlows",
            columns: new[] { "IsActive", "IsEnabled" });

        migrationBuilder.CreateIndex(
            name: "IX_CampaignFlows_StoreId",
            table: "CampaignFlows",
            column: "StoreId");

        // Create indexes for CampaignFlowSteps
        migrationBuilder.CreateIndex(
            name: "IX_CampaignFlowSteps_Flow_Order",
            table: "CampaignFlowSteps",
            columns: new[] { "FlowId", "StepOrder" });

        migrationBuilder.CreateIndex(
            name: "IX_CampaignFlowSteps_IsActive",
            table: "CampaignFlowSteps",
            column: "IsActive");

        // Create indexes for MemberFlowEnrollments
        migrationBuilder.CreateIndex(
            name: "IX_MemberFlowEnrollments_Member_Flow",
            table: "MemberFlowEnrollments",
            columns: new[] { "MemberId", "FlowId" });

        migrationBuilder.CreateIndex(
            name: "IX_MemberFlowEnrollments_Status",
            table: "MemberFlowEnrollments",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_MemberFlowEnrollments_NextStepScheduledAt",
            table: "MemberFlowEnrollments",
            column: "NextStepScheduledAt");

        migrationBuilder.CreateIndex(
            name: "IX_MemberFlowEnrollments_Status_Schedule_Active",
            table: "MemberFlowEnrollments",
            columns: new[] { "Status", "NextStepScheduledAt", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_MemberFlowEnrollments_FlowId",
            table: "MemberFlowEnrollments",
            column: "FlowId");

        migrationBuilder.CreateIndex(
            name: "IX_MemberFlowEnrollments_StoreId",
            table: "MemberFlowEnrollments",
            column: "StoreId");

        // Create indexes for FlowStepExecutions
        migrationBuilder.CreateIndex(
            name: "IX_FlowStepExecutions_Enrollment_Step",
            table: "FlowStepExecutions",
            columns: new[] { "EnrollmentId", "StepId" });

        migrationBuilder.CreateIndex(
            name: "IX_FlowStepExecutions_Status",
            table: "FlowStepExecutions",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_FlowStepExecutions_ScheduledAt",
            table: "FlowStepExecutions",
            column: "ScheduledAt");

        migrationBuilder.CreateIndex(
            name: "IX_FlowStepExecutions_Status_Scheduled_Active",
            table: "FlowStepExecutions",
            columns: new[] { "Status", "ScheduledAt", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_FlowStepExecutions_StepId",
            table: "FlowStepExecutions",
            column: "StepId");

        // Create indexes for CampaignEmailTemplates
        migrationBuilder.CreateIndex(
            name: "IX_CampaignEmailTemplates_StoreId",
            table: "CampaignEmailTemplates",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignEmailTemplates_Category",
            table: "CampaignEmailTemplates",
            column: "Category");

        // Create indexes for CampaignSmsTemplates
        migrationBuilder.CreateIndex(
            name: "IX_CampaignSmsTemplates_StoreId",
            table: "CampaignSmsTemplates",
            column: "StoreId");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignSmsTemplates_Category",
            table: "CampaignSmsTemplates",
            column: "Category");

        // Create unique index for CampaignFlowConfigurations
        migrationBuilder.CreateIndex(
            name: "IX_CampaignFlowConfigurations_StoreId",
            table: "CampaignFlowConfigurations",
            column: "StoreId",
            unique: true,
            filter: "[StoreId] IS NOT NULL");

        // Seed default welcome flow
        migrationBuilder.Sql(@"
            INSERT INTO CampaignFlows (Name, Description, Type, Trigger, TriggerDaysOffset, IsEnabled, MaxEnrollmentsPerMember, CooldownDays, IsActive, CreatedAt)
            VALUES
            ('Welcome Flow', 'Welcome new loyalty members with a special discount', 0, 0, 0, 1, 1, 0, 1, GETUTCDATE()),
            ('Birthday Flow', 'Send birthday wishes and special birthday discount', 1, 1, -7, 1, 1, 365, 1, GETUTCDATE()),
            ('Win-Back Flow', 'Re-engage inactive members', 5, 5, 0, 1, 1, 30, 1, GETUTCDATE());

            -- Insert steps for Welcome Flow
            INSERT INTO CampaignFlowSteps (FlowId, StepOrder, Name, Description, DelayDays, DelayHours, Channel, Subject, Content, DiscountPercent, DiscountValidDays, ConditionType, IsEnabled, IsActive, CreatedAt)
            VALUES
            (1, 1, 'Welcome Email', 'Send welcome email immediately', 0, 0, 0, 'Welcome to {{StoreName}}!', 'Hi {{MemberName}}, welcome to our loyalty program! Enjoy 10% off your next purchase.', 10, 30, 0, 1, 1, GETUTCDATE()),
            (1, 2, 'Follow-up Email', 'Follow up after 3 days', 3, 0, 0, 'How was your experience?', 'Hi {{MemberName}}, we hope you enjoyed your visit! Remember, you have {{PointsBalance}} points waiting.', NULL, NULL, 0, 1, 1, GETUTCDATE());

            -- Insert steps for Birthday Flow
            INSERT INTO CampaignFlowSteps (FlowId, StepOrder, Name, Description, DelayDays, DelayHours, Channel, Subject, Content, DiscountPercent, DiscountValidDays, ConditionType, IsEnabled, IsActive, CreatedAt)
            VALUES
            (2, 1, 'Birthday Week Email', 'Send 7 days before birthday', 0, 0, 0, 'Happy Birthday Week, {{MemberName}}!', 'Your birthday is coming up! Here is a special 15% discount just for you.', 15, 14, 0, 1, 1, GETUTCDATE()),
            (2, 2, 'Birthday Day SMS', 'Send on birthday', 7, 0, 1, NULL, 'Happy Birthday {{MemberName}}! 🎂 Enjoy your special day with us. Your 15% discount is still valid!', NULL, NULL, 0, 1, 1, GETUTCDATE());

            -- Insert steps for Win-Back Flow
            INSERT INTO CampaignFlowSteps (FlowId, StepOrder, Name, Description, DelayDays, DelayHours, Channel, Subject, Content, DiscountPercent, DiscountValidDays, ConditionType, IsEnabled, IsActive, CreatedAt)
            VALUES
            (3, 1, 'We Miss You Email', 'First win-back message', 0, 0, 0, 'We miss you, {{MemberName}}!', 'It has been a while since your last visit. Here is 20% off to welcome you back!', 20, 14, 0, 1, 1, GETUTCDATE()),
            (3, 2, 'Last Chance Email', 'Final reminder', 7, 0, 0, 'Your discount is expiring soon!', 'Hi {{MemberName}}, your 20% discount expires in 7 days. Do not miss out!', NULL, NULL, 0, 1, 1, GETUTCDATE());

            -- Insert default configuration
            INSERT INTO CampaignFlowConfigurations (StoreId, IsEnabled, EmailEnabled, SmsEnabled, MaxMessagesPerMemberPerDay, QuietHoursStart, QuietHoursEnd, WinBackInactivityDays, BirthdayFlowStartDays, PointsExpiryNotifyDays, MaxRetryAttempts, RetryDelayMinutes, IsActive, CreatedAt)
            VALUES (NULL, 1, 1, 1, 3, 21, 8, 30, 7, 30, 3, 15, 1, GETUTCDATE());
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FlowStepExecutions");
        migrationBuilder.DropTable(name: "MemberFlowEnrollments");
        migrationBuilder.DropTable(name: "CampaignFlowSteps");
        migrationBuilder.DropTable(name: "CampaignFlows");
        migrationBuilder.DropTable(name: "CampaignEmailTemplates");
        migrationBuilder.DropTable(name: "CampaignSmsTemplates");
        migrationBuilder.DropTable(name: "CampaignFlowConfigurations");
    }
}
