using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CPDLCServer.Persistence.Migrations.PostgreSql
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    StationIdentifier = table.Column<string>(type: "text", nullable: false),
                    FlightSimulationNetwork = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    EncryptedAuthenticationCode = table.Column<string>(type: "text", nullable: false),
                    DefaultAdscReportingInterval = table.Column<int>(type: "integer", nullable: false),
                    SelectedAdscContractDuration = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcarsConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdscContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Callsign = table.Column<string>(type: "text", nullable: false),
                    StationId = table.Column<string>(type: "text", nullable: false),
                    ReportingInterval = table.Column<long>(type: "bigint", nullable: true),
                    DefaultContractState = table.Column<string>(type: "text", nullable: true),
                    SelectedReportingInterval = table.Column<long>(type: "bigint", nullable: true),
                    SelectedContractState = table.Column<string>(type: "text", nullable: true),
                    SelectedContractEnds = table.Column<long>(type: "bigint", nullable: true),
                    DemandContractState = table.Column<string>(type: "text", nullable: true),
                    LastActivity = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdscContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdscReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Callsign = table.Column<string>(type: "text", nullable: false),
                    StationId = table.Column<string>(type: "text", nullable: false),
                    Received = table.Column<long>(type: "bigint", nullable: false),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false),
                    Altitude = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdscReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AircraftConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Callsign = table.Column<string>(type: "text", nullable: false),
                    AcarsConnectionId = table.Column<string>(type: "text", nullable: false),
                    StationId = table.Column<string>(type: "text", nullable: false),
                    DataAuthorityState = table.Column<string>(type: "text", nullable: false),
                    ConnectionState = table.Column<string>(type: "text", nullable: false),
                    LogonRequested = table.Column<long>(type: "bigint", nullable: false),
                    LogonAccepted = table.Column<long>(type: "bigint", nullable: true),
                    LastSeen = table.Column<long>(type: "bigint", nullable: false),
                    NextDataAuthority = table.Column<string>(type: "text", nullable: true),
                    ExpectedTransferTime = table.Column<long>(type: "bigint", nullable: true),
                    DidSentNextDataAuthorityMessage = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AircraftConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dialogues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AircraftCallsign = table.Column<string>(type: "text", nullable: false),
                    Opened = table.Column<long>(type: "bigint", nullable: false),
                    Closed = table.Column<long>(type: "bigint", nullable: true),
                    Archived = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dialogues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageIdSequences",
                columns: table => new
                {
                    AcarsConnectionId = table.Column<string>(type: "text", nullable: false),
                    Callsign = table.Column<string>(type: "text", nullable: false),
                    LastMessageId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageIdSequences", x => new { x.AcarsConnectionId, x.Callsign });
                });

            migrationBuilder.CreateTable(
                name: "ServerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectivityCheckIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    AircraftConnectionTimeoutMinutes = table.Column<int>(type: "integer", nullable: false),
                    AdscContractTimeoutMinutes = table.Column<int>(type: "integer", nullable: false),
                    AdscReportRetentionHours = table.Column<int>(type: "integer", nullable: false),
                    HandoffNotificationLeadTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    VatsimStatusEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    VatsimStatusUrl = table.Column<string>(type: "text", nullable: false),
                    VatsimStatusPollingIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    VatsimStatusAllowNonEquippedAircraft = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CpdlcMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DialogueId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    MessageReference = table.Column<int>(type: "integer", nullable: true),
                    AlertType = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Closed = table.Column<long>(type: "bigint", nullable: true),
                    Acknowledged = table.Column<long>(type: "bigint", nullable: true),
                    Direction = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    Sender = table.Column<string>(type: "text", nullable: true),
                    DownlinkResponseType = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Received = table.Column<long>(type: "bigint", nullable: true),
                    IsControllerLate = table.Column<bool>(type: "boolean", nullable: true),
                    Recipient = table.Column<string>(type: "text", nullable: true),
                    SenderCallsign = table.Column<string>(type: "text", nullable: true),
                    UplinkResponseType = table.Column<string>(type: "text", nullable: true),
                    Sent = table.Column<long>(type: "bigint", nullable: true),
                    ClosedManually = table.Column<bool>(type: "boolean", nullable: true),
                    IsPilotLate = table.Column<bool>(type: "boolean", nullable: true),
                    IsTransmissionFailed = table.Column<bool>(type: "boolean", nullable: true)
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
