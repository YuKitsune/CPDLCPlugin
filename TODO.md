TODO:

- [ ] Fix jurisdiction checker.
- [ ] Move NDA transmission to server. Plugin tells server what the NDA is, and the server sends it at the appropriate time.
- [ ] Live NDA handoff test.
- [ ] Write unit tests for plugin code.
- [ ] Write test cases for same flight across multiple units within the same server.
- [ ] Clean up label item creation.
- [ ] Add strip item cration.
- [ ] Write documentation.
- [ ] Write SOPs.

---

- [ ] Callsign label item disappears
- [ ] BUG: When a handoff is initiated, the `ControllerTracking` appears to change, so the notification is raised while the aircraft is in the handover-out state.
- [ ] BUG: Message routing appears correct, but messages are displayed inconsistently.
    - Current Messages Window appears, and is blank
    - Message sent to other YMMM ends up opening on YBBB
- [ ] BUG: `ERROR. CONNECTION NOT ESTABLISHED.` sent from server after sending downlink.
