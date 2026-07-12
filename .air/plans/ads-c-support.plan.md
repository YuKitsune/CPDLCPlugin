# ADS-C Support for CPDLC Plugin and Server

## Context

The CPDLC server relays CPDLC messages between vatSys controllers and aircraft on Hoppie's ACARS network, but does not handle Hoppie's `ads-c` message type (marked `// TODO: ADS-C` in `HoppieAcarsClient.cs:8` and `Plugin.cs:25`). This plan adds ADS-C support: parse ADS-C position reports from Hoppie, let controllers manually establish/cancel periodic contracts and request one-off demand reports per aircraft, and store report data in the plugin for later ASD display.

Confirmed scope decisions:
- Contract management: manual, per aircraft, via a new dedicated ADS-C window in the plugin.
- Contract types: periodic (preset intervals 5/10/15/20/30 min) and demand only. No event contracts.
- ASD display: out of scope. Plugin stores contract state + report history so display can be built later.

## Goal

Controllers can establish/cancel ADS-C periodic contracts and request demand reports per aircraft from a new plugin window; the server sends the corresponding Hoppie uplinks, parses incoming reports, tracks contract state (including overdue detection), and pushes state/reports to the plugin, which stores them.

## Approach

Follow existing architecture end-to-end: Hoppie client parses `ads-c` packets into a new received-message type on the existing channel (made polymorphic); MediatR commands/notifications manage contract state in a new in-memory repository; SignalR hub methods + push events expose it; plugin mirrors state in a new store and presents a new WPF window via the existing WindowManager/MediatR pattern. No new frameworks.

## Phase 0: Protocol verification (blocks all packet-format code)

Exact Hoppie ADS-C packet syntax must be verified, not assumed. Believed shape: uplink `REQUEST PERIODIC` (interval handling TBD) / `REQUEST CANCEL`, one-off demand request; downlink `REPORT <callsign> <time> <lat> <lon> <alt>`.

1. Read https://www.hoppie.nl/acars/system/ads-c.html and https://www.hoppie.nl/acars/system/tech.html. Record exact uplink packets for periodic request, demand request, cancel; exact report format (field order, units, sign conventions, optional fields like groundspeed/heading); whether ATC chooses the interval at all. If Hoppie carries no interval, the preset interval becomes a client-side expectation used only for overdue detection (plan works either way).
2. Capture real packets from a Hoppie pilot client; save verbatim as test fixtures in `source/CPDLCServer.Tests/`.
3. Document verified formats in a comment block in `HoppieAcarsClient.cs`.

## Phase 1: Server receive path + model

### Model (`source/CPDLCServer/Model/`)
- **Create** `AdscReport.cs`: record `(string Callsign, double Latitude, double Longitude, int AltitudeFeet, DateTimeOffset Received, int? GroundspeedKnots, int? HeadingDegrees)` (optional fields per Phase 0 findings).
- **Create** `AdscAircraftState.cs`: mutable aggregate per aircraft, style of `AircraftConnection.cs` (identity in ctor, private-set state, mutation methods taking `DateTimeOffset now`). Keyed by existing `AircraftKey(callsign, acarsClientId)`.
  - `AdscContract? PeriodicContract`: `TimeSpan Interval`, `AdscContractState` enum (`Requesting`, `Established`, `Cancelled`), `RequestedAt`, `EstablishedAt?`, `LastReportAt?`, `bool IsOverdue`.
  - `DateTimeOffset? DemandRequestedAt`: pending one-off demand (a timestamp, not a second contract; any incoming report satisfies it and refreshes the periodic contract).
  - `List<AdscReport> Reports`: bounded history, cap 50, trim oldest.
  - Methods: `RequestPeriodic(interval, now)`, `RequestDemand(now)`, `Cancel(now)`, `ReceiveReport(report, now)` (Requesting -> Established, clears demand + overdue, appends), `MarkOverdue()`, `ClearDemand()`.
  - State rules: `Established` with no report within `max(Interval * 1.5, 2 min)` -> `IsOverdue = true` (state unchanged; late report clears). `Requesting` with no report within `max(2 min, Interval)` -> overdue flag. Cancel marks `Cancelled` when the cancel uplink sends (Hoppie has no cancel ack). Unsolicited reports create/refresh a state with no contract.
- **Create** `ReceivedAdscReport.cs`: record `(string Sender, AdscReport Report)`, analogous to `ReceivedDownlink`.
- **Create** `AdscUplink.cs`: record `(string Recipient, AdscRequestType RequestType /* Periodic, Demand, Cancel */, TimeSpan? Interval)`.

### Persistence (`source/CPDLCServer/Persistence/`)
- **Create** `IAdscRepository.cs` + `InMemoryAdscRepository.cs`: copy `InMemoryAircraftRepository` pattern (SemaphoreSlim + dictionary). `Add/Find/All/Remove` plus `FindByCallsign`. Register singleton in `Program.cs`.

### Hoppie client (`source/CPDLCServer/Clients/`)
- **Modify** channel typing to polymorphic (single channel preserves ordering): new marker interface `Model/IReceivedAcarsMessage.cs`; `ReceivedDownlink` and `ReceivedAdscReport` implement it. Change `IAcarsClient.MessageReader` and `HoppieAcarsClient` channel (`HoppieAcarsClient.cs:23,32`) to `IReceivedAcarsMessage`. Update `CPDLCServer.Tests/Mocks/TestAcarsClient.cs`.
- **Modify** `HoppieAcarsClient.cs` type switch (lines 270-274): add `"ads-c" => ParseAdscDownlink(from, packet)`. Parser implements Phase-0 format, token-based, log-and-drop (return null) on unrecognised packets so one malformed client cannot poison the poll loop. Remove TODO at line 8.
- **Modify** `HoppieAcarsClient.Send` (lines 57-100): extract shared private `PostMessage(to, type, packet, ct)` helper; add `SendAdsc(AdscUplink, ct)` to `IAcarsClient` posting `type=ads-c` with verified packet text and updating `_lastSendTime` (triggers 20s fast poll for prompt first report).
- **Modify** `ClientManager.Subscribe` (`ClientManager.cs:172-206`): pattern-match dequeued message; `ReceivedDownlink` -> existing `DownlinkReceivedNotification`, `ReceivedAdscReport` -> new `AdscReportReceivedNotification(acarsClientId, stationId, report)`.
- **Modify** `IClientManager` (`ClientManager.cs:8-11`): add `FindAcarsClientByStationId(string stationId, ct)` (configs already carry `StationIdentifier`; needed because contracts may target aircraft with no CPDLC logon). Prefer the aircraft's `AircraftConnection.AcarsClientId` when one exists. Update `TestClientManager` mock.

## Phase 2: Server commands, hub, DTOs, monitor

### MediatR (`source/CPDLCServer/Messages/` + `Handlers/`)
- **Create** `RequestAdscContractCommand` `(StationId, Callsign, AdscContractType Periodic|Demand, TimeSpan? Interval)`; handler resolves ACARS client (aircraft connection first, else by stationId), upserts state (`RequestPeriodic`/`RequestDemand` with `IClock.UtcNow()`), sends uplink, publishes `AdscStateChangedNotification`. Re-requesting periodic with new interval = new uplink + reset to `Requesting` (verify supersede semantics in Phase 0).
- **Create** `CancelAdscContractCommand` `(StationId, Callsign)`; handler sends cancel uplink, `state.Cancel(now)`, clears pending demand, publishes. Keeps the state row (latest report still viewable); monitor purges later.
- **Create** `AdscReportReceivedNotification` + handler: find-or-create state, `ReceiveReport`, publish `AdscStateChangedNotification` with the new report; also `LogLastSeen` on matching `AircraftConnection`.
- **Create** `AdscStateChangedNotification` `(AdscAircraftState State, AdscReport? NewReport)` + handler (pattern `DialogueChangedNotificationHandler`): convert via new `Model/AdscConverter.cs` (pattern `DialogueConverter.cs`); push `"AdscAircraftUpdated"` (always) and `"AdscReportReceived"` (when NewReport non-null) to controllers via `IHubContext<ControllerHub>`.
- Aircraft disconnect handlers: leave ADS-C state intact (CPDLC logoff does not stop Hoppie clients reporting); overdue detection + purge is the safety net.

### DTOs (`source/CPDLCServer.Contracts/`, new `AdscMessages.cs`)
- `AdscContractStateDto` enum; `AdscReportDto` record; `AdscAircraftDto` record `(Callsign, StationId, PeriodicContractState?, PeriodicIntervalMinutes?, IsPeriodicOverdue, LastReportTime?, DemandRequestedAt?, LatestReport?)`. Flat, no polymorphism needed. Must stay net472-compatible.

### Hub (`source/CPDLCServer/Hubs/ControllerHub.cs`)
- **Modify**: add methods per `UpdateNextDataAuthority` pattern (resolve controller by ConnectionId, dispatch command):
  - `RequestPeriodicContract(callsign, intervalMinutes)` (validate interval in {5,10,15,20,30})
  - `RequestDemandReport(callsign)`
  - `CancelAdscContract(callsign)`
  - `GetAdscAircraft()` -> `AdscAircraftDto[]` (initial load)
  - `GetAdscReports(callsign)` -> `AdscReportDto[]` (history; needed for future ASD work)

### Background service (`source/CPDLCServer/Services/`)
- **Create** `AdscMonitorService.cs`: copy `MessageMonitorService` shape (BackgroundService, 5s tick, `IClock`, per-iteration try/catch, register in `Program.cs`). Flags/unflags overdue (notify only on transitions), expires pending demands after 5 min, purges `Cancelled` and inactive contract-less states after 30 min.

## Phase 3: Plugin transport + store

- **Modify** `source/CPDLCPlugin/Server/IDownlinkHandlerDelegate.cs`: add `AdscAircraftUpdated(AdscAircraftDto)`, `AdscReportReceived(AdscReportDto)`.
- **Modify** `source/CPDLCPlugin/Server/SignalRConnectionManager.cs`: register `_connection.On<...>` for both events (lines 112-125 pattern); add invocation wrappers `RequestPeriodicContract`, `RequestDemandReport`, `CancelAdscContract`, `GetAdscAircraft`, `GetAdscReports` (pattern of `UpdateNextDataAuthority`).
- **Modify** `source/CPDLCPlugin/Server/MediatorMessageHandler.cs`: two new methods publishing plugin-side notifications (same try/catch shape).
- **Create** `source/CPDLCPlugin/AdscStore.cs`: SemaphoreSlim-guarded (pattern `DialogueStore`); dictionary of `AdscAircraftDto` by callsign + bounded report history per callsign (cap 50) for future ASD display. `Populate/Upsert/AddReport/All/GetReports/Clear`. Register singleton in `Plugin.cs` DI (lines 85-106).
- **Create** in `source/CPDLCPlugin/Messages/`: `AdscAircraftUpdatedNotification` (handler: store upsert + `WeakReferenceMessenger` `AdscDataChanged` broadcast), `AdscReportReceivedNotification` (handler: store add + broadcast) - pattern `AircraftConnectionUpdatedNotification`.
- **Modify** `ConnectedNotification` handler: populate `AdscStore` from `GetAdscAircraft()` on connect. **Modify** `DisconnectedNotification` bridge: clear store.
- **Create** contract-management requests (request+handler colocated, pattern `UpdateNextDataAuthorityForFdrRequest`): `RequestAdscPeriodicContractRequest(Callsign, IntervalMinutes)`, `RequestAdscDemandReportRequest(Callsign)`, `CancelAdscContractRequest(Callsign)` - each guards on connection and calls the connection-manager wrapper.

## Phase 4: Plugin ADS-C window

- **Modify** `source/CPDLCPlugin/Windows/WindowKeys.cs`: add `Adsc`.
- **Create** `source/CPDLCPlugin/Messages/OpenAdscWindowRequest.cs` (pattern `OpenHistoryWindowRequest`): optional initial callsign; `WindowManager.FocusOrCreateWindow(WindowKeys.Adsc, ...)`.
- **Modify** `Plugin.cs` toolbar registration (lines 287-336): add "ADS-C" `CustomToolStripMenuItem` under the CPDLC category; click sends `OpenAdscWindowRequest(MMI.SelectedTrack?.GetFDR()?.Callsign)` (same prefill trick as History item).
- **Create** `source/CPDLCPlugin/ViewModels/AdscViewModel.cs` + `AdscAircraftViewModel.cs` (pattern `HistoryViewModel`: ObservableObject, `IRecipient<AdscDataChanged>`, `IGuiInvoker`, `IErrorReporter`, `IDisposable`):
  - `ObservableCollection<AdscAircraftViewModel>` rebuilt from `AdscStore.All()` on load and on each change.
  - New-contract entry: callsign TextInput (prefilled from selected track) + interval presets `[5, 10, 15, 20, 30]` + `EstablishPeriodic` RelayCommand.
  - Per-row RelayCommands: `RequestDemand`, `Cancel`, `ChangeInterval` (re-establish with different preset).
  - `AdscAircraftViewModel`: display formatting - contract state text ("REQ", "EST 10MIN", "OVERDUE", "CXL", "DEMAND PENDING"), last-report age, lat/lon, altitude. Overdue rows highlighted with existing theme brushes.
- **Create** `source/CPDLCPlugin/Windows/AdscWindow.xaml` + `.xaml.cs` (chrome/styling per `HistoryWindow.xaml`: Theme resources, BeveledBorder, CustomScrollBar): top bar (callsign input, interval buttons, REQ PERIODIC / REQ DEMAND), main list (row per aircraft: callsign, state, interval, last report time + age, position/altitude), per-row DEMAND/CANCEL buttons. `Closed += (_, _) => viewModel.Dispose();` and close-on-disconnect per existing window pattern.

## Ordered implementation steps (each a working increment)

1. **Verify protocol** (Phase 0): documented formats + captured fixtures. Blocks everything else.
2. **Server receive path**: channel refactor (`IReceivedAcarsMessage`) as an isolated commit guarded by the existing test suite; then `ParseAdscDownlink`, model, repository, `AdscReportReceivedNotification` + handler. Increment: server stores unsolicited ADS-C reports; CPDLC tests green.
3. **Server contract commands + uplinks**: `AdscUplink`, `SendAdsc`, `FindAcarsClientByStationId`, request/cancel commands + handlers, state machine.
4. **Server push + hub + monitor**: DTOs, `AdscConverter`, broadcast handler, hub methods, `AdscMonitorService`. Server feature complete.
5. **Plugin transport + store**: SignalR handlers, wrappers, `AdscStore`, notifications, connect/disconnect population. Plugin silently tracks data.
6. **Plugin window**: request/handler, ViewModels, XAML, menu item. User-visible.
7. **Test hardening + manual E2E.**

## Acceptance criteria

- Server parses a valid Hoppie `ads-c` report packet into an `AdscReport` and stores it (unit test with Phase-0 fixture).
- Malformed `ads-c` packets are logged and dropped; poll loop continues (unit test).
- `RequestPeriodicContract(callsign, 10)` from the plugin results in a `type=ads-c` uplink to that callsign and contract state `Requesting`; first report transitions to `Established` and the plugin receives `AdscAircraftUpdated` + `AdscReportReceived`.
- `RequestDemandReport` results in one uplink; the next report clears the pending demand; an unanswered demand expires after 5 minutes with a state push.
- `CancelAdscContract` sends the cancel uplink and state shows `Cancelled`; row purged after 30 min.
- Established contract with no report for `max(interval * 1.5, 2 min)` is flagged overdue exactly once (single notification per transition).
- Interval values outside {5,10,15,20,30} rejected by the hub.
- Plugin ADS-C window opens from the vatSys CPDLC menu, lists aircraft with contract state/interval/last report age/position/altitude, and its controls dispatch the three hub methods.
- `AdscStore` retains at most 50 reports per callsign and clears on disconnect.
- All existing CPDLC tests remain green after the channel refactor.

## Verification

- `dotnet test` on `CPDLCServer.Tests` and `CPDLCPlugin.Tests` (per user's workflow rules, run only when instructed).
- Manual E2E: run server locally with Hoppie `.env` config, plugin in vatSys Sweatbox, Hoppie pilot client: establish periodic -> reports appear in window; demand -> single report; cancel -> CXL, reports stop; kill pilot client -> OVERDUE flag.
- Edge cases: unsolicited report with no contract (row appears, "no contract"), re-request with new interval, aircraft with no CPDLC logon, server restart mid-contract (state recreated by next report).

## Risks & mitigations

1. **Unknown exact Hoppie ads-c packet format** (highest): mandatory Phase 0 with captured fixtures before parser/serializer; tolerant token-based parser that drops unknown packets. Note: Hoppie protocol doc pages tripped automated fetch filters during planning; developer should read them directly.
2. **Pilot client variance** (fixed cadences, no demand support, format drift): interval treated as expectation (1.5x grace), demand expiry timeout, parser tolerant of extra/missing fields, unparseable packets logged verbatim for fixture harvesting.
3. **Contract state vs CPDLC disconnect**: contracts survive logoff (Hoppie client keeps reporting); overdue + 30-min purge is the safety net. Server restart loses in-memory state; unsolicited reports recreate it.
4. **Channel refactor blast radius**: `IAcarsClient.MessageReader` type change touches `ClientManager` + mocks; isolated first commit, full existing test suite as guard.

## Unresolved questions

- Whether Hoppie's `REQUEST PERIODIC` carries an interval (Phase 0 determines; UI presets remain either way, as expectation or as wire parameter).
- Whether pilot clients include groundspeed/heading in reports (optional DTO fields cover both).