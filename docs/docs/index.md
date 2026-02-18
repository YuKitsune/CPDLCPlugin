---
sidebar_position: 1
---

# CPDLC Server

:::warning Flight Simulation Only
This software is intended for **flight simulation use only** on networks such as VATSIM. It must not be used for real-world aviation or air traffic control operations. No warranty is provided; all content is offered "as is" without liability.
:::

The CPDLC Server handles communication between air traffic controllers and aircraft over ACARS networks like Hoppie's.
It manages ATSU connections, relays messages, and tracks dialogues.
Multiple controllers can share a single ATSU connection, with messages automatically routed to the appropriate controller.

The companion CPDLC Plugin for [vatSys](https://virtualairtrafficsystem.com) provides the controller interface, allowing you to send and receive CPDLC messages with any aircraft connected to your ATSU.

## Getting Started

Follow the [Installation Guide](./user-guide/01-installation.md) to set up the plugin.
