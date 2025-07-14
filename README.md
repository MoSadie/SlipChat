# SlipChat

Trigger the in-game custom order feature via a local API

Listens for an HTTP request and, after validating the message and other checks, sends a custom order to your crew!

Send a GET request to `http://localhost:8001/sendchat?message=Hello%20World` to trigger an announcement of "Hello World" in-game.

In addition to raw HTTP requests, there is also a Stream Deck Plugin to make custom orders.
You can download it from the [Elgato Marketplace](https://marketplace.elgato.com/product/slipchat-b2fbb151-3209-40a5-9541-5a4e9b86cf11) or use the manual download button. Inside the zip should be a file ending in `.streamDeckPlugin` you can use to install the plugin. The default settings should work, just need to add a message.

Current requirements to send a custom order:
- Must be Captain or First Mate of the ship.
- Must be at the Helm station.

In addition, you can use special $variables to automatically replace these with values from in-game:

### Crew Variables:
- $captain: The display name of the Captain
- $randomCrew[id]: A random crew member's name, replace [id] to keep it consistent in the message (ex $randomCrew1)
- $crew[id]: The crew member with that numeric id, replace [id] with a number (ex $crew0)

### Fight Variables:
(These will be blank if no fight is occurring)
- $enemyName: The name of the enemy ship
- $enemyIntel: The intel of the enemy ship
- $enemyInvaders: The invaders from the enemy ship
- $enemyThreat: The threat level of the enemy ship
- $enemySpeed: The speed of the enemy ship
- $enemyCargo: The cargo of the enemy ship

### Run Variables:
- $campaignName: The name of the campaign (ex Pluto)
- $sectorName: The name of the sector (ex Pluto Outskirts)

### Misc Variables:
- $version: The version of MoCore (for debugging purposes, no real purpose)