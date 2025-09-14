# 1v1 Arena - Game Programmer Portfolio

Hi! 👋  
This repository contains the **core scripts** for my Unity multiplayer FPS project **"1v1 Arena"**.  

I am submitting this for the **Unity × Google Play Game Dev Training Program** as part of my Game Programmer portfolio.

---

## 🎮 About the Project

**1v1 Arena** is a **multiplayer FPS** game with both 1v1 and team-based matches (up to 4v4).  
Currently in **beta testing for PC**, with plans to release on **Android** in the future.  

Key features include:

- **9 playable characters**, each with unique abilities  
- Multiple **weapons** per character type (sniper, assault rifle, pistol, SMG, machine gun)  
- **Bots** with AI state machines  
- **Capture the Flag mode**  
- Match mechanics: timer, score tracking, respawn, kill/death tracking, networked scoreboard, and winning conditions  
- Multiplayer support via **LAN or network (VPS server in progress)**  
- **Private rooms**, matchmaking in progress  

> Developing this multiplayer project was a challenge, as syncing changes between clients and host requires double the work compared to a single-player game.  

---

## 🏹 Character Abilities

| Character  | Ability Description | Status |
|------------|------------------|--------|
| Ninja 🥷    | Forward Dash ⚡   | ✅ Done |
| Archer 🎯   | One-shot kill for 5 seconds (Arrows VFX by Gabriel Aguiar) | ✅ Done |
| Eve 🛡     | Deployable Turret (auto-fire support) | ✅ Done |
| Survivor 👁 | X-Ray Vision (see through walls) | ✅ Done |
| Dreyar ⚔   | Shield (by Gabriel Aguiar) | ✅ Done |
| Vampire 🦇 | Ground Slam (Devastating impact) | ✅ Basic done, VFX/animations remaining |
| Knight ⚔  | Reflective Shield (Protect and damage) | ✅ Done |
| Knight ⚔  | Instant-Kill Sword Slash | ❌ Abandoned due to time limitations |

> **Tip:** If Unity pauses the game when logging errors, turn off "Error Pause" in the console—it’s not a bug.  

---

## 🔧 Project Setup

This repository contains **scripts only**, which implement all the gameplay, AI, networking, and UI logic.  
To run the full game, the **scenes, assets, and server setup** are required.  

1v1 Arena/
│
├─ _Scripts/ # C# scripts for gameplay, AI, weapons, UI, and networking
├─ Packages/ # Unity package manifest files (dependencies)
└─ ProjectSettings/ # Core Unity project settings (input, graphics, physics)



**Assets used:**
- VFX: Created using tutorials by Gabriel Aguiar  
- Models, textures, and animations: From Mixamo  

---

## 📺 Demo & Gameplay Explanation

Since the project is **multiplayer**, running it locally requires LAN or a server.  
I have prepared a **demo video** showing:

- Gameplay and mechanics  
- Project structure and workflow  
- Key features and multiplayer functionality  

Watch the video here: [LinkedIn Demo Video](YOUR_LINKEDIN_VIDEO_LINK)

---

## 💻 Check the Code

All scripts are available in the `_Scripts` folder.  
You can see how I implemented:

- Core FPS mechanics (movement, shooting, recoil, weapons)  
- Multiplayer sync logic and client-host interactions  
- AI state machine for bots  
- UI, match scoring, timers, and win conditions  

GitHub repo link: [1v1 Arena Scripts](https://github.com/Kaushal-Teraiya/Avarix-Protocol/tree/main)

---

## ✨ About Me

I am a passionate **Game Developer** focused on:

- **Game Programming:** FPS, multiplayer, AI, mechanics  
- **Graphics & Physics:** Realistic simulations and VFX  
- Mastering **Unity and C#**  

I am applying to the **Unity × Google Play Game Dev Training Program** to refine my skills and contribute to exciting projects.  

---

Thank you for checking out my work!  
Feel free to reach out on LinkedIn: [YOUR_LINKEDIN_PROFILE]
