#nullable disable

using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CopyCat.Infrastructure.Data.Migrations;

/// <inheritdoc />
internal partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "text", nullable: false),
                EntityType = table.Column<string>(type: "text", nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                BeforeJson = table.Column<string>(type: "text", nullable: true),
                AfterJson = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLog", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ChannelSyncStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                LastSeenMessageId = table.Column<long>(type: "bigint", nullable: true),
                LastBackfilledMessageId = table.Column<long>(type: "bigint", nullable: true),
                LastSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                SyncStatus = table.Column<string>(type: "text", nullable: false),
                LastError = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChannelSyncStates", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DataProtectionKeys",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FriendlyName = table.Column<string>(type: "text", nullable: true),
                Xml = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FilterSets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FilterSets", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ForwardingJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                FilterVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                RewriteVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<string>(type: "text", nullable: false),
                ForwardingMode = table.Column<string>(type: "text", nullable: false),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "text", nullable: true),
                NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                TargetTelegramMessageId = table.Column<long>(type: "bigint", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ForwardingJobs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MessageDecisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                FilterVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                RewriteVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                Decision = table.Column<string>(type: "text", nullable: false),
                MatchedRuleId = table.Column<string>(type: "text", nullable: true),
                TraceJson = table.Column<string>(type: "text", nullable: true),
                RewritePreview = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MessageDecisions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Messages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramMessageId = table.Column<long>(type: "bigint", nullable: false),
                GroupedId = table.Column<string>(type: "text", nullable: true),
                MessageDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EditDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Text = table.Column<string>(type: "text", nullable: true),
                Caption = table.Column<string>(type: "text", nullable: true),
                NormalizedText = table.Column<string>(type: "text", nullable: true),
                RawJson = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Messages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RewriteSets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RewriteSets", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TelegramControlOperations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OperationType = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                MappingId = table.Column<Guid>(type: "uuid", nullable: true),
                PayloadJson = table.Column<string>(type: "text", nullable: true),
                ResultJson = table.Column<string>(type: "text", nullable: true),
                LastError = table.Column<string>(type: "text", nullable: true),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TelegramControlOperations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TelegramSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                PhoneNumberMasked = table.Column<string>(type: "text", nullable: true),
                PhoneNumberEncrypted = table.Column<string>(type: "text", nullable: true),
                ApiIdEncrypted = table.Column<string>(type: "text", nullable: true),
                ApiHashEncrypted = table.Column<string>(type: "text", nullable: true),
                SessionDataEncrypted = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                LastConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "text", nullable: true),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                PendingChallenge = table.Column<string>(type: "text", nullable: true),
                PendingPhoneNumber = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TelegramSessions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FilterVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FilterSetId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                FilterDefinition = table.Column<string>(type: "text", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FilterVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_FilterVersions_FilterSets_FilterSetId",
                    column: x => x.FilterSetId,
                    principalTable: "FilterSets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RewriteVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RewriteSetId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                Rules = table.Column<string>(type: "text", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RewriteVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_RewriteVersions_RewriteSets_RewriteSetId",
                    column: x => x.RewriteSetId,
                    principalTable: "RewriteSets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TelegramChannels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramChannelId = table.Column<long>(type: "bigint", nullable: false),
                AccessHash = table.Column<string>(type: "text", nullable: true),
                Title = table.Column<string>(type: "text", nullable: false),
                Username = table.Column<string>(type: "text", nullable: true),
                ChannelType = table.Column<string>(type: "text", nullable: false),
                IsSource = table.Column<bool>(type: "boolean", nullable: false),
                IsTarget = table.Column<bool>(type: "boolean", nullable: false),
                CanPost = table.Column<bool>(type: "boolean", nullable: true),
                CanAdmin = table.Column<bool>(type: "boolean", nullable: true),
                CanCreateRelatedTargets = table.Column<bool>(type: "boolean", nullable: true),
                DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RawJson = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TelegramChannels", x => x.Id);
                table.ForeignKey(
                    name: "FK_TelegramChannels_TelegramSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "TelegramSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ChannelMappings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SourceChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                TargetChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                DefaultPolicy = table.Column<string>(type: "text", nullable: false),
                ForwardingMode = table.Column<string>(type: "text", nullable: false),
                ActiveFilterSetId = table.Column<Guid>(type: "uuid", nullable: true),
                ActiveRewriteSetId = table.Column<Guid>(type: "uuid", nullable: true),
                LiveForwardingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                BackfillCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChannelMappings", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChannelMappings_FilterSets_ActiveFilterSetId",
                    column: x => x.ActiveFilterSetId,
                    principalTable: "FilterSets",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ChannelMappings_RewriteSets_ActiveRewriteSetId",
                    column: x => x.ActiveRewriteSetId,
                    principalTable: "RewriteSets",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ChannelMappings_TelegramChannels_SourceChannelId",
                    column: x => x.SourceChannelId,
                    principalTable: "TelegramChannels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChannelMappings_TelegramChannels_TargetChannelId",
                    column: x => x.TargetChannelId,
                    principalTable: "TelegramChannels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MessageAttachments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                AttachmentType = table.Column<string>(type: "text", nullable: false),
                TelegramFileId = table.Column<string>(type: "text", nullable: true),
                FileUniqueId = table.Column<string>(type: "text", nullable: true),
                MimeType = table.Column<string>(type: "text", nullable: true),
                FileName = table.Column<string>(type: "text", nullable: true),
                SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                MetadataJson = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MessageAttachments", x => x.Id);
                table.ForeignKey(
                    name: "FK_MessageAttachments_Messages_MessageId",
                    column: x => x.MessageId,
                    principalTable: "Messages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MessageLinks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                Url = table.Column<string>(type: "text", nullable: false),
                LinkType = table.Column<string>(type: "text", nullable: false),
                DisplayText = table.Column<string>(type: "text", nullable: true),
                MetadataJson = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MessageLinks", x => x.Id);
                table.ForeignKey(
                    name: "FK_MessageLinks_Messages_MessageId",
                    column: x => x.MessageId,
                    principalTable: "Messages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ChannelMappings_ActiveFilterSetId",
            table: "ChannelMappings",
            column: "ActiveFilterSetId");

        migrationBuilder.CreateIndex(
            name: "IX_ChannelMappings_ActiveRewriteSetId",
            table: "ChannelMappings",
            column: "ActiveRewriteSetId");

        migrationBuilder.CreateIndex(
            name: "IX_ChannelMappings_SourceChannelId",
            table: "ChannelMappings",
            column: "SourceChannelId");

        migrationBuilder.CreateIndex(
            name: "IX_ChannelMappings_SourceChannelId_TargetChannelId",
            table: "ChannelMappings",
            columns: new[] { "SourceChannelId", "TargetChannelId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChannelMappings_TargetChannelId",
            table: "ChannelMappings",
            column: "TargetChannelId");

        migrationBuilder.CreateIndex(
            name: "IX_ChannelSyncStates_SessionId_ChannelId",
            table: "ChannelSyncStates",
            columns: new[] { "SessionId", "ChannelId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FilterVersions_FilterSetId_VersionNumber",
            table: "FilterVersions",
            columns: new[] { "FilterSetId", "VersionNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ForwardingJobs_Status_NextRetryAt",
            table: "ForwardingJobs",
            columns: new[] { "Status", "NextRetryAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ForwardingJobs_MessageId_MappingId",
            table: "ForwardingJobs",
            columns: new[] { "MessageId", "MappingId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MessageAttachments_MessageId",
            table: "MessageAttachments",
            column: "MessageId");

        migrationBuilder.CreateIndex(
            name: "IX_MessageDecisions_MessageId_MappingId",
            table: "MessageDecisions",
            columns: new[] { "MessageId", "MappingId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MessageLinks_MessageId",
            table: "MessageLinks",
            column: "MessageId");

        migrationBuilder.CreateIndex(
            name: "IX_Messages_SourceChannelId_TelegramMessageId",
            table: "Messages",
            columns: new[] { "SourceChannelId", "TelegramMessageId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RewriteVersions_RewriteSetId_VersionNumber",
            table: "RewriteVersions",
            columns: new[] { "RewriteSetId", "VersionNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TelegramChannels_SessionId_TelegramChannelId",
            table: "TelegramChannels",
            columns: new[] { "SessionId", "TelegramChannelId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TelegramControlOperations_Status_CreatedAt",
            table: "TelegramControlOperations",
            columns: new[] { "Status", "CreatedAt" });

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION notify_forwarding_jobs_changed()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                PERFORM pg_notify('copycat_forwarding_jobs_changed', COALESCE(NEW."Id", OLD."Id")::text);
                RETURN COALESCE(NEW, OLD);
            END;
            $$;
            """);

        migrationBuilder.Sql(
            """
            CREATE TRIGGER trg_forwarding_jobs_changed
            AFTER INSERT OR UPDATE OR DELETE ON "ForwardingJobs"
            FOR EACH ROW
            EXECUTE FUNCTION notify_forwarding_jobs_changed();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_forwarding_jobs_changed ON \"ForwardingJobs\";");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS notify_forwarding_jobs_changed();");
        migrationBuilder.DropTable(name: "AuditLog");
        migrationBuilder.DropTable(name: "ChannelMappings");
        migrationBuilder.DropTable(name: "ChannelSyncStates");
        migrationBuilder.DropTable(name: "DataProtectionKeys");
        migrationBuilder.DropTable(name: "FilterVersions");
        migrationBuilder.DropTable(name: "ForwardingJobs");
        migrationBuilder.DropTable(name: "MessageAttachments");
        migrationBuilder.DropTable(name: "MessageDecisions");
        migrationBuilder.DropTable(name: "MessageLinks");
        migrationBuilder.DropTable(name: "RewriteVersions");
        migrationBuilder.DropTable(name: "TelegramControlOperations");
        migrationBuilder.DropTable(name: "TelegramChannels");
        migrationBuilder.DropTable(name: "Messages");
        migrationBuilder.DropTable(name: "FilterSets");
        migrationBuilder.DropTable(name: "RewriteSets");
        migrationBuilder.DropTable(name: "TelegramSessions");
    }
}
