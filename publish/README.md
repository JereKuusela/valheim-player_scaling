# Player Scaling

Allows admins to change the size of each player.

Install on all clients and on the server (modding [guide](https://youtu.be/L9ljm2eKLrk)).

# Usage

Adds a new console command for admins: `scale_player [player/character id/steam id] [scale or x,y,z] [offset from ground]`.

Adds a new console command with debug mode: `scale_self [scale or x,y,z] [offset from ground]`.

Scaling information is saved per game world.

When scaling each axis separately, the player may start floating so the last parameter can be used to fine tune this.
