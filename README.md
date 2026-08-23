# Cloning of the Project

To avoid problems with assets handled by GIT LFS, use the following commands in the terminal to clone the project:
1. ```git lfs install```
2. ```git clone https://github.com/finn-odt/Game-Programming-2_2026_Pirate-Bay.git```
Theoretically, LFS-Files should be included in the downloadable zip-File.

Use Unity 6000.4.4f1 for the best version compatibility.

# Pirate Bay

You're a pirate on the main island of a pirate hideout and you're mate has been captured by a mean captain and some renegade skeletons. Now it is your duty to help him.
But you have no money, no weapons, or anything else with you. So come on, pirate, aye!

## The Project

This Game is an alternative exam assignment for the *Game Programming 2* course in the second semester of the *Animation & Game* degree programme, teached by Martin Leissler.

The task was to put into practice all the topics covered in the lectures and create a playable game (experience). My personal idea behind the development was to learn more of Unity's processes and just try out different systems to have the best skillset for the upcoming semester projects. Due to this, the game experience is a little bit random and has no clear gameplay logic, as I programmed so many different systems that it got hard to combine them in the end. For me, it's still a big learning, as one perfect polished game does not teach me as much as a weird sandbox game with a lots of different systems.

For the main graphics, the *Synty POLYGON Pirate Pack* from Unity's Asset Store was used, as well as a Skybox from *Poly Haven* and manually adapted, royalty-free icons and sounds from the internet for UI or sound effects.
In addition to the standard packages, the following Unity packages were used:
- Curved Text Easy
- Starter Assets - ThirdPerson | URP
- Opsive Ultimate Character Controller
- Opsive Swimming & Climbing Add-On
- Wingman - Your Inspector's Best Friend
- Unity Constants Generator
- Tri Inspector (CodeWriter)
- Unity Service Locator (Adammyhre)
- Scene Management (Adammyhre)
- Scene Reference (Eflatun)

## Inputs/Controls

### Keyboard

| Button    | Function |
| -------- | ------- |
| WASD  | Move    |
| SHIFT | Run     |
| SPACE    | Jump    |
| E    | Pickup/Interact    |
| F    | Use ability (climb, dive)    |
| Mouse Move    | Look around    |
| Mouse Click    | Use item in respective hand    |
| TAB    | Toggle Inventory    |
| ESC    | Toggle Pause Menu    |
| ENTER    | Agree to conversation    |
| BACKSPACE    | Decline conversation    |
| R    | Restart Game (when Game Over or Won)    |

### Gamepad

| Button    | Function |
| -------- | ------- |
| Left Joystick  | Move    |
| Left Joystick Press | Run     |
| A (Button South)    | Jump    |
| X (Button West)    | Pickup/Interact    |
| B (Button East)    | Use ability (climb, dive)    |
| Right Joystick    | Look around    |
| Left/Right Trigger    | Use item in respective hand    |
| Left Shoulder    | Toggle Inventory    |
| Start    | Toggle Pause Menu    |
| A (Button South)    | Agree to conversation    |
| B (Button East)    | Decline conversation    |
| Select    | Restart Game (when Game Over or Won)    |

## What you can do

You can walk, jump, run, swim, dive and climb specific ladders (two in the game). You can use items in your hands (that you can equip in the inventory after picking up objects), but not all items have functionalities (only bomb and smoke grenade I think). You can interact with objects (Canons, Loot Chests, Sailors), and therefore also collect money, spend money and travel on boats like a Taxi (but be careful, they have strict routes and they won't wait for you). 
There are NPCs that follow you, as soon as you are close enough and in their vision field. Be careful, they run! If they come close, they hurt you (yes, the hit-animation is missing due to the Animator being controlled the the ThirdPersonStarterAI-code), but if they loose you out of seight, they will go back to patroling. There are also sharks that can hunt and hurt you in the water.
Play the game and find out all funny interactions and mechanics :)

# How the Requirements were fulfilled

| Requirement    | Implementation |
| -------- | ------- |
| Pathfinding  | Sharks, NPCs and boats navigate autonoumously through the world with three different NavMeshs.   |
| FSM | FSMs are used for Sharks, three different types of NPCs and the overall GameState (which should be overworked).     |
| Observer Pattern    | The GameEventManager handles all communication between scripts via event-raising, registering and unregistering.    |
| Performant handling and spawning of objects    | UnityServiceLocator for quick performant Player-Fetching & Pooling-System for Items and Sharks    |
| Feel Asset/Juice    | I used the Feel-Asset only a little bit (for letting the loot chests jump when being opened), as I added Juice with Sounds, Cutscenes and overall gameplay. I wanted to use Feel more, but e.g. the Camera Shake wouldn't work with the Camera Controller from Opsive, that is used for the UCC.    |
| Scriptable Objects    | Items, their Use-Behaviour and e.g. the SceneGroupCollection for the SceneLoader.    |
| High-Level Gameplay Systems    | Inventory, Dialogue, Stealth (NPC avoidance), Underwater Behaviour.   |
| Cinemachine    | Used for the gameplay camera, as well as heavily for the two cut scenes.    |
| Visual Quality    | Specific Skybox and different Post-Processing Volumes with a VolumeController that changes according to the game state.    |
| Bonus    | Two full cut scenes with additional loading screen, main menu, pause menu and different UI overlays for all scenes. Sharks that are spawned automatically and move autonoumously while being able to attack as well as boats that can be used as taxi service when giving money to the sailor. Special Shader for the target, so that the player can see their goal.    |