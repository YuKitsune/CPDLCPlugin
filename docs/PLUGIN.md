
TODO:
- Installation

- User Interface Overview
    - Main window layout
    - Key UI elements (brief, details in usage sections below)

- Connecting to the CPDLC Server
- Pilot Logon, Logoff and Handover
    - Pilot logons are accepted automatically
    - Controller-initiated logon `LOGON REQUESTED`
    - Use `LOGOFF` message to instruct pilots to logoff CPDLC
    - Use `NEXT DATA AUTHORITY [ICAO]` to handoff to next station (Sent automatically)

- Sending Uplink Messages
- Replying to Downlink Messages
- Managing Dialogues
    - Understanding Dialogue state
    - Message Acknowledgement (mark as read)
    - Manual Acknowledgement (Manually close a dialogue)

- Accessing historical messages

- Limitations
    - ADS-C not simulated
    - FANS and ATN not simulated
    - Equipment codes ignored
    - 

- Troubleshooting
    - Plugin not loading
    - Dialogues not closing
