---
sidebar_position: 1
slug: /concepts
displayed_sidebar: null
---

# CPDLC Overview

Controller-Pilot Data Link Communications (CPDLC) is a method of communication between air traffic controllers and pilots using data link rather than voice. This document provides an overview of CPDLC concepts relevant to controllers using the CPDLC Plugin on the VATSIM network.

## ATS Units and Aircraft

An Air Traffic Service Unit (ATSU) is a ground station connected to an ACARS network.
Aircraft connect to ATSUs via an intermediate network (such as ACARS) to exchange CPDLC messages with controllers.

## Connection Establishment

Before CPDLC messages can be exchanged, a connection must be established between the aircraft and ground station.

### Logon Process

Aircraft establish CPDLC connections by sending a logon request to an ATSU:

```mermaid
sequenceDiagram
    participant Pilot
    participant ATSU

    Pilot->>ATSU: REQUEST LOGON
    ATSU->>Pilot: LOGON ACCEPTED
    Note over ATSU,Pilot: Connection established (NDA state)
```

The ground station responds with `LOGON ACCEPTED` to confirm the connection. The aircraft initially connects as Next Data Authority (NDA) and is promoted to Current Data Authority (CDA) upon sending the first downlink message.

### Initiation Methods

Connections can be initiated by:

- **Pilot-initiated:** Aircraft sends `REQUEST LOGON` when entering ATSU airspace
- **Controller-initiated:** Ground station sends `CONNECTION REQUESTED` to prompt the pilot to logon

In both cases, the server automatically accepts valid logon requests.

## Data Authority

Data authority determines which ground station is responsible for communicating with an aircraft.

### Current Data Authority

The Current Data Authority (CDA) is the ground station currently responsible for the flight. CPDLC dialogues between the pilot and controller take place through the CDA.

Aircraft may reject uplink messages from stations that are not their Current Data Authority.

### Next Data Authority

The Next Data Authority (NDA) is the ground station designated to take over communications. This typically occurs when an aircraft connects to the next ATSU preemptively before a handoff.

New connections from aircraft always start as Next Data Authority. An aircraft in NDA state is promoted to CDA upon receipt of the first downlink message.

## Uplink and Downlink Messages

CPDLC messages are categorised by their direction:

- **Uplink messages** are sent from the ground station to the aircraft (controller to pilot)
- **Downlink messages** are sent from the aircraft to the ground station (pilot to controller)

Uplink and downlink messages have different properties, particularly around response requirements.

## Message Composition

CPDLC messages are typically standardised templates with elements for the pilot or controller to populate.
For example, a climb clearance template might include a placeholder for the flight level.

Multiple messages can be joined together and sent as a single uplink.
This is useful when issuing related instructions together.

Free-text messages allow for manual text entry when no template from the message set is appropriate.

## Response Types

Each message template specifies a response type that determines how the recipient should respond.

### Uplink Message Response Types

| Response Type | Description |
| :-----------: | ----------- |
| `WU` | Pilot responds with WILCO or UNABLE |
| `AN` | Pilot responds with AFFIRM or NEGATIVE |
| `R` | Pilot responds with ROGER |
| `NE` | No response expected |

### Downlink Message Response Types

| Response Type | Description |
| :-----------: | ----------- |
| `Y` | ATC must respond to the message |
| `N` | ATC is not required to respond |

### Concatenated Message Response Types

When multiple uplink messages are joined together, the response type for the entire message is determined by priority:

1. `WU` (highest priority)
2. `AN`
3. `R`
4. `NE` (lowest priority)

For example:

| Message | Individual Response Type |
| :-----: | :----------------------: |
| `CLIMB TO FL370` | `WU` |
| `REPORT PASSING FL350` | `R` |
| **Concatenated Message** | **`WU`** |

Because `WU` has the highest priority, the pilot will be prompted to respond with WILCO or UNABLE for the entire concatenated message.

## Dialogues

Dialogues represent a conversation between aircraft and ground stations. A dialogue can be:

- A single message that is immediately closed; or
- A series of related messages linked through Message Reference Numbers (MRN)

### Dialogue State

- A dialogue is **open** if any message within it is open
- A dialogue is **closed** when all messages within it are closed

Messages that do not require a response (`NE` for uplinks, `N` for downlinks) are closed automatically.
Some messages are exempt from closing dialogues, such as `STANDBY` and `REQUEST DEFERRED`.

### Example: Climb Request

```mermaid
sequenceDiagram
    participant Pilot
    participant ATSU

    Pilot->>ATSU: REQUEST CLIMB TO FL370
    ATSU->>Pilot: CLIMB TO FL370
    Pilot->>ATSU: WILCO
```

| Step | Message | Response Type | Uplink State | Downlink State | Dialogue State |
| :--: | ------- | :-----------: | :----------: | :------------: | :------------: |
| 1 | Pilot: REQUEST CLIMB TO FL370 | `Y` | | Open | Open |
| 2 | ATC: CLIMB TO FL370 | `WU` | Open | Closed | Open |
| 3 | Pilot: WILCO | `N` | Closed | Closed | Closed |

### Example: Climb Request with STANDBY

```mermaid
sequenceDiagram
    participant Pilot
    participant ATSU

    Pilot->>ATSU: REQUEST CLIMB TO FL370
    ATSU->>Pilot: STANDBY
    ATSU->>Pilot: CLIMB TO FL370
    Pilot->>ATSU: WILCO
```

| Step | Message | Response Type | Uplink State | Downlink State | Dialogue State |
| :--: | ------- | :-----------: | :----------: | :------------: | :------------: |
| 1 | Pilot: REQUEST CLIMB TO FL370 | `Y` | | Open | Open |
| 2 | ATC: STANDBY | `NE` | | Open | Open |
| 3 | ATC: CLIMB TO FL370 | `WU` | Open | Closed | Open |
| 4 | Pilot: WILCO | `N` | Closed | Closed | Closed |

Note that STANDBY does not close the pilot's request.
The dialogue remains open until the controller issues a clearance and the pilot responds.

## Authority Transfer

When transferring an aircraft to another ATSU, the system automatically sends a `NEXT DATA AUTHORITY` uplink message well before the FIR boundary.
This instructs the pilot to establish a connection with the next ATSU and terminate the connection with the current one.

```mermaid
sequenceDiagram
    participant YBBB
    participant Pilot
    participant YMMM

    YBBB->>Pilot: NEXT DATA AUTHORITY YMMM
    Pilot->>YMMM: REQUEST LOGON
    YMMM->>Pilot: LOGON ACCEPTED
    YBBB-->>Pilot: (messages can still be exchanged)
    Pilot-->>YBBB:
    YBBB->>Pilot: END SERVICE
    Pilot->>YMMM: CURRENT DATA AUTHORITY
    YMMM-->>Pilot: (service continues with YMMM)
    Pilot-->>YMMM:
```

## Connection Termination

Connection termination occurs when a CPDLC session between an aircraft and ATSU ends.
The `END SERVICE` uplink message signals to the pilot that the ATSU is ending the CPDLC session.


:::note
In certain aircraft, reception of the `END SERVICE` (`LOGOFF` for Hoppie's) will cause the aircraft to automatically terminate the CPDLC connection.
:::

### Termination Timing

Connection termination is deferred until the dialogue containing the `END SERVICE` message closes.
This allows controllers to include frequency change instructions, or any additional messages with the termination message.

**Self-closing END SERVICE:**
- When `END SERVICE` is sent alone, no response is required
- The message closes immediately
- The connection terminates immediately

**END SERVICE with additional message elements:**
- When `END SERVICE` includes other elements that require a response (e.g., "END SERVICE. CONTACT MELBOURNE ON 123.45")
- The message remains open until the pilot responds
- The dialogue remains open until all messages are closed
- The connection terminates only after the pilot responds and the dialogue closes

### Example: END SERVICE with Frequency Change

```mermaid
sequenceDiagram
    participant Pilot
    participant YBBB

    YBBB->>Pilot: END SERVICE. CONTACT MELBOURNE ON 123.450
    Note over YBBB,Pilot: Connection remains active
    Pilot->>YBBB: WILCO
    Note over YBBB,Pilot: Dialogue closes, connection terminates
```

## CPDLC on VATSIM

VATSIM does not have an official CPDLC solution, though one is in development. In the meantime, CPDLC for flight simulation networks typically uses Hoppie's ACARS network.

### Hoppie's ACARS Network

Hoppie's network does not prescribe a standard protocol or message format for CPDLC.
ATC client and aircraft addon developers must agree on message formats.
This can lead to incompatibilities between different implementations.

Stations connected to Hoppie's network poll for messages at regular intervals.
Messages are not received instantly by the recipient.
It may take up to one minute for a message to reach the pilot, and another minute for a response to return.

### Aggregation Server

Hoppie's network does not allow multiple controllers to use a single ATSU callsign. The aggregation server solves this problem by acting as a relay between controllers and the ACARS network, allowing multiple controllers to share a single ATSU connection.
