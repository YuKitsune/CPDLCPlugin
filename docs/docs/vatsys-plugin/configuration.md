---
sidebar_position: 3
---

# Configuration

The plugin is configured via `CPDLC.yaml` in the plugin directory. The keys are the same as the setting names on this page, for example `ServerEndpoint`. You can add comments to the file with `#`.

## Connection Settings

### ServerEndpoint

URL of the CPDLC server WebSocket endpoint.

**Example:**
```yaml
ServerEndpoint: "http://localhost:5261/hubs/controller"
```

### AutoConnect

When `true`, the plugin connects to the CPDLC server when vatSys connects to VATSIM. It uses the first station in `Stations`. The plugin must be authenticated (see [Connecting to the CPDLC Server](plugin.md#connecting-to-the-cpdlc-server)). Default: `false`.

**Example:**
```yaml
AutoConnect: true
```

### Stations

List of station identifiers this controller position will use. The plugin will connect to the server using these callsigns.

**Example:**
```yaml
Stations: [YZZZ, YXXX]
```

## Logging

### LogLevel

Controls the verbosity of plugin logs. Valid values: `Verbose`, `Debug`, `Information`, `Warning`, `Error`.

**Example:**
```yaml
LogLevel: Verbose
```

## Message Display

### MaxCurrentMessages

Maximum number of current (active) messages to display per aircraft.

**Example:**
```yaml
MaxCurrentMessages: 50
```

### MaxArchivedMessages

Maximum number of archived messages to display per aircraft.

**Example:**
```yaml
MaxArchivedMessages: 50
```

### MaxDisplayMessageLength

Maximum character length for messages shown in the label items. Messages exceeding this length will be truncated with "...".

**Example:**
```yaml
MaxDisplayMessageLength: 40
```

### MaxExtendedMessageLength

Maximum character length for messages shown in the extended message view. Messages exceeding this length will be truncated with "...".

**Example:**
```yaml
MaxExtendedMessageLength: 80
```

## Next Data Authority

### AtsuCodes

Maps ATSU/CPDLC logon codes to the vatSys sector names they cover. The plugin uses this to determine the Next Data Authority (NDA) for each tracked aircraft by walking the FDR route and finding the first sector that belongs to a different ATSU.

Each entry has an `AtsuCode` (the code aircraft logon to) and a `Sectors` array of sector names from the vatSys `Sectors.xml` file.

When calculating the NDA, the plugin walks the sector hierarchy from most-specific to least-specific (subsector to parent to grandparent). For each level it collects ATSU code candidates, then selects the first candidate that is currently connected to the ACARS network. This allows FIRs that use individual Hoppie logon codes per controller position to be mapped at the subsector level.

**Example (FIR using a single logon code):**
```yaml
AtsuCodes:
  - AtsuCode: YBBB
    Sectors: [ARL, INL, KPL, TSN]
  - AtsuCode: YMMM
    Sectors: [GUN, BLA, IND]
```

**Example (FIR using per-position logon codes):**
```yaml
AtsuCodes:
  - AtsuCode: WIIF
    Sectors: [WIIF]
  - AtsuCode: WIIFSG
    Sectors: [WIIFSG, WIIFBD]
  - AtsuCode: WIIFMN
    Sectors: [WIIFMN, WIIFIS]
```

In the per-position example, an aircraft entering `WIIFBD` airspace will have `WIIFSG` set as its NDA if that controller is online, falling back to `WIIF` if not.

If a sector appears in more than one entry, the NDA calculation will produce an error for that aircraft. If a sector does not appear in any entry, it is treated as outside CPDLC coverage and skipped.

### NdaRecalculationIntervalMinutes

How often (in minutes) the plugin recalculates the NDA for all tracked aircraft in the background. The NDA is also recalculated on every FDR update, so this is a catch-all interval.

Default: `1`

**Example:**
```yaml
NdaRecalculationIntervalMinutes: 1
```

### ShowInstallationMenuItems

Controls whether the `Install label & strip items` and `Uninstall label & strip items` entries appear under the `CPDLC` menu. Set to `false` when shipping the plugin with pre-modified profiles to prevent controllers from accidentally reverting the changes.

Default: `true`

**Example:**
```yaml
ShowInstallationMenuItems: false
```

### ADSCTrackExtrapolationThreshold

The minimum time (in seconds) that an ADS-C track shows the reported position with the `ADSC` symbol. After this time, the next flight plan extrapolation moves the track along its route, and the track uses the `ADSCExtrapolated` symbol.

Default: `10`

**Example:**
```yaml
ADSCTrackExtrapolationThreshold: 10
```

## Flagged Downlinks

### FlaggedElementId

The FANS 1/A downlink elements that are "flagged". The Current Messages window shows `!` next to a downlink that has one of these elements. If the downlink also has a free text element, the window shows `P` in place of `!`.

Default: none

**Example:**
```yaml
FlaggedElementId:
  - DM29  # CLIMBING TO [altitude]
  - DM30  # DESCENDING TO [altitude]
  - DM80  # DEVIATING [distance offset] [direction] OF ROUTE
```

## Uplink Messages

`UplinkMessages` sets the uplink elements that the controller can select in the editor. The plugin contains the full FANS 1/A uplink message set (UM0 to UM182), so the configuration only refers to the elements. The text, the parameters, the response type and the alert of each element come from the message set. You cannot change them.

### Element references

Each reference has:

- `Element`: The FANS 1/A uplink element, for example `UM20`.
- `DefaultParameters` (optional): Values for the parameters of the element. The key is the name of the parameter.

The name of a parameter is the name of its type, in camel case, for example `altitude`, `position` or `freeText`. If a type occurs more than one time in an element, the names have a number, for example `altitude1` and `altitude2`.

**Example:**
```yaml
- Element: UM169  # [free text]
  DefaultParameters:
    freeText: "REQUEST RECEIVED, RESPONSE WILL BE VIA VOICE"
```

#### Parameter Types

- `altitude`: Flight level or altitude
- `time`: Time in HHMM format
- `position`: Waypoint, fix, or latitude and longitude
- `speed`: Airspeed (knots or Mach)
- `distanceOffset`: Lateral offset distance
- `distance`: Distance
- `direction`: LEFT, RIGHT, or a compass direction
- `degrees`: Heading or track in degrees
- `legType`: Holding leg distance or time
- `routeClearance`: Route string
- `procedureName`: SID, STAR, or approach name
- `icaoUnitName`: ATC unit name
- `frequency`: Radio frequency
- `beaconCode`: Transponder code
- `altimeter`: Altimeter setting
- `atisCode`: ATIS identifier
- `errorInformation`: Error text
- `icaoFacilityDesignation`: ICAO facility code
- `tp4Table`: TP4 table
- `freeText`: Arbitrary text
- `verticalRate`: Vertical rate
- `toFrom`: TO or FROM
- `predepartureClearance`: Pre-departure clearance

### QuickAccessMessages

The element references that show in the quick access panel.

**Example:**
```yaml
QuickAccessMessages:
  - Element: UM147  # REQUEST POSITION REPORT
  - Element: UM20   # CLIMB TO AND MAINTAIN [altitude]
```

### Groups

The message groups in the editor. Each group has:

- `Name`: Group label shown in the UI
- `Messages`: The element references in the group

**Example:**
```yaml
Groups:
  - Name: LEVEL
    Messages:
      - Element: UM19  # MAINTAIN [altitude]
      - Element: UM20  # CLIMB TO AND MAINTAIN [altitude]
      - Element: UM23  # DESCEND TO AND MAINTAIN [altitude]
```

## Complete Example

```yaml
ServerEndpoint: "http://localhost:5261/hubs/controller"
AutoConnect: false
Stations: [YZZZ, YXXX]
LogLevel: Verbose
MaxCurrentMessages: 50
MaxArchivedMessages: 50
MaxDisplayMessageLength: 40
MaxExtendedMessageLength: 80
FlaggedElementId: [DM29, DM30, DM80]
AtsuCodes:
  - AtsuCode: YBBB
    Sectors: [ARL, INL, KPL, TSN]
  - AtsuCode: YMMM
    Sectors: [GUN, BLA, IND]
NdaRecalculationIntervalMinutes: 1
ADSCTrackExtrapolationThreshold: 10
UplinkMessages:
  QuickAccessMessages:
    - Element: UM20  # CLIMB TO AND MAINTAIN [altitude]
  Groups:
    - Name: LEVEL
      Messages:
        - Element: UM20  # CLIMB TO AND MAINTAIN [altitude]
```
