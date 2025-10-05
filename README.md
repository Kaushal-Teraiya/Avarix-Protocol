# PvP Arena/Blitzone-Warfare - Game Programmer Portfolio

Watch the video here: [Portfolio game demo on YouTube](https://youtu.be/9sCAUyiXATg?si=nLa7sp9CUbbPbNDO)

Visit my LinkedIn:  
[LINKEDIN_PROFILE](https://www.linkedin.com/in/kaushal-teraiya-875596384/)

Hi! 👋  I'm Kaushal Teraiya. This project represents **9 months of self-learning and dedication**.  
This repository contains the **core scripts** for my Unity multiplayer FPS project **"Blitzone Warfare"**, submitted for the **Unity × Google Play Game Dev Training Program** as part of my Game Programmer portfolio.

---

## 🎮 About the Project

**PvP Arena/Avarix Protocol** is a **multiplayer FPS** game with both 1v1 and team-based matches (up to 4v4).  
Currently in **beta testing for PC**, with plans to release on **Android** in the future.  

Key features include:

- **9 playable characters**, each with unique abilities  
- Multiple **weapons** per character type (sniper, assault rifle, pistol, SMG, machine gun)  
- **Bots** with AI state machines and CTF AI brains: Survival Brain, Combat Brain, Strategic Brain, and Flag Handling Brain  
- **Capture the Flag mode** with dynamic AI handling flag logic  
- Match mechanics: timer, score tracking, respawn, kill/death tracking, networked scoreboard, and winning conditions  
- Multiplayer support via **LAN or network (VPS server in progress)**  
- **Private rooms**, matchmaking in progress  
- **Firebase integration:** login/signup system, one-device-at-a-time session check, server-side rewards, and saving player progress securely  

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
| AI Bot 🤖 | CTF AI: Survival, Combat, Strategic & Flag Handling brains | ✅ Done |

> **AI Scripts Highlight:**  
> The AI system is modular and sophisticated, with **Survival, Combat, Strategic, and Flag Handling brains**.  
> These scripts manage **bots and AI-controlled players**, making gameplay dynamic, intelligent, and challenging.  
> All AI scripts are located in the `_Scripts/AI` folder.

> **Tip:** If Unity pauses the game when logging errors, turn off "Error Pause" in the console—it’s not a bug.  

--- 

## 🔧 Project Setup

This repository contains **scripts only**, which implement all the gameplay, AI, networking, and UI logic.  
To run the full game, the **scenes, assets, and server setup** are required.  

PvP Arena/  
│  
├─ _Scripts/ # C# scripts for gameplay, AI (including CTF AI), weapons, UI, networking, and Firebase integration  
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

Watch the video here: [Portfolio game demo on YouTube](https://youtu.be/9sCAUyiXATg?si=nLa7sp9CUbbPbNDO)  

---


## 💻 Check the Code

All scripts are available in the `_Scripts` folder.  
You can see how I implemented:

- Core FPS mechanics (movement, shooting, recoil, weapons)  
- Multiplayer sync logic and client-host interactions  
- AI state machine for bots, including **CTF AI with Survival, Combat, Strategic, and Flag Handling brains**  
- UI, match scoring, timers, and win conditions  
- **Firebase login/signup and secure server-side reward system**  

GitHub repo link: [1v1 Arena Scripts](https://github.com/Kaushal-Teraiya/Avarix-Protocol)  

---

## ✨ About Me

I am a passionate **Game Developer** focused on:

- **Game Programming:** FPS, multiplayer, AI, mechanics  
- **Graphics & Physics:** Realistic simulations and VFX  
- Mastering **Unity and C#**  

I am applying to the **Unity × Google Play Game Dev Training Program** to refine my skills and contribute to exciting projects.  

---

Thank you for checking out my work!  
Feel free to reach out on LinkedIn: [LINKEDIN_PROFILE](https://www.linkedin.com/in/kaushal-teraiya-875596384/)  
