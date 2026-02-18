# CPDLC Overview

TODO:

- ATSUs and Aircraft
    - An ATSU is a ground station connected to an ACARS network that aircraft can communicate with

- Uplink and Downlink Messages
    - Uplink messages are sent from the ground station to the aircraft
    - Downlink messages are sent from the aircraft to the ground station
    - Uplink and downlink messages can have different properties

- Message composition
    - CPDLC messages are typically standardised templates with elements for the pilot or controller to populate
    - Multiple messages can be joined together and sent in a single uplink message
    - Free-text messages allow for manual text entry when no message template from the set is appropriate

- Response types
    - For Uplink Messages:
        - `WU`: The pilot will be prompted to respond with either "WILCO" or "UNABLE"
        - `AN`: The pilot will be prompted to respond with either "AFFIRMATIVE" or "NEGATIVE"
        - `R`: The pilot can only respond with "ROGER"
        - `NE`: The pilot will not be asked to respond to the message
    - For Downlink Messages:
        - `Y`: ATC are required to respond to the message
        - `N`: ATC are NOT required to respond to the message
    - Each message template specifies a response type
    - If multiple uplink message templates are joined together, the response type for the entire concatenated message will be prioritised by:
        1. `WU` (highest priority)
        2. `AN`
        3. `R`
        4. `NE` (lowest priority)
        TODO: Include a table with three message templates and their individual response types, then show the resulting response type for the entire message at the end.

- Dialogues
    - Dialogues represent a conversation between air and ground stations.
    - Dialogues can be a single message that is a closed message; or
    - a series of messages beginning with an open message, consisting of any messages related to the original open message and each other through the use of a Message Reference Number (MRN) and ending when all of these messages are closed.
    - a CPDLC dialogue is open if any of the CPDLC messages in the dialogue are open;
    - a CPDLC dialogue is closed if all CPDLC messages in the dialogue are closed.

    - Messages that do not require a response (i.e. `NE` for uplinks, and `N` for downlinks) are closed automatically.
    - Some messages are exempt from closing dialogues (i.e. `STANDBY`, and `REQUEST DEFERRED`)
    - Responses are correlated to messages by their `MRN` (message response number)

TODO: Add a mermaid diagram with an aircraft on the left and the ATSU on the right.
Pilot -> ATSU: REQUEST CLIMB TO FL370
ATSU -> Pilot: CLIMB TO FL370
Pilot -> ATSU: WILCO

|Step|Message|Response|Uplink State|Downlink State|Dialogue State|
|----|-------|--------|------------|--------------|--------------|
|1|Downlink request|`Y`||Open|Open|
|2|Uplink response|`WU`|Open|Closed|Open|
|3|Downlink response|`N`|Closed|Closed|Open|

TODO: Add a mermaid diagram with an aircraft on the left and the ATSU on the right.
Pilot -> ATSU: REQUEST CLIMB TO FL370
ATSU -> Pilot: STANDBY
ATSU -> Pilot: CLIMB TO FL370
Pilot -> ATSU: WILCO

|Step|Message|Response|Uplink State|Downlink State|Dialogue State|
|----|-------|--------|------------|--------------|--------------|
|1|Downlink request|`Y`||Open|Open|
|2|Standby response|`NE`|Open|Open|Open|
|3|Uplink response|`WU`|Open|Closed|Open|
|4|Downlink response|`N`|Closed|Closed|Open|

- Data authority
    - Next Data Authority: The ground system so designated by the current data authority through which an onward transfer of communications and control can take place.
        - This typically means the aircraft has connected to the ATSU preemptively.
        - New connections from aircraft will always start as Next Data Authority
        
    - Current Data Authority: The designated ground system through which a CPDLC dialogue between a pilot and a controller currently responsible for the flight is permitted to take place.
        - Aircraft in the Next Data Authority state will be promoted to Current Data Authority on recept of the first downlink message.
    - Aircraft may reject CPDLC uplinks from an ATSU if they are not the Current Data Authority.

- CPDLC on VATSIM
    - VATSIM does not have a bespoke CPDLC solution, though they are actively working on this.
    - CPDLC for use within Flight Simulation Networks typically happens over Hoppie's ACARS network.
    - Hoppie's doesn't prescribe a standard protocol or message format for CPDLC messages, so it's up to ATC client and addon aircraft developers to agree on a message format.
    - Some messages sent between aircraft and ATC may be incompatible due to differences in addon implementation.
    - Hoppies doesn't allow multiple controllers to utilise a single ATSU. The aggregation server exists to solve this problem, acting as a relay between ATC and the ACARS network.
