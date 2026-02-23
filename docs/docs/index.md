---
sidebar_position: 1
hide_table_of_contents: true
---

# CPDLC Plugin

:::warning Flight Simulation Only
This software is intended for **flight simulation use only** on networks such as VATSIM. It must not be used for real-world aviation or air traffic control operations. No warranty is provided; all content is offered "as is" without liability.
:::

The CPDLC Plugin enables Controller-Pilot Data Link Communications for [vatSys](https://virtualairtrafficsystem.com) on the VATSIM network.

## Overview

The system consists of two components:

**[CPDLC Server](/server)** connects to ACARS networks (like Hoppie's) and manages ATSU connections.
Multiple controllers can share a single ATSU connection, with messages automatically routed to the appropriate controller.

**[vatSys Plugin](/vatsys-plugin/installation)** provides the controller interface within vatSys for sending and receiving CPDLC messages with aircraft.

![Architecture Diagram](../static/diagram.png)

## Getting Started

1. Follow the [Installation Guide](/vatsys-plugin/installation) to set up the plugin
2. Review [CPDLC Concepts](/concepts) to understand how CPDLC works
3. Read the [Plugin Guide](/vatsys-plugin/plugin) to learn how to use the interface
