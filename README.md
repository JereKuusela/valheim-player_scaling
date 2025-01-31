# Player Scaling

Allows admins to change the size of each player.

Install on all clients and on the server (modding [guide](https://youtu.be/L9ljm2eKLrk)).

# Usage

Adds new console commands:

- `scale_player [player] [scale or x,y,z] [offset from ground]`
- `offset_player [player] [offset from ground]`
- `scale_self [scale or x,y,z] [offset from ground]`
- `offset_self [offset from ground]`

Scaling information is saved per game world.

When scaling each axis separately, the player may start floating so the last offset parameter can be used to fine tune this.
