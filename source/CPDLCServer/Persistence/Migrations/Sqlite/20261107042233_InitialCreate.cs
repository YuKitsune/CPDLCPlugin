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
                    FlightSimulationNetwork = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedAuthenticationCode = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultAdscReportingInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectedAdscContractDuration = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcarsConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdscContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Callsign = table.Column<string>(type: "TEXT", nullable: false),
                    StationId = table.Column<string>(type: "TEXT", nullable: false),
                    ReportingInterval = table.Column<long>(type: "INTEGER", nullable: true),
                    DefaultContractState = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedReportingInterval = table.Column<long>(type: "INTEGER", nullable: true),
                    SelectedContractState = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedContractEnds = table.Column<long>(type: "INTEGER", nullable: true),
                    DemandContractState = table.Column<string>(type: "TEXT", nullable: true),
                    LastActivity = table.Column<long>(type: "INTEGER", nullable: false)
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
                    IsLatest = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    DidSentNextDataAuthorityMessage = table.Column<bool>(type: "INTEGER", nullable: false)
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
                    Opened = table.Column<long>(type: "INTEGER", nullable: false),
                    Closed = table.Column<long>(type: "INTEGER", nullable: true),
                    Archived = table.Column<long>(type: "INTEGER", nullable: true)
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
                    LastMessageId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageIdSequences", x => new { x.AcarsConnectionId, x.Callsign });
                });

            migrationBuilder.CreateTable(
                name: "ServerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConnectivityCheckIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AircraftConnectionTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AdscContractTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AdscReportRetentionHours = table.Column<int>(type: "INTEGER", nullable: false),
                    HandoffNotificationLeadTimeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    VatsimStatusEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    VatsimStatusUrl = table.Column<string>(type: "TEXT", nullable: false),
                    VatsimStatusPollingIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    VatsimStatusAllowNonEquippedAircraft = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
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
                    IsTransmissionFailed = table.Column<bool>(type: "INTEGER", nullable: true)
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
                columns: new[] { "Callsign", "StationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdscReports_Callsign",
                table: "AdscReports",
                column: "Callsign");

            migrationBuilder.CreateIndex(
                name: "IX_AdscReports_Received",
                table: "AdscReports",
                column: "Received");

            migrationBuilder.CreateIndex(
                name: "IX_AircraftConnections_Callsign_AcarsConnectionId",
                table: "AircraftConnections",
                columns: new[] { "Callsign", "AcarsConnectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CpdlcMessages_DialogueId",
                table: "CpdlcMessages",
                column: "DialogueId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialogues_AircraftCallsign",
                table: "Dialogues",
                column: "AircraftCallsign");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcarsConnections");

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
                name: "ServerSettings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Dialogues");
        }
    }
}
