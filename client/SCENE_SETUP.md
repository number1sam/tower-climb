# Tower Climb - Unity Scene Setup Guide

Complete step-by-step guide to set up your Unity scene from scratch.

---

## Prerequisites

- Unity 2022.3 LTS installed
- Project opened in Unity Hub
- All scripts in `Assets/Scripts/` folder
- TextMeshPro package imported

---

## Scene Setup Checklist

- [ ] Create Main Camera
- [ ] Create Persistent Managers (DontDestroyOnLoad)
- [ ] Create UI Canvas
- [ ] Create HomeScreen UI
- [ ] Create GameScreen UI
- [ ] Create ResultsScreen UI
- [ ] Wire up all references
- [ ] Configure Supabase credentials
- [ ] Test in Play mode

---

## Part 1: Create New Scene

1. **Create Scene:**
   - File → New Scene → 2D (Core)
   - Save as: `Assets/Scenes/MainScene.unity`

2. **Main Camera:**
   - Already exists in 2D template
   - Set **Background:** Solid Color (dark blue/black)
   - Set **Size:** 5
   - Leave **Projection:** Orthographic

---

## Part 2: Persistent Managers

Create these GameObjects in the Hierarchy (right-click → Create Empty):

### 2.1 GameStateMachine

```
GameObject: GameStateMachine
  - Add Component: GameStateMachine.cs
  - Tag: GameController (optional)
```

**Inspector Settings:**
- Current State: Idle
- Current Seed: 0
- Current Floor: 1
- Week Id: 0
- (Leave other fields empty - will be populated at runtime)

### 2.2 SupabaseClient

```
GameObject: SupabaseClient
  - Add Component: SupabaseClient.cs
```

**⚠️ CRITICAL - Inspector Settings:**
```
Supabase Url: https://YOUR_PROJECT_REF.supabase.co
Supabase Anon Key: YOUR_ANON_KEY_HERE
```

**Get these from:** Supabase Dashboard → Settings → API

### 2.3 SessionManager

```
GameObject: SessionManager
  - Add Component: SessionManager.cs
```

**Inspector Settings:**
- (All references will auto-find via `Instance` pattern)

### 2.4 AudioManager

```
GameObject: AudioManager
  - Add Component: AudioManager.cs
  - Add Component: Audio Source (for SFX)
  - Add Component: Audio Source (for Music)
```

**Inspector Settings:**
```
SFX Source: [Drag first AudioSource]
Music Source: [Drag second AudioSource]

Pattern SFX:
  Tap Sound: [Assign AudioClip]
  Swipe Sound: [Assign AudioClip]
  Hold Sound: [Assign AudioClip]
  Rhythm Sound: [Assign AudioClip]
  Tilt Sound: [Assign AudioClip]

Feedback SFX:
  Perfect Sound: [Assign AudioClip]
  Good Sound: [Assign AudioClip]
  Miss Sound: [Assign AudioClip]
  Fail Sound: [Assign AudioClip]

UI SFX:
  Button Click Sound: [Assign AudioClip]
  Unlock Sound: [Assign AudioClip]

Music:
  Menu Music: [Assign AudioClip]
  Gameplay Music: [Assign AudioClip]

Settings:
  SFX Volume: 0.8
  Music Volume: 0.5
```

**Music Source Settings:**
- Loop: ☑️ Enabled
- Play On Awake: ☐ Disabled

### 2.5 VFXManager

```
GameObject: VFXManager
  - Add Component: VFXManager.cs
```

**Inspector Settings:**
```
Particle Systems:
  Perfect Particles: [Create ParticleSystem GameObject, assign]
  Good Particles: [Create ParticleSystem GameObject, assign]
  Miss Particles: [Create ParticleSystem GameObject, assign]
  Swipe Trail Particles: [Create ParticleSystem GameObject, assign]

Screen Shake:
  Main Camera: [Drag Main Camera from Hierarchy]
  Shake Intensity: 0.1
  Shake Duration: 0.2

Flash Effect:
  Flash Overlay: [Create UI Image, assign its CanvasGroup]
  Perfect Flash Color: (0, 255, 128, 76) - Green with alpha
  Miss Flash Color: (255, 0, 0, 76) - Red with alpha
```

**Creating Particle Systems:**

1. Right-click VFXManager → Effects → Particle System
2. Name it `PerfectParticles`
3. Configure:
   - **Emission:** Rate over Time = 50
   - **Shape:** Sphere, Radius = 1
   - **Color over Lifetime:** Green → Transparent
   - **Size over Lifetime:** 1 → 0
   - Duration: 0.5s
   - Looping: OFF
   - Play On Awake: OFF

4. Duplicate for `GoodParticles`, `MissParticles` (change colors)

---

## Part 3: UI Canvas

### 3.1 Create Canvas

```
Right-click Hierarchy → UI → Canvas
Name: UICanvas
```

**Canvas Component:**
- Render Mode: Screen Space - Overlay

**Canvas Scaler:**
```
UI Scale Mode: Scale With Screen Size
Reference Resolution: 1080 x 1920 (portrait)
Screen Match Mode: Match Width Or Height
Match: 0.5
```

**Add Component:** `Graphic Raycaster`

### 3.2 Event System

Should auto-create. If not:
```
Right-click Hierarchy → UI → Event System
```

---

## Part 4: HomeScreen UI

Create under UICanvas:

```
UICanvas
  └─ HomeScreen (Panel)
       ├─ Background (Image)
       ├─ Title (TextMeshPro - Text)
       ├─ PlayButton (Button - TextMeshPro)
       ├─ PersonalBestText (TextMeshPro - Text)
       ├─ WeekInfoText (TextMeshPro - Text)
       ├─ LeaderboardButton (Button - TextMeshPro)
       ├─ MissionsButton (Button - TextMeshPro)
       └─ LoadingPanel (Panel)
            ├─ LoadingText (TextMeshPro - Text)
            └─ Spinner (Image, rotating)
```

### Detailed Setup:

#### HomeScreen (Panel)
```
Add Component: HomeScreen.cs

RectTransform:
  Anchors: Stretch (all corners)
  Left/Right/Top/Bottom: 0

Inspector (HomeScreen.cs):
  Play Button: [Drag PlayButton]
  Leaderboard Button: [Drag LeaderboardButton]
  Missions Button: [Drag MissionsButton]
  Personal Best Text: [Drag PersonalBestText]
  Week Info Text: [Drag WeekInfoText]
  Loading Panel: [Drag LoadingPanel]
```

#### Background
```
Component: Image
Color: Dark gradient (e.g., #1a1a2e)
Material: None
Raycast Target: ON
```

#### Title
```
Component: TextMeshPro - Text
Text: "TOWER CLIMB"
Font Size: 80
Alignment: Center, Top
Color: White
```

#### PlayButton
```
Component: Button - TextMeshPro
Text: "PLAY"
Font Size: 48
Button Colors: Normal (green), Highlighted (lighter green), Pressed (darker green)

RectTransform:
  Width: 400
  Height: 120
  Position: Center, slightly below middle
```

#### PersonalBestText
```
Component: TextMeshPro - Text
Text: "Personal Best: Floor 0"
Font Size: 32
Alignment: Center
Color: Yellow/Gold
Position: Below Play button
```

#### WeekInfoText
```
Component: TextMeshPro - Text
Text: "Week #1"
Font Size: 24
Alignment: Center, Top
Color: Light gray
Position: Top of screen
```

#### LoadingPanel
```
Component: Panel (Image)
Color: Black with alpha 128 (semi-transparent)
Enabled: OFF (will be toggled by script)

Children:
  LoadingText: "Loading..."
  Spinner: Rotating image (optional)
```

---

## Part 5: GameScreen UI

Create under UICanvas:

```
UICanvas
  └─ GameScreen (Panel)
       ├─ Background (Image)
       ├─ FloorText (TextMeshPro - Text)
       ├─ PatternDisplay (Panel)
       │    ├─ PatternIcon (Image)
       │    ├─ PatternText (TextMeshPro - Text)
       │    └─ TimerBar (Image - Filled)
       ├─ FeedbackPanel
       │    ├─ PerfectFeedback (TextMeshPro - Text) "PERFECT!"
       │    ├─ GoodFeedback (TextMeshPro - Text) "GOOD!"
       │    └─ MissFeedback (TextMeshPro - Text) "MISS"
       └─ InputHandler (Empty GameObject)
```

### Detailed Setup:

#### GameScreen (Panel)
```
Add Component: PatternExecutor.cs

RectTransform:
  Anchors: Stretch
  Left/Right/Top/Bottom: 0

Initially: DISABLED (enable when run starts)

Inspector (PatternExecutor.cs):
  Pattern Icon: [Drag PatternIcon]
  Pattern Text: [Drag PatternText]
  Timer Bar: [Drag TimerBar]
  Floor Text: [Drag FloorText]
  Perfect Feedback: [Drag PerfectFeedback GameObject]
  Good Feedback: [Drag GoodFeedback GameObject]
  Miss Feedback: [Drag MissFeedback GameObject]

Pattern Icons: (Create sprites in Assets/Sprites/)
  Tap Icon: [Assign Sprite]
  Swipe Left Icon: [Assign Sprite]
  Swipe Right Icon: [Assign Sprite]
  Swipe Up Icon: [Assign Sprite]
  Swipe Down Icon: [Assign Sprite]
  Hold Icon: [Assign Sprite]
  Rhythm Icon: [Assign Sprite]
  Tilt Icon: [Assign Sprite]
  Double Tap Icon: [Assign Sprite]
```

#### FloorText
```
Component: TextMeshPro - Text
Text: "Floor 1"
Font Size: 48
Alignment: Center, Top
Color: White
Position: Top of screen
```

#### PatternDisplay (Panel)
```
Position: Center of screen
Size: 600 x 600
Background: Semi-transparent dark
```

#### PatternIcon
```
Component: Image
Sprite: [Will be set by script]
Size: 400 x 400
Position: Center of PatternDisplay
Preserve Aspect: ON
```

#### PatternText
```
Component: TextMeshPro - Text
Text: "TAP"
Font Size: 64
Alignment: Center
Color: White
Position: Below PatternIcon
```

#### TimerBar
```
Component: Image
Sprite: UI Sprite (white square)
Color: Green → Yellow → Red (will change based on time)
Image Type: Filled
Fill Method: Horizontal
Fill Origin: Left
Fill Amount: 1.0 (will decrease)

RectTransform:
  Width: 800
  Height: 40
  Position: Bottom of screen
```

#### FeedbackPanel
All children initially DISABLED (toggled by PatternExecutor)

**PerfectFeedback:**
```
Text: "PERFECT!"
Font Size: 120
Color: Green (bright)
Alignment: Center
Font Style: Bold
```

**GoodFeedback:**
```
Text: "GOOD!"
Font Size: 100
Color: Yellow
Alignment: Center
```

**MissFeedback:**
```
Text: "MISS"
Font Size: 100
Color: Red
Alignment: Center
```

#### InputHandler (Empty GameObject)
```
Add Component: InputHandler.cs

Inspector (InputHandler.cs):
  Min Swipe Distance: 50
  Max Swipe Time: 0.5
  Min Hold Time: 0.3
  Double Tap Window: 0.3
  Rhythm Tap Window: 0.5
  Rhythm Sequence Window: 2.0
  Tilt Threshold: 0.3
```

---

## Part 6: ResultsScreen UI

Create under UICanvas:

```
UICanvas
  └─ ResultsScreen (Panel)
       ├─ Background (Image)
       ├─ Title (TextMeshPro - Text) "RUN COMPLETE"
       ├─ StatsPanel (Panel)
       │    ├─ FloorsReachedText (TextMeshPro - Text)
       │    ├─ RuntimeText (TextMeshPro - Text)
       │    ├─ AvgReactionText (TextMeshPro - Text)
       │    ├─ PerfectRateText (TextMeshPro - Text)
       │    └─ ComparisonText (TextMeshPro - Text)
       ├─ TipText (TextMeshPro - Text)
       ├─ NewBestIndicator (Image) "NEW BEST!"
       ├─ ButtonPanel
       │    ├─ RetryButton (Button - TextMeshPro)
       │    ├─ HomeButton (Button - TextMeshPro)
       │    └─ ShareButton (Button - TextMeshPro)
       └─ SubmittingPanel (Panel)
            └─ Text: "Submitting..."
```

### Detailed Setup:

#### ResultsScreen (Panel)
```
Add Component: ResultsScreen.cs

RectTransform:
  Anchors: Stretch
  Left/Right/Top/Bottom: 0

Initially: DISABLED (enable when run ends)

Inspector (ResultsScreen.cs):
  Floors Reached Text: [Drag FloorsReachedText]
  Runtime Text: [Drag RuntimeText]
  Avg Reaction Text: [Drag AvgReactionText]
  Perfect Rate Text: [Drag PerfectRateText]
  Comparison Text: [Drag ComparisonText]
  Tip Text: [Drag TipText]
  Retry Button: [Drag RetryButton]
  Home Button: [Drag HomeButton]
  Share Button: [Drag ShareButton]
  New Best Indicator: [Drag NewBestIndicator]
  Submitting Panel: [Drag SubmittingPanel]
```

#### StatsPanel
```
Layout: Vertical (use Vertical Layout Group)
Spacing: 20
Padding: 40
Child Alignment: Upper Center
```

#### FloorsReachedText
```
Component: TextMeshPro - Text
Text: "Floor 25"
Font Size: 80
Alignment: Center
Color: White
Font Style: Bold
```

#### RuntimeText
```
Text: "Time: 02:15"
Font Size: 36
Alignment: Center
Color: Light gray
```

#### AvgReactionText
```
Text: "Avg Reaction: 342ms"
Font Size: 32
Alignment: Center
Color: Light gray
```

#### PerfectRateText
```
Text: "Perfect: 78.3%"
Font Size: 32
Alignment: Center
Color: Gold
```

#### ComparisonText
```
Text: "+5 floors from PB!"
Font Size: 40
Alignment: Center
Color: Green (if positive) / Red (if negative)
Font Style: Bold
```

#### TipText
```
Text: "💡 Tip: Practice hold patterns"
Font Size: 28
Alignment: Center
Color: Yellow
Wrapping: Enabled
Max Width: 800
Position: Below stats
```

#### NewBestIndicator
```
Component: Image
Sprite: Star or badge sprite
Color: Gold with glow
Size: 150 x 150
Position: Top-right corner
Initially: DISABLED (shown only for new PB)
```

#### RetryButton
```
Component: Button - TextMeshPro
Text: "RETRY"
Font Size: 42
Colors: Normal (blue), Highlighted (lighter), Pressed (darker)
Size: 350 x 100
```

#### HomeButton
```
Text: "HOME"
Font Size: 42
Colors: Gray theme
Size: 350 x 100
```

#### ShareButton
```
Text: "SHARE"
Font Size: 42
Colors: Green theme
Size: 350 x 100
```

#### SubmittingPanel
```
Component: Panel (Image)
Color: Black with alpha 200 (mostly opaque)
Covers entire screen
Initially: DISABLED (shown during API call)

Child Text: "Submitting run..."
  Font Size: 36
  Alignment: Center
  Color: White
```

---

## Part 7: Flash Overlay (for VFXManager)

Create under UICanvas (at root level, above all screens):

```
UICanvas
  └─ FlashOverlay (Image)
```

**Setup:**
```
Component: Image
Color: White (will be set by script)
Raycast Target: OFF
RectTransform:
  Anchors: Stretch
  Left/Right/Top/Bottom: 0

Add Component: Canvas Group
  Alpha: 0
  Interactable: OFF
  Blocks Raycasts: OFF
```

**Wire to VFXManager:**
- Drag FlashOverlay's CanvasGroup to VFXManager → Flash Overlay

---

## Part 8: Wire Everything Together

### 8.1 Check Manager References

Go through each manager and verify:

**GameStateMachine:**
- Should auto-find itself via `Instance`
- No manual wiring needed

**SupabaseClient:**
- ⚠️ **CRITICAL:** Set `Supabase Url` and `Supabase Anon Key`

**SessionManager:**
- Auto-finds SupabaseClient and GameStateMachine
- No manual wiring needed

**AudioManager:**
- Assign all audio clips
- Wire SFX Source and Music Source

**VFXManager:**
- Wire particle systems
- Wire Main Camera
- Wire Flash Overlay CanvasGroup

### 8.2 Check UI Script References

**HomeScreen.cs:**
- Wire all UI elements (buttons, texts, loading panel)

**PatternExecutor.cs:**
- Wire all UI elements (icon, text, timer, feedback)
- Assign all pattern icon sprites

**ResultsScreen.cs:**
- Wire all UI elements (stats texts, buttons, indicators)

**InputHandler.cs:**
- Set threshold values in Inspector

---

## Part 9: Test in Play Mode

### 9.1 Pre-Flight Check

Before pressing Play:
- [ ] All managers have required components
- [ ] SupabaseClient has valid credentials
- [ ] All UI references wired (no "None" in Inspector)
- [ ] Audio clips assigned (can use placeholders)
- [ ] Particle systems configured
- [ ] HomeScreen is ENABLED
- [ ] GameScreen is DISABLED
- [ ] ResultsScreen is DISABLED

### 9.2 Press Play

**Expected Flow:**
1. HomeScreen appears
2. "Personal Best: Floor 0" displayed
3. Click PLAY button
4. Loading panel shows
5. **Error:** "Not authenticated" (expected - need backend running)

**If backend is running:**
1. Click PLAY
2. Loading panel shows
3. GameScreen appears
4. Pattern displays (e.g., "SWIPE LEFT")
5. Timer bar decreases
6. Click/swipe to test input
7. Feedback shows ("PERFECT!" or "MISS")
8. Next floor or run ends
9. ResultsScreen appears with stats

### 9.3 Debug Checks

Open **Console** (Window → General → Console):

Expected logs:
```
[SessionManager] Authenticating...
[SessionManager] Authentication successful: <user_id>
[SessionManager] Starting new run...
[GameStateMachine] Initialized run with seed=<seed>, week=<week>
[GameStateMachine] Floor 1: Pattern(Tap, None, window=1.50s, speed=1.00)
```

**Common Errors:**

**"SupabaseClient not found!"**
→ Ensure GameObject named `SupabaseClient` exists with component

**"InputHandler not found in scene!"**
→ Add InputHandler component to GameScreen → InputHandler GameObject

**"NullReferenceException: pattern icon"**
→ Wire PatternExecutor → Pattern Icon in Inspector

---

## Part 10: Run Unit Tests

Verify pattern determinism:

1. Window → General → Test Runner
2. Select **PlayMode** tab
3. Click **Run All**

**Expected:** 10/10 tests pass ✅

If tests fail:
- Check console for errors
- Verify PatternGenerator.cs matches server version exactly
- Verify SeededRandom.cs PRNG implementation

---

## Part 11: Build Settings (Optional)

For mobile testing:

**Android:**
```
File → Build Settings
Platform: Android
Switch Platform

Player Settings:
  Company Name: TowerClimb
  Product Name: Tower Climb
  Package Name: com.towerclimb.game
  Version: 1.0.0

  Other Settings:
    Minimum API Level: Android 7.0 (API level 24)
    Target API Level: Automatic (highest installed)
    Scripting Backend: IL2CPP
    Target Architectures: ARM64 ☑️

  Resolution and Presentation:
    Default Orientation: Portrait
    Allowed Orientations: Portrait only
```

**iOS:**
```
File → Build Settings
Platform: iOS
Switch Platform

Player Settings:
  Bundle Identifier: com.towerclimb.game
  Version: 1.0.0
  Build: 1

  Other Settings:
    Target minimum iOS Version: 12.0
    Architecture: ARM64
```

---

## Part 12: Quick Reference

### UI Hierarchy Summary

```
Hierarchy:
  Main Camera
  GameStateMachine
  SupabaseClient
  SessionManager
  AudioManager
    ↳ SFX Source (AudioSource)
    ↳ Music Source (AudioSource)
  VFXManager
    ↳ PerfectParticles (ParticleSystem)
    ↳ GoodParticles (ParticleSystem)
    ↳ MissParticles (ParticleSystem)
    ↳ SwipeTrailParticles (ParticleSystem)
  UICanvas
    ↳ EventSystem
    ↳ FlashOverlay (Image + CanvasGroup)
    ↳ HomeScreen (Panel + HomeScreen.cs) [ENABLED]
    ↳ GameScreen (Panel + PatternExecutor.cs) [DISABLED]
    │   ↳ InputHandler (InputHandler.cs)
    ↳ ResultsScreen (Panel + ResultsScreen.cs) [DISABLED]
```

### Critical Wiring Checklist

- [ ] SupabaseClient: URL + Anon Key set
- [ ] AudioManager: All audio clips assigned
- [ ] VFXManager: Camera + Flash Overlay + Particles assigned
- [ ] HomeScreen: All buttons + texts wired
- [ ] PatternExecutor: All UI + icon sprites wired
- [ ] ResultsScreen: All UI wired
- [ ] InputHandler: Threshold values set

---

## Troubleshooting

**Issue:** "Managers not found"
→ Ensure GameObjects are in Hierarchy (not prefabs)
→ Check they have correct components

**Issue:** UI not showing
→ Check Canvas Render Mode: Screen Space - Overlay
→ Check screen is ENABLED in Hierarchy

**Issue:** Input not working
→ Verify EventSystem exists
→ Check InputHandler component is active
→ Test with mouse first (easier to debug)

**Issue:** Audio not playing
→ Check AudioManager has AudioSource components
→ Verify clips are assigned
→ Check volume > 0
→ Verify AudioListener exists (on Main Camera)

**Issue:** VFX not showing
→ Check particle systems are children of VFXManager
→ Verify "Play On Awake" is OFF
→ Check VFXManager → Main Camera is assigned

---

## Next Steps

1. **Create placeholder assets:**
   - Audio clips (can use free assets from Unity Asset Store)
   - Pattern icon sprites (simple shapes work)
   - Particle effects (Unity's default particles)

2. **Test full flow:**
   - Home → Play → Game → Results → Retry

3. **Deploy backend:**
   - See `server/QUICKSTART.md`
   - Test with real API calls

4. **Polish:**
   - Add animations (DOTween recommended)
   - Improve visuals
   - Add more SFX variety

5. **Build to device:**
   - Test on Android/iOS
   - Verify touch input works
   - Check accelerometer (tilt patterns)

---

**Scene Setup Complete!** 🎉

You now have a fully configured Unity scene ready to play.

Next: Deploy backend and test end-to-end integration.
