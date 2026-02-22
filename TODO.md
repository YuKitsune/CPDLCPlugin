TODO:

- [ ] Don't return null when text indicator is not required, leave it there so the click-spot remains
- [ ] Fix jurisdiction checker.
- [X] Move NDA transmission to server. Plugin tells server what the NDA is, and the server sends it at the appropriate time.
- [X] Live NDA handoff test.
- [ ] Write unit tests for plugin code.
- [ ] Write test cases for same flight across multiple units within the same server.
- [ ] Clean up label item creation.
- [ ] Use "." for "CPDLC available", ignore equipment code.
- [ ] Add strip item cration.
- [ ] Write documentation.
- [ ] Write SOPs.

---

- [ ] BUG: NextDataAuthority check isn't frequent enough.
    - Needs more triggers, consider a time-based trigger in addition to events.
- [ ] BUG: Message routing appears correct, but messages are displayed inconsistently.
    - Current Messages Window appears, and is blank
    - Message sent to other YMMM ends up opening on YBBB
- [ ] ATSU2 seeing messages transmitted by ATSU1 in current messages (i.e, during handoffs. Should exclude dialogues where the uplink was sent by another controller)
- [ ] BUG: `ERROR. CONNECTION NOT ESTABLISHED.` sent from server after sending downlink.
