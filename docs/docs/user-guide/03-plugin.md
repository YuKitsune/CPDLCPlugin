
# Plugin

This guide covers the CPDLC Plugin for vatSys.
It explains how to connect to the server, interpret track labels, and manage CPDLC messages with pilots.

## Connecting to the CPDLC Server

To connect, open the CPDLC menu and select Setup. Verify the server URL and station ID, then click Connect.

The station you select is your primary ATSU. Once connected, you can send and receive messages for any ATSU managed by that server. This allows controllers to extend their jurisdiction across FIR boundaries (e.g., Brisbane Centre extending to Melbourne Centre) when both ATSUs share a server.

## Track Label Item

<!-- TODO: Screenshot -->

The CPDLC track label item shows the connection status of each aircraft.

The connection status is denoted by the following symbols:

- (blank): Aircraft is not CPDLC capable
- `.`: Aircraft is connected to the ACARS network and available for logon
- `-`: Aircraft has logged on, we are the Next Data Authority
- `+`: Aircraft has logged on, we are the Current Data Authority

The background of the item will change when a downlink message has been received and is awaiting response.
The colour of the symbol will change when an `UNABLE` response has been received

Left-clicking the label item opens the CPDLC Editor.
If there are open downlink messages, the most recent one is automatically selected.

If the aircraft is not already logged on, left-clicking sends a `CONNECTION REQUESTED` message to initiate a controller-initiated logon.

## Editor Window

<!-- TODO: Screenshot -->

- Downlink Messages Area
- Message Class list
- Hot actions (<!-- TODO: Screenshot -->)
- Message Element list
- Message Element editor
- Escape
- Suspend
- Restore
- Send

## Current Messages Window

<!-- TODO: Screenshot -->

The Current Messages Window shows active CPDLC conversations that need your attention.
It opens automatically when there are open dialogues and closes when all dialogues are resolved.

### Reading Messages

Messages are colour-coded to indicate their state. The specific colours may vary between vatSys profiles. Refer to local procedures for your specific colour coding.

Unacknowledged messages appear with inverted colours. Left-click a message to acknowledge it (mark as read).

**Message indicators:**

- `*` — Message is truncated. Right-click to expand.
- `P` — Message contains free-text.

<!-- TODO: Screenshot of expanded message -->

Once a dialogue is closed and acknowledged, messages move to the [History Window](#history-window).

### Responding to Messages

Left-click the callsign to open the action menu. The available options depend on the message state.

For downlink messages (pilot requests), each menu item sends the corresponding uplink response to the pilot.
For example, selecting `STANDBY` sends the `STANDBY` message.

Additional actions:

- `MANUAL ACK`: Closes the message without a pilot response
- `RE-ISSUE`: Re-transmits a failed message
- `HISTORY`: Moves the dialogue to history

:::tip[Message ordering]
Messages are grouped by dialogue, not strictly by time. This means messages from different dialogues may appear out of chronological order, even though they are correctly ordered within each dialogue.

```
01:30: QFA1     REQUEST CLIMB TO FL370
01:31: QFA1     STANDBY
01:35: QFA1     CLIMB TO FL370
01:36: QFA1     WILCO
01:32: VOZ2     REQUEST WEATHER DEVIATION UP TO 20 NM LEFT OF ROUTE
01:33: VOZ2     CLEARED TO DEVIATE UP TO 20 NM LEFT OF ROUTE
01:33: VOZ2     WILCO
```
:::

## History Window

<!-- TODO: Screenshot -->

The History Window shows previously completed dialogues. Use it to review past conversations with an aircraft.

Enter a callsign in the ACID input to display that aircraft's message history. Only one aircraft can be viewed at a time.

Messages are displayed the same way as in the Current Messages Window. Messages prefixed with `M` were manually acknowledged by the controller.

## Voice Capability Indicators

<!-- TODO: Screenshot -->

vatSys uses the real-world CPDLC track label symbols (`.`, `-`, `+`) to indicate voice capabilities.
With this plugin introducing CPDLC functionality, those symbols are now used for their intended purpose.
The plugin provides a replacement label item for voice capability to avoid confusion.

The replacement label shows the aircraft's voice capability:

- (blank): Aircraft is fully voice-capable
- `R`: Aircraft is receive-only
- `T`: Aircraft is text-only
- `V`: Aircraft is voice-capable (only displayed when a text message has been received)

The background colour will change to indicate that a text message has been received.

Left-clicking on the label item will open the text message editor.

## Automatic Actions

The plugin handles certain actions automatically without controller input.

**Logon:** Pilot logon requests are automatically accepted by the server.

**Handoff:** When you hand an aircraft off to another controller, the plugin notifies the pilot. If the receiving controller is using CPDLC, a `NEXT DATA AUTHORITY` message instructs the pilot to transfer to the next unit. Otherwise, an `END SERVICE` message instructs the pilot to disconnect from CPDLC.

## Limitations

The plugin does not fully simulate all aspects of real-world CPDLC.

**ADS-C:** ADS-C position reporting is not simulated due to limitations within the vatSys plugin API.

**Equipment codes:** Aircraft equipment codes are ignored. The `.` symbol indicates the aircraft is connected to an ACARS network, regardless of their filed equipment.

## Troubleshooting

### Dialogues are not closing

This usually happens when you send a response without having the original downlink message selected. The response starts a new dialogue instead of closing the existing one.

Use `MANUAL ACK` on the original downlink message to close it.
