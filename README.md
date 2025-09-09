# SilklessCoop

View the Nexusmods page [here](https://www.nexusmods.com/hollowknightsilksong/mods/73).

A simple coop mod allowing you to see your friends in each others game.

It currently features two modes:

- Steam P2P (right click your friend and press "Join Game", requires all users to have valid steam copies)
- Standalone (requires you to set up a [server](https://github.com/nek5s/echoserver) or connect via VPN / port forwarding)

Disclaimer: this mod was made in 2 days with no prior modding experience, so expect bugs.

<details>
<summary>

## Screenshots / Videos

</summary>

[![Movement Footage](https://img.youtube.com/vi/CJR4MXvXHsI/0.jpg)](https://www.youtube.com/watch?v=CJR4MXvXHsI)

[![Combat Footage](https://img.youtube.com/vi/L90_3az_o0M/0.jpg)](https://www.youtube.com/watch?v=L90_3az_o0M)

![Bellhart Screenshot 1](./Media/bellhart_1.jpg)
![Bellhart Screenshot 2](./Media/bellhart_2.jpg)
![Bellhart Screenshot 3](./Media/bellhart_3.jpg)
![Bellhart Screenshot 4](./Media/bellhart_4.jpg)
![Bellhart Screenshot 5](./Media/bellhart_5.jpg)
![Shellwood Screenshot 1](./Media/shellwood_1.jpg)
![Shellwood Screenshot 1](./Media/shellwood_2.jpg)
![Shellwood Screenshot 1](./Media/shellwood_3.jpg)
![Shellwood Screenshot 1](./Media/shellwood_4.jpg)

Note: player counter in the bottom left corner when viewing the quick map (holding L1)

</details>

## Installation

- Download [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/) (tested on 5.4.23.3) and extract it into your root game folder
- Download [SilklessCoop.zip](https://github.com/nek5s/SilklessCoop/releases) and extract it into your root game folder
- Edit BepInEx/config/SilklessCoop.cfg to your liking (see file for details)
- Start the game

## Usage

- Install the mod
- Start the game load your save file
- Press F5 to enable multiplayer
- If you do not see your friends, open BepInEx/LogOutput.txt and check for errors or send the file to me

## Setting up a standalone server (mostly for non-steam players):



<ins>Option 1: hosting locally + Hamachi LogMeIn / Radmin</ins>

Player A starts the server locally and sets their server IP to 127.0.0.1.

Player A creates a network in Hamachi.

Player B connects to the created network.

Player B copies the hamachi IP address of player A (right click DESKTOP-xxxx -> copy IPv4).

Player B sets their server IP to the hamachi IP address.

Video guide provided by EEw33:

[![Radmin Video Guide](https://img.youtube.com/vi/Hfxq-sTTlzM/0.jpg)](https://www.youtube.com/watch?v=Hfxq-sTTlzM)



<ins>Option 2: hosting on a dedicated server (more technical)</ins>

Player A starts the server on a dedicated computer.

All players set their server IPs to the public IP of the server.



<ins>Option 3: hosting locally + port forwarding</ins>

Player A starts the server locally and sets their server IP to 127.0.0.1.

Player A enables port forwarding in their router settings for the selected port (default 45565).

Player B sets their server IP to the public IP of player A.

## Known bugs

- Some attacks have weird animations
- Disconnecting and reconnecting will create unmoving duplicates (they disappear when changing scenes)
- Sound isn't synchronised

## What's Next

- Bugfixes
- Syncing sounds
- Player count and connection status display
- Syncing compass icons on the map
- Arrow to other players on the border of the screen
- Public servers hosted by me (scary + expensive)
