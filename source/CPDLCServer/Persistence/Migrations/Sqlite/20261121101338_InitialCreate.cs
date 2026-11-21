using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPDLCServer.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcarsConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    StationIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedAuthenticationCode = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultAdscReportingInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    Motd = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MotdDelaySeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcarsConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Administrators",
                columns: table => new
                {
                    Cid = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Added = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administrators", x => x.Cid);
                });

            migrationBuilder.CreateTable(
                name: "AdscContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Callsign = table.Column<string>(type: "TEXT", nullable: false),
                    StationId = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultReportingInterval = table.Column<long>(type: "INTEGER", nullable: false),
                    ReportingInterval = table.Column<long>(type: "INTEGER", nullable: true),
                    DefaultContractState = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedReportingInterval = table.Column<long>(type: "INTEGER", nullable: true),
                    SelectedContractState = table.Column<string>(type: "TEXT", nullable: true),
                    DemandContractState = table.Column<string>(type: "TEXT", nullable: true),
                    LastActivity = table.Column<long>(type: "INTEGER", nullable: false),
                    LastReportReceived = table.Column<long>(type: "INTEGER", nullable: true),
                    ReportingSource = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdscContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdscReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Callsign = table.Column<string>(type: "TEXT", nullable: false),
                    StationId = table.Column<string>(type: "TEXT", nullable: false),
                    Received = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Altitude = table.Column<int>(type: "INTEGER", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdscReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AircraftConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Callsign = table.Column<string>(type: "TEXT", nullable: false),
                    AcarsConnectionId = table.Column<string>(type: "TEXT", nullable: false),
                    StationId = table.Column<string>(type: "TEXT", nullable: false),
                    DataAuthorityState = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionState = table.Column<string>(type: "TEXT", nullable: false),
                    LogonRequested = table.Column<long>(type: "INTEGER", nullable: false),
                    LogonAccepted = table.Column<long>(type: "INTEGER", nullable: true),
                    LastSeen = table.Column<long>(type: "INTEGER", nullable: false),
                    NextDataAuthority = table.Column<string>(type: "TEXT", nullable: true),
                    ExpectedTransferTime = table.Column<long>(type: "INTEGER", nullable: true),
                    DidSentNextDataAuthorityMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AircraftConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dialogues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AircraftCallsign = table.Column<string>(type: "TEXT", nullable: false),
                    StationId = table.Column<string>(type: "TEXT", nullable: false),
                    Opened = table.Column<long>(type: "INTEGER", nullable: false),
                    Closed = table.Column<long>(type: "INTEGER", nullable: true),
                    Archived = table.Column<long>(type: "INTEGER", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dialogues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageIdSequences",
                columns: table => new
                {
                    AcarsConnectionId = table.Column<string>(type: "TEXT", nullable: false),
                    Callsign = table.Column<string>(type: "TEXT", nullable: false),
                    LastMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageIdSequences", x => new { x.AcarsConnectionId, x.Callsign });
                });

            migrationBuilder.CreateTable(
                name: "PluginTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cid = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    Expires = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PrivacyContact = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ServerName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Motd = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    BannerImage = table.Column<byte[]>(type: "BLOB", nullable: true),
                    BannerContentType = table.Column<string>(type: "TEXT", nullable: true),
                    BannerVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectivityCheckIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AircraftConnectionTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AdscContractTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DialogueRetentionHours = table.Column<int>(type: "INTEGER", nullable: false),
                    HandoffNotificationLeadTimeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    VatsimStatusEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    VatsimStatusUrl = table.Column<string>(type: "TEXT", nullable: false),
                    VatsimStatusPollingIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    VatsimStatusAllowNonEquippedAircraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    VatsimStatusReportDelayMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CpdlcMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DialogueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageReference = table.Column<int>(type: "INTEGER", nullable: true),
                    AlertType = table.Column<string>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Closed = table.Column<long>(type: "INTEGER", nullable: true),
                    Acknowledged = table.Column<long>(type: "INTEGER", nullable: true),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    Sender = table.Column<string>(type: "TEXT", nullable: true),
                    DownlinkResponseType = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    Received = table.Column<long>(type: "INTEGER", nullable: true),
                    IsControllerLate = table.Column<bool>(type: "INTEGER", nullable: true),
                    Recipient = table.Column<string>(type: "TEXT", nullable: true),
                    SenderCallsign = table.Column<string>(type: "TEXT", nullable: true),
                    UplinkResponseType = table.Column<string>(type: "TEXT", nullable: true),
                    Sent = table.Column<long>(type: "INTEGER", nullable: true),
                    ClosedManually = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsPilotLate = table.Column<bool>(type: "INTEGER", nullable: true),
                    DidTransmissionFail = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CpdlcMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CpdlcMessages_Dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "Dialogues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcarsConnections_StationIdentifier",
                table: "AcarsConnections",
                column: "StationIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdscContracts_Callsign_StationId",
                table: "AdscContracts",
                columns: new[] { "Callsign", "StationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdscReports_Callsign_StationId",
                table: "AdscReports",
                columns: new[] { "Callsign", "StationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AircraftConnections_Callsign_AcarsConnectionId",
                table: "AircraftConnections",
                columns: new[] { "Callsign", "AcarsConnectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CpdlcMessages_DialogueId",
                table: "CpdlcMessages",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_AircraftCallsign_StationId",
                table: "Dialogues",
                columns: new[] { "AircraftCallsign", "StationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_Opened",
                table: "Dialogues",
                column: "Opened");

            migrationBuilder.CreateIndex(
                name: "IX_PluginTokens_Cid",
                table: "PluginTokens",
                column: "Cid");

            migrationBuilder.CreateIndex(
                name: "IX_PluginTokens_TokenHash",
                table: "PluginTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcarsConnections");

            migrationBuilder.DropTable(
                name: "Administrators");

            migrationBuilder.DropTable(
                name: "AdscContracts");

            migrationBuilder.DropTable(
                name: "AdscReports");

            migrationBuilder.DropTable(
                name: "AircraftConnections");

            migrationBuilder.DropTable(
                name: "CpdlcMessages");

            migrationBuilder.DropTable(
                name: "MessageIdSequences");

            migrationBuilder.DropTable(
                name: "PluginTokens");

            migrationBuilder.DropTable(
                name: "ServerSettings");

            migrationBuilder.DropTable(
                name: "Dialogues");
        }
    }
}
