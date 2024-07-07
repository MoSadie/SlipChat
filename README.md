# SlipChat
Trigger the in-game announcement feature via a local API

Listens for an HTTP request and, after validating the message and other checks, sends it!

Send a GET request to `http://localhost:8002/sendchat?message=Hello%20World` to trigger an announcement of "Hello World" in-game.

Current requirements:
- Must be captain of the ship
- Must be at the Helm station.

In addition, you can use special $variables to automatically replace these with values from in-game:
 Variables: $captain, $randomCrew[id], $crew[id] $enemyName, $enemyIntel, $enemyInvaders, $enemyThreat, $enemySpeed, $enemyCargo, $campaignName, $sectorName
- $captain: The name of the Captain
- $randomCrew[id]: A random crew member's name, replace [id] to keep it consistant in the message (ex $randomCrew1)
- $crew[id]: The crew member with that numeric id, replace [id] with a number (ex $crew1)
- $enemyName: The name of the enemy ship
- $enemyIntel: The intel of the enemy ship
- $enemyInvaders: The invaders from the enemy ship
- $enemyThreat: The threat level of the enemy ship
- $enemySpeed: The speed of the enemy ship
- $enemyCargo: The cargo of the enemy ship
- $campaignName: The name of the campaign (ex Pluto)
- $sectorName: The name of the sector (ex Pluto Outskirts)