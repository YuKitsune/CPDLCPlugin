---
sidebar_position: 3
---

# Configuration

The plugin is configured via `CPDLC.json` in the plugin directory.

## Connection Settings

### ServerEndpoint

URL of the CPDLC server WebSocket endpoint.

**Example:**
```json
"ServerEndpoint": "http://localhost:5261/hubs/controller"
```

### Stations

List of station identifiers this controller position will use. The plugin will connect to the server using these callsigns.

**Example:**
```json
"Stations": ["YZZZ", "YXXX"]
```

## Logging

### LogLevel

Controls the verbosity of plugin logs. Valid values: `Verbose`, `Debug`, `Information`, `Warning`, `Error`.

**Example:**
```json
"LogLevel": "Verbose"
```

## Message Display

### MaxCurrentMessages

Maximum number of current (active) messages to display per aircraft.

**Example:**
```json
"MaxCurrentMessages": 50
```

### MaxArchivedMessages

Maximum number of archived messages to display per aircraft.

**Example:**
```json
"MaxArchivedMessages": 50
```

### MaxDisplayMessageLength

Maximum character length for messages shown in the label items. Messages exceeding this length will be truncated with "...".

**Example:**
```json
"MaxDisplayMessageLength": 40
```

### MaxExtendedMessageLength

Maximum character length for messages shown in the extended message view. Messages exceeding this length will be truncated with "...".

**Example:**
```json
"MaxExtendedMessageLength": 80
```

## Next Data Authority

### AtsuCodes

Maps ATSU/CPDLC logon codes to the vatSys sector names they cover. The plugin uses this to determine the Next Data Authority (NDA) for each tracked aircraft by walking the FDR route and finding the first sector that belongs to a different ATSU.

Each entry has an `AtsuCode` (the code aircraft logon to) and a `Sectors` array of sector names from the vatSys `Sectors.xml` file. Subsector names do not need to be listed; the plugin resolves them to their parent sector automatically.

**Example:**
```json
"AtsuCodes": [
  { "AtsuCode": "YBBB", "Sectors": ["ARL", "INL", "TSN"] },
  { "AtsuCode": "YMMM", "Sectors": ["GUN", "BLA", "IND"] }
]
```

If a sector appears in more than one entry, the NDA calculation will produce an error for that aircraft. If a sector does not appear in any entry, it is treated as outside CPDLC coverage and skipped.

### NdaRecalculationIntervalMinutes

How often (in minutes) the plugin recalculates the NDA for all tracked aircraft in the background. The NDA is also recalculated on every FDR update, so this is a catch-all interval.

Default: `1`

**Example:**
```json
"NdaRecalculationIntervalMinutes": 1
```

## Uplink Messages

The `UplinkMessages` object defines the available CPDLC messages for the controller to send.

### MasterMessages

Array of all available message templates. Each message has:

- `Id`: Unique identifier matching ICAO Doc 10037
- `Template`: Message text with parameter placeholders in brackets (e.g., `[lev]`, `[pos]`)
- `Parameters`: Array of parameter definitions with `Name` and `Type`
- `ResponseType`: Expected pilot response (`WilcoUnable`, `Roger`, `AffirmativeNegative`, `NoResponse`)

**Example:**
```json
{
  "Id": 20,
  "Template": "CLIMB TO [lev]",
  "Parameters": [
    { "Name": "lev", "Type": "Level" }
  ],
  "ResponseType": "WilcoUnable"
}
```

#### Parameter Types

- `Level`: Flight level or altitude
- `Position`: Waypoint or fix
- `Time`: Time in HHMM format
- `Speed`: Airspeed (knots or Mach)
- `Degree`: Heading or track in degrees
- `Direction`: LEFT or RIGHT
- `DistanceOffset`: Distance in nautical miles
- `RouteClearance`: Route string
- `ProcedureName`: SID, STAR, or approach name
- `UnitName`: ATC unit name
- `Frequency`: Radio frequency
- `Code`: Transponder code
- `Altimeter`: Altimeter setting
- `AtisCode`: ATIS identifier
- `FacilityDesignation`: ICAO facility code
- `VerticalRate`: Vertical rate in feet per minute
- `ToFrom`: TO or FROM
- `FreeText`: Arbitrary text

### QuickAccessMessages

Array of messages shown in the quick access panel. Each entry references a `MessageId` from `MasterMessages`. Optionally includes `DefaultParameters` to pre-fill values and override `ResponseType`.

**Example:**
```json
{
  "MessageId": 169,
  "DefaultParameters": {
    "freetext": "REQUEST RECEIVED, RESPONSE WILL BE VIA VOICE"
  },
  "ResponseType": "Roger"
}
```

### Groups

Array of message groups for organizing messages in the editor. Each group has:

- `Name`: Group label shown in the UI
- `Messages`: Array of message references (with optional `DefaultParameters` and `ResponseType` overrides)

**Example:**
```json
{
  "Name": "LEVEL",
  "Messages": [
    { "MessageId": 19 },
    { "MessageId": 20 },
    { "MessageId": 23 }
  ]
}
```

## Complete Example

```json
{
  "ServerEndpoint": "http://localhost:5261/hubs/controller",
  "Stations": ["YZZZ", "YXXX"],
  "LogLevel": "Verbose",
  "MaxCurrentMessages": 50,
  "MaxArchivedMessages": 50,
  "MaxDisplayMessageLength": 40,
  "MaxExtendedMessageLength": 80,
  "AtsuCodes": [
    { "AtsuCode": "YBBB", "Sectors": ["OCN", "ARL", "SDY", "TSN"] },
    { "AtsuCode": "YMMM", "Sectors": ["ASP", "SNO", "IND", "INE"] }
  ],
  "NdaRecalculationIntervalMinutes": 1,
  "UplinkMessages": {
    "MasterMessages": [
      {
        "Id": 0,
        "Template": "UNABLE",
        "Parameters": [],
        "ResponseType": "NoResponse"
      },
      {
        "Id": 20,
        "Template": "CLIMB TO [lev]",
        "Parameters": [
          { "Name": "lev", "Type": "Level" }
        ],
        "ResponseType": "WilcoUnable"
      }
    ],
    "QuickAccessMessages": [
      { "MessageId": 20 }
    ],
    "Groups": [
      {
        "Name": "LEVEL",
        "Messages": [
          { "MessageId": 20 }
        ]
      }
    ]
  }
}
```
