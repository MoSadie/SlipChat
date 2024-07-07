import streamDeck, { action, KeyDownEvent, SingletonAction, WillAppearEvent } from "@elgato/streamdeck";

/**
 * An example action class that displays a count that increments by one each time the button is pressed.
 */
@action({ UUID: "com.mosadie.slipchat.sendannouncement" })
export class SendAnnouncement extends SingletonAction<SendAnnouncementSettings> {
	/**
	 * The {@link SingletonAction.onWillAppear} event is useful for setting the visual representation of an action when it become visible. This could be due to the Stream Deck first
	 * starting up, or the user navigating between pages / folders etc.. There is also an inverse of this event in the form of {@link streamDeck.client.onWillDisappear}. In this example,
	 * we're setting the title to the "count" that is incremented in {@link SendAnnouncement.onKeyDown}.
	 */
	onWillAppear(ev: WillAppearEvent<SendAnnouncementSettings>): void | Promise<void> {
		// Default settings check
		let ip = ev.payload.settings.ip ?? "127.0.0.1";
		let port = ev.payload.settings.port ?? 8002;
		let message = ev.payload.settings.message ?? "";

		// Update the current count in the action's settings, and change the title.
		ev.action.setSettings({ ip, port, message });
	}

	/**
	 * Listens for the {@link SingletonAction.onKeyDown} event which is emitted by Stream Deck when an action is pressed. Stream Deck provides various events for tracking interaction
	 * with devices including key down/up, dial rotations, and device connectivity, etc. When triggered, {@link ev} object contains information about the event including any payloads
	 * and action information where applicable. In this example, our action will display a counter that increments by one each press. We track the current count on the action's persisted
	 * settings using `setSettings` and `getSettings`.
	 */
	async onKeyDown(ev: KeyDownEvent<SendAnnouncementSettings>): Promise<void> {
		await ev.action.setTitle(`Sending...`);
		let ip = ev.payload.settings.ip ?? "127.0.0.1";
		let port = ev.payload.settings.port ?? 8002;
		let message = ev.payload.settings.message ?? "";

		let url = `http://${ip}:${port}/sendchat`;

		console.log("Sending message");
		
		// Validate the url
		try {
			new URL(url);
		} catch (error) {
			console.error("Invalid URL:", error);
			await ev.action.showAlert();
			await ev.action.setTitle(`Failed`);
			setTimeout(() => ev.action.setTitle(), 5000);
			return;
		}

		// Send the message as a GET request with the message as a query parameter
		try {
			let res = await fetch(`${url}?message=${encodeURIComponent(message)}`);
			if (!res.ok) {
				console.error("Failed to send message:", res.statusText);
				await ev.action.showAlert();
				await ev.action.setTitle(`Error`);
				setTimeout(() => ev.action.setTitle(), 5000);
				return;
			}
		} catch (error) {
			console.error("Failed to send message:", error);
			await ev.action.showAlert();
			await ev.action.setTitle(`Failed :(`);
			setTimeout(() => ev.action.setTitle(), 5000);
			return;
		}
		
		console.log("Message sent:", message);


		// Update the settings, in case any defaults were used.
		await ev.action.setSettings({ ip, port, message });
		await ev.action.setTitle(`Sent!`);
		setTimeout(() => ev.action.setTitle(), 5000);
		await ev.action.showOk();
	}
}

/**
 * Settings for {@link SendAnnouncement}.
 */
type SendAnnouncementSettings = {
	ip: string;
	port: number;
	message: string;
};
