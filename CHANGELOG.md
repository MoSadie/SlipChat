## v1.0.0

First major release!

Updated to support the latest game version.

## v0.1.1

Updated to support First Mate sending orders, and marking as compatible with the v1.90.0 update.

Also added MoCore to enable the ability to manage compatible versions without having to make a new release every time.

#### Known Issue

- If the first mate and captain are both on the helm (so the first mate cannot see the helm screen) they can still send messages.

## v0.1.0

### Initial (Testing) Release!

I got it working! Time to get it in the hands of the community so they can break it.

Check the main README page for for a list of everything you can replace in a message using variables.

I have not had time to test every single variable and I'm sure there's something that will break, but I feel it's ready enough for people to start coming up with ideas on how to use it.

For this test I've setup two easy ways to send announcements:

#### Stream Deck Plugin

If you click the "Manual Download" button to download the zip file, inside should be a file ending in `.streamDeckPlugin` you can use to install the plugin. Most of the default settings should work, just need to change the message.

#### Streamer.bot / MixItUp / HTTP request

You can make a get request to `http://localhost:8002/sendmessage?message=<insert message here>` to attempt to send a message. This is also called "Fetch URL" in Streamer.bot.

An easy idea for inspiration: Combine with SlipStreamer.Bot's existing actions StartFight for automatic announcements of fight information. (Remember Streamer.bot does not need you to be live for things to run. Offline ships could do this too.)

#### Known Issues:
- If you use a varible such as `$captain` but include anything else after (ex: `Hello $captain!` or `Hi $captain.`) then the variable will not work.