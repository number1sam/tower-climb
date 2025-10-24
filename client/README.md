# Tower Climb Game - Unity Client

Mobile-first tower climb game built with Unity. Players face randomized micro-challenges with adaptive difficulty and weekly leaderboard competition.

## Architecture Overview

```
Client (Unity C#)
├── Core Systems
│   ├── PatternGenerator (deterministic, server-synced)
│   ├── GameStateMachine (flow control)
│   └── PlayerModel (adaptive difficulty tracking)
│
├── Gameplay
│   ├── PatternExecutor (challenge execution)
│   ├── InputHandler (tap/swipe/hold/rhythm/tilt)
│   ├── AudioManager (SFX + music)
│   └── VFXManager (particles, shake, flash)
│
├── API
│   ├── SupabaseClient (HTTP wrapper)
│   └── SessionManager (auth + run flow)
│
└── UI
    ├── HomeScreen
    ├── GameScreen (PatternExecutor UI)
    └── ResultsScreen
```

## Project Structure

```
client/Assets/Scripts/
├── Core/
│   ├── GameTypes.cs                   # Shared data structures
│   ├── PatternGenerator.cs            # CRITICAL: Must match server
│
├── Utils/
│   └── SeededRandom.cs                # xoshiro128** PRNG
│
├── Gameplay/
│   ├── GameStateMachine.cs            # Game flow controller
│   ├── PatternExecutor.cs             # Challenge execution
│   ├── InputHandler.cs                # Multi-input detection
│   ├── AudioManager.cs                # Sound system
│   └── VFXManager.cs                  # Visual effects
│
├── API/
│   ├── SupabaseClient.cs              # HTTP API wrapper
│   └── SessionManager.cs              # Session orchestration
│
├── UI/
│   ├── HomeScreen.cs                  # Main menu
│   └── ResultsScreen.cs               # Post-run stats
│
└── Tests/
    └── PatternGeneratorTests.cs       # Unit tests (NUnit)
```

## Setup Instructions

### Prerequisites

- **Unity 2022.3 LTS** or newer
- **TextMeshPro** package
- **Unity Test Framework** (for tests)
- A deployed Supabase backend (see `server/README.md`)

### 1. Create Unity Project

```bash
# If creating from scratch:
Unity Hub → New Project → 2D Core
Project Name: TowerClimb
```

Or use the existing folder structure.

### 2. Install Required Packages

Open **Package Manager** (Window → Package Manager):

1. **TextMeshPro** (for UI text)
2. **Unity Test Framework** (for unit tests)
3. **Input System** (optional, for advanced input)

### 3. Import Scripts

Copy all scripts from `client/Assets/Scripts/` to your Unity project's `Assets/Scripts/` folder.

### 4. Configure Supabase Connection

Edit `SupabaseClient.cs`:

```csharp
[SerializeField] private string supabaseUrl = "https://your-project.supabase.co";
[SerializeField] private string supabaseAnonKey = "your-anon-key-here";
```

Get these values from your Supabase project dashboard (Settings → API).

### 5. Create Game Scene

#### A. Create Managers (DontDestroyOnLoad objects)

1. Create empty GameObject: `GameStateMachine`
   - Add component: `GameStateMachine.cs`

2. Create empty GameObject: `SessionManager`
   - Add component: `SessionManager.cs`

3. Create empty GameObject: `SupabaseClient`
   - Add component: `SupabaseClient.cs`
   - Set `supabaseUrl` and `supabaseAnonKey` in Inspector

4. Create empty GameObject: `AudioManager`
   - Add component: `AudioManager.cs`
   - Assign audio clips in Inspector

5. Create empty GameObject: `VFXManager`
   - Add component: `VFXManager.cs`
   - Assign particle systems and camera

#### B. Create UI Canvas

1. Create UI Canvas (right-click Hierarchy → UI → Canvas)
   - Set Canvas Scaler to "Scale With Screen Size"
   - Reference Resolution: 1080 x 1920 (portrait)

2. **HomeScreen Panel:**
   - Add Panel: `HomeScreen`
   - Add Button: "Play"
   - Add TextMeshPro: "Personal Best"
   - Add TextMeshPro: "Week Info"
   - Add component: `HomeScreen.cs`
   - Wire up UI references in Inspector

3. **GameScreen Panel:**
   - Add Panel: `GameScreen`
   - Add Image: `PatternIcon`
   - Add TextMeshPro: `PatternText`
   - Add Image: `TimerBar` (Fill type)
   - Add TextMeshPro: `FloorText`
   - Add GameObject: `InputHandler`
     - Add component: `InputHandler.cs`
   - Add component: `PatternExecutor.cs`
   - Wire up UI references

4. **ResultsScreen Panel:**
   - Add Panel: `ResultsScreen`
   - Add TextMeshPro: "Floors Reached"
   - Add TextMeshPro: "Stats"
   - Add Button: "Retry"
   - Add Button: "Home"
   - Add component: `ResultsScreen.cs`
   - Wire up UI references

### 6. Build Settings

1. File → Build Settings
2. Platform: Android or iOS
3. Add current scene to build
4. Player Settings:
   - **Android:**
     - Minimum API Level: 24 (Android 7.0)
     - Scripting Backend: IL2CPP
     - Target Architectures: ARM64
   - **iOS:**
     - Minimum iOS Version: 12.0
     - Architecture: ARM64

### 7. Run Tests

Window → General → Test Runner → PlayMode tab

Click "Run All" to verify pattern generator determinism.

Expected output:
```
✅ PRNG_ProducesConsistentResults_WithSameSeed
✅ PatternGeneration_IsDeterministic
✅ CooldownFloors_ProduceTapPatterns
... (10 tests total)
```

**CRITICAL:** All tests must pass for anti-cheat to work!

## Gameplay Flow

```
1. App Launch
   ↓
2. Anonymous Auth (auto)
   ↓
3. HomeScreen
   ↓ [Play Button]
4. SessionManager.StartRun()
   ↓ API: /start-run
5. Server returns: { seed, weekId }
   ↓
6. Pre-generate 100 patterns locally
   ↓
7. GameStateMachine.StartFloor()
   ↓
8. PatternExecutor shows pattern
   ↓
9. InputHandler detects player action
   ↓
10. Success → Next floor | Fail → End run
   ↓
11. SessionManager.SubmitRun()
   ↓ API: /submit-run
12. ResultsScreen (with PB comparison)
   ↓
13. [Retry] → Step 4 | [Home] → Step 3
```

## Input System

The `InputHandler` supports 6 input types:

| Type | Detection | Mobile | Desktop (testing) |
|------|-----------|--------|-------------------|
| **Tap** | Single touch < 0.3s | Touch screen | Mouse click |
| **Swipe** | Touch drag > 50px | Touch drag | Mouse drag |
| **Hold** | Touch held > 0.3s | Long press | Hold mouse |
| **Rhythm** | N taps in window | Multi-tap | Multiple clicks |
| **Tilt** | Accelerometer delta | Device tilt | Arrow keys (fallback) |
| **DoubleTap** | 2 taps < 0.3s apart | Quick double-tap | Double-click |

### Testing Input on PC

- **Tap:** Click
- **Swipe:** Click + drag
- **Hold:** Click + hold
- **Tilt:** Arrow keys (L/R/U/D)
- **DoubleTap:** Double-click
- **Rhythm:** Multiple quick clicks

## Pattern Generator: Client-Server Sync

**⚠️ CRITICAL REQUIREMENT:** The C# `PatternGenerator` must produce **identical** output to the TypeScript version on the server for the same seed and floor.

### Verification Steps

1. **Run C# unit tests:**
   ```
   Window → General → Test Runner → Run All
   ```

2. **Cross-platform test** (manual):
   - Server (TypeScript):
     ```bash
     cd server
     deno test --allow-all shared/utils/pattern-generator.test.ts
     ```
   - Client (Unity):
     ```
     Run PatternGeneratorTests in Unity Test Runner
     ```

3. **Compare outputs** for seed=12345, floor=10:
   - Both should produce same: `type`, `direction`, `timeWindow`, `speed`

### Known Seed Test

Use `KnownSeedFloor_ProducesExpectedPattern` test to verify against server:

```csharp
Seed: 1234567890123456789
Floor: 10
Expected (verify with server):
  Type: [Check server output]
  Direction: [Check server output]
  TimeWindow: [Check server output]
```

Run equivalent server test and compare.

## API Integration

### 1. Authentication

```csharp
// Auto-runs on app start via SessionManager
yield return supabaseClient.SignInAnonymously((success, error) => {
    if (success) {
        Debug.Log($"User ID: {supabaseClient.userId}");
    }
});
```

### 2. Start Run

```csharp
sessionManager.StartNewRun();
// → API call to /start-run
// → Returns: { userId, weekId, seed, currentBest }
// → Initializes GameStateMachine with seed
```

### 3. Submit Run

```csharp
// Auto-called when run ends
// Sends: { floors, timings[], playerModel, breakdown }
// Returns: { success, cheatFlags, newBest, unlocks[] }
```

### 4. Leaderboard

```csharp
sessionManager.FetchLeaderboard(weekId: null, scope: "global", (response) => {
    foreach (var entry in response.entries) {
        Debug.Log($"{entry.rank}. {entry.handle}: Floor {entry.bestFloor}");
    }
});
```

## Performance Optimization

### Pattern Pre-generation

Patterns are generated **once** at run start (100 floors):

```csharp
preGeneratedPatterns = PatternGenerator.GenerateSequence(
    seed, 1, 100, difficultyConfig, playerModel
);
```

**Why:** No network calls mid-game → buttery smooth gameplay.

### Offline Queue (TODO)

Failed submissions are queued and retried:

```csharp
// Store failed submission in PlayerPrefs
// Retry on next app launch
```

## Audio & VFX

### Audio Manager

Automatically plays SFX based on events:

- **Pattern shown:** Pattern-specific sound (tap/swipe/hold)
- **Perfect hit:** Chime + particles
- **Miss:** Error sound + screen shake
- **Run end:** Fail sound + flash

### VFX Manager

Visual feedback:

- **Perfect:** Green flash + particle burst
- **Good:** Small particles
- **Miss:** Red flash + medium shake
- **Fail:** Red flash + heavy shake

## Testing

### Unit Tests (NUnit)

Located in `Assets/Scripts/Tests/PatternGeneratorTests.cs`:

- PRNG determinism
- Pattern uniqueness
- Speed progression
- Cooldown floors
- Weakness adaptation
- Cross-platform consistency

Run in: **Window → General → Test Runner**

### Manual Testing Checklist

- [ ] Anonymous auth works
- [ ] Start run fetches seed
- [ ] Patterns are deterministic (same seed = same sequence)
- [ ] All 6 input types detected correctly
- [ ] Timing calculation accurate (< 100ms human min)
- [ ] Run submission succeeds
- [ ] Results screen shows stats
- [ ] Retry button starts new run
- [ ] Audio plays for each event
- [ ] VFX shows for perfect/miss
- [ ] Leaderboard loads

## Build & Deploy

### Android

```bash
# 1. Build APK
File → Build Settings → Platform: Android → Build

# 2. Test on device
adb install -r TowerClimb.apk

# 3. Upload to Google Play Console
```

### iOS

```bash
# 1. Build Xcode project
File → Build Settings → Platform: iOS → Build

# 2. Open in Xcode
cd build/iOS
open Unity-iPhone.xcodeproj

# 3. Configure signing & build
Product → Archive → Distribute App
```

## Troubleshooting

**Issue:** Patterns don't match server (cheat flag 4)

**Fix:**
1. Run `PatternGeneratorTests` in Unity
2. Run `deno test` on server
3. Compare outputs for same seed/floor
4. Check PRNG implementation byte-for-byte

---

**Issue:** "Not authenticated" error

**Fix:**
1. Check `SupabaseClient` is in scene
2. Verify `supabaseUrl` and `supabaseAnonKey` are set
3. Check Supabase Dashboard → Auth → Anonymous sign-in enabled

---

**Issue:** Input not detected

**Fix:**
1. Ensure `InputHandler` component is active
2. Check Unity Event System exists (UI → Event System)
3. Test with mouse first (desktop), then touch (mobile)

---

**Issue:** Audio not playing

**Fix:**
1. Check `AudioManager` has AudioSource components
2. Assign audio clips in Inspector
3. Verify volume > 0

## Next Steps

1. **Add Leaderboard UI** (fetch + display)
2. **Implement Missions** (daily challenges)
3. **Practice Mode** (drill specific patterns)
4. **Cosmetic Unlocks** (themes, SFX packs)
5. **Analytics** (Unity Analytics or GameAnalytics)
6. **Push Notifications** (weekly reset alerts)
7. **Social Features** (friend leaderboards)

## Support

- Unity Docs: https://docs.unity3d.com
- Supabase C# Client: https://github.com/supabase-community/supabase-csharp
- Discord: [Your Discord server]

---

Built with Unity 2022.3 LTS
