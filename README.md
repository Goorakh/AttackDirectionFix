# Attack Direction Fix

Fixes attacks not always aiming where your crosshair is. Also known as the "Pierce Bug".

## The problem

![](https://github.com/Goorakh/AttackDirectionFix/blob/master/ReadmeAssets/Problem.jpg?raw=true)

There are 2 relevant components here. The *Aim Origin* and the *Aim Direction*

The *Aim Origin* is the point on the character where most attacks (projectiles, bullets, and a few other things) originate from, usually located in the upper torso of the survivor.

The *Aim Direction* is the direction attacks should go in, this is determined from the camera's rotation.

However, the game can't just use the *Aim Origin* with the raw facing direction of the camera, since that would mean projectiles never impact where you're aiming (they would always be offset down slightly).

The simple fix for this is to figure out what the player is looking at (closest object that is under your crosshair), then offset the *Aim Direction* such that a straight line from the *Aim Origin* will intersect the camera view direction at that point. This is what the game does normally.

This works just fine for attacks that don't pierce, but looking at the image above, if the attack is allowed to continue through the target, the attack will follow the **Actual** path, not the **Expected** path, and it will seem to tilt up slightly after it pierces, this is what's known as the "Pierce bug".

A slightly less-brought-up issue that this causes is the following scenario:
![](https://github.com/Goorakh/AttackDirectionFix/blob/master/ReadmeAssets/Problem_Obstruction.jpg?raw=true)

Even though your crosshair is over the Lemurian, some object is obstructing the line between your *Aim Origin* and the target, which will cause your attacks to unexpectedly impact the obstruction instead.

## The solution

The solution this mod uses is to always use the camera as the *Aim Origin* and the raw camera orientation as the *Aim Direction*. This makes all attacks perfectly follow where your crosshair is at all times.

Projectiles shooting straight out of the camera looks bad however. So one final thing the mod does is for a short period of the projectile's lifetime, projectiles visuals are offset to appear to come out of the model as usual, and over-time interpolate to the actual path. Projectile collision and everything logic related follows the **Actual** path while everything visual follows the **Visual** path before lining up with the **Actual** path after a short while.

![](https://github.com/Goorakh/AttackDirectionFix/blob/master/ReadmeAssets/Solution.jpg?raw=true)