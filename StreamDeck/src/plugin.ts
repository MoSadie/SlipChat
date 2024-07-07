import streamDeck, { LogLevel } from "@elgato/streamdeck";

import { SendAnnouncement } from "./actions/send-announcement";

// We can enable "trace" logging so that all messages between the Stream Deck, and the plugin are recorded. When storing sensitive information
streamDeck.logger.setLevel(LogLevel.TRACE);

// Register the send announcement action.
streamDeck.actions.registerAction(new SendAnnouncement());

// Finally, connect to the Stream Deck.
streamDeck.connect();
