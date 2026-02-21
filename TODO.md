TODO:

- [ ] Don't return null when text indicator is not required, leave it there so the click-spot remains
- [ ] Fix jurisdiction checker.
- [X] Move NDA transmission to server. Plugin tells server what the NDA is, and the server sends it at the appropriate time.
- [ ] Live NDA handoff test.
- [ ] Write unit tests for plugin code.
- [ ] Write test cases for same flight across multiple units within the same server.
- [ ] Clean up label item creation.
- [ ] Add strip item cration.
- [ ] Write documentation.
- [ ] Write SOPs.

---

- [ ] BUG: When a handoff is initiated, the `ControllerTracking` appears to change, so the notification is raised while the aircraft is in the handover-out state.
- [ ] BUG: Duplicate NEXT DATA AUTHORITY messages are transmitted for the same flight.
- [ ] BUG: Message routing appears correct, but messages are displayed inconsistently.
    - Current Messages Window appears, and is blank
    - Message sent to other YMMM ends up opening on YBBB
- [ ] BUG: `ERROR. CONNECTION NOT ESTABLISHED.` sent from server after sending downlink.

---

## Automatic Handoff

- Parameters
    - HandoffMessageLeadTime

- When the FDR updates:
    - Calculate:
        - Sector entry time for each subsequent ATC sector
        - CPDLC code utilised within each sector
            - If multiple CPDLC codes are found, display an error to the user, but only if the CPDLC code conflict is with the **next** sector
        - The first entry where the CPDLC code is not the same as the current station ID is the ATSU boundary
        - Record the exit time, and next data authority

- For each CPDLC aircraft, re-calculate the ATSU exit time, and CPDLC code every N minutes
    - Need to ensure we're using fresh data, in case the subsequent sector goes offline after the initial calculation.

- At Sector Exit Time minus `HandoffMessageLeadTime`:
    - Calculate CPDLC code for the next sector
        - If multiple codes are found, show an error to the user
    - Transmit `NEXT DATA AUTHORITY ZZZZ` message to the aircraft
