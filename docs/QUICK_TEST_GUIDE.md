# ⚡ Quick Test Guide - 30 Minutes to Playable

**Goal:** Get from "untested code" to "playable prototype" as fast as possible

---

## 📋 Prerequisites

- Unity 2022.3 LTS or newer installed
- Git (to clone backend repo)
- Node.js 18+ (for Supabase CLI)
- Supabase account (free tier works)

---

## 🚀 30-Minute Sprint

### Phase 1: Unity Setup (10 minutes)

#### Step 1.1: Open Project (2 min)
```bash
# Open Unity Hub
# Click "Add" → Browse to: /home/sam/Projects/game-app/client/
# Select Unity 2022.3+ LTS
# Wait for import (2-3 min)
```

#### Step 1.2: Install TextMeshPro (1 min)
```
Window > Package Manager
Search: "TextMesh Pro"
Click: Install
Close Package Manager
```

#### Step 1.3: Check for Errors (1 min)
```
Open Console: Ctrl+Shift+C (Windows) or Cmd+Shift+C (Mac)
Look for RED errors
```

**Expected:** 0 errors
**If errors exist:** Screenshot and send to me

#### Step 1.4: Create Minimal Scene (6 min)
```
1. File > New Scene
2. Delete "Main Camera" and "Directional Light"
3. GameObject > Create Empty → Name: "_Managers"
4. Add components to _Managers:
   - GameStateMachine
   - SessionManager
   - SupabaseClient
   - InputHandler
   - MissionsManager
   - PracticeMode
   - AnalyticsManager
   - AnalyticsIntegration

5. GameObject > UI > Canvas → Name: "Canvas"
   - Add Canvas Scaler component (if not present)
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1080 x 1920

6. Right-click Canvas > Create Empty → Name: "HomeScreen"
   - Add Component: HomeScreen script
   - Don't worry about references yet

7. Right-click Canvas > Create Empty → Name: "GameScreen"
   - Add Component: GameScreen script
   - Add Component: PatternExecutor script

8. Save Scene as: "MainScene.unity"
```

---

### Phase 2: Backend Setup (5 minutes)

#### Step 2.1: Initialize Supabase (3 min)
```bash
cd server
npx supabase init
# Creates local config
```

#### Step 2.2: Deploy Schema (1 min)
```bash
npx supabase db push
# Deploys database tables
```

#### Step 2.3: Deploy Functions (1 min)
```bash
npx supabase functions deploy start-run
npx supabase functions deploy submit-run
npx supabase functions deploy get-leaderboard
```

#### Step 2.4: Get Credentials
```bash
npx supabase status
# Copy these values:
# - API URL
# - anon key
```

---

### Phase 3: Configure Unity (5 minutes)

#### Step 3.1: Set Supabase Credentials (2 min)
```
1. In Hierarchy, click "_Managers"
2. Find "SupabaseClient" component in Inspector
3. Paste:
   - Supabase Url: <your-api-url>
   - Supabase Anon Key: <your-anon-key>
```

#### Step 3.2: Minimal HomeScreen Setup (3 min)
```
1. Click "HomeScreen" in Hierarchy
2. In Inspector, find "HomeScreen" component
3. Create minimal buttons:
   a. Right-click HomeScreen > UI > Button → Name: "StartButton"
   b. Drag "StartButton" to "Start Button" field in Inspector
```

---

### Phase 4: First Test (10 minutes)

#### Step 4.1: Press Play (1 min)
```
Click ► Play button at top of Unity Editor
```

#### Step 4.2: Check Console (2 min)
Look for these messages:
```
[SessionManager] Authenticating...
[SessionManager] Authentication successful: <user-id>
```

**If you see errors:** Screenshot and send to me

#### Step 4.3: Test Start Button (2 min)
```
1. Click "StartButton" in Game view
2. Should see:
   [SessionManager] Starting new run...
   [SessionManager] Run started: Week X, Seed Y
   [GameStateMachine] Initialized run with seed=...
   [GameStateMachine] State: Idle → PreRun
   [GameStateMachine] State: PreRun → PlayingFloor
```

#### Step 4.4: Test Input (5 min)
```
1. When GameScreen shows, tap/click screen
2. Should see in Console:
   [PatternExecutor] Started pattern: ...
   [PatternExecutor] Pattern SUCCESS/FAILED: ...
   [GameStateMachine] Pattern completed
```

---

## ✅ Success Criteria

After 30 minutes, you should have:

1. ✅ Unity project opens without errors
2. ✅ Console shows authentication success
3. ✅ Click Start button → Run starts
4. ✅ GameScreen appears
5. ✅ Can tap and see pattern detection logs

---

## 🐛 Troubleshooting

### Error: "SupabaseClient not found"
**Fix:** Make sure SupabaseClient component is on _Managers GameObject

### Error: "The type or namespace name 'TMPro' could not be found"
**Fix:** Install TextMeshPro package (Step 1.2)

### Error: "NullReferenceException: Object reference not set"
**Likely cause:** Missing Inspector reference
**Fix:** Check which component has null reference, assign in Inspector

### No authentication message
**Check:** Supabase credentials are set correctly
**Check:** Internet connection working
**Check:** Supabase project is active (not paused)

### Button click does nothing
**Check:** Button has "On Click ()" event
**Check:** Button references HomeScreen.OnStartClicked

### Pattern doesn't appear
**Check:** GameScreen and PatternExecutor are on same GameObject or properly referenced
**Check:** GameScreen is subscribing to events in Start()

---

## 📊 What Each Phase Tests

| Phase | Tests | Pass/Fail |
|-------|-------|-----------|
| **1. Unity** | Code compiles, scene loads | ⏳ |
| **2. Backend** | Supabase functions deploy | ⏳ |
| **3. Config** | Credentials are valid | ⏳ |
| **4. Test** | Full gameplay loop works | ⏳ |

---

## 🎯 After Success

Once basic test works, you can:

### 1. Add More UI (10 min)
- Create proper button UI with TextMeshProUGUI
- Add pattern icon images
- Style with colors and fonts

### 2. Generate Placeholder Assets (5 min)
```
Tools > Tower Climb > Generate Placeholder Sprites
Tools > Tower Climb > Generate Placeholder Audio
```

### 3. Wire Up Other Screens (20 min)
- Create Leaderboard screen GameObject
- Create Missions screen GameObject
- Create Practice screen GameObject
- Create Shop screen GameObject
- Create Settings screen GameObject
- Assign references in HomeScreen

### 4. Build to Mobile (15 min)
```
File > Build Settings
Platform: Android or iOS
Build and Run
Test on real device
```

---

## 📞 If You Get Stuck

### Option A: Send Me Details
```
1. Screenshot of Console errors
2. Screenshot of Inspector (which GameObject?)
3. What step you're on
4. What you expected vs what happened
```

### Option B: Share Unity Log
```
Unity > Preferences > Logs
Find: Editor.log
Send last 50 lines
```

### Option C: Test Individual Systems
```
# Test 1: Authentication only
- Press Play
- Check for auth success message
- If fails: Backend issue

# Test 2: Scene setup only
- Don't press Play
- Check Inspector for missing references
- If many missing: Scene setup issue

# Test 3: Script compilation only
- Check Console for red errors
- If errors: Code issue (send to me)
```

---

## 💡 Pro Tips

1. **Save Often:** Ctrl+S (scene) and Ctrl+Shift+S (project)
2. **Console Filters:** Use dropdown to show "Errors Only"
3. **Pause Mode:** Click || button to pause when debugging
4. **Step Through:** Click single frame ► button to advance frame-by-frame
5. **Inspector Lock:** Click lock icon to keep Inspector focused on one object

---

## 🎮 Expected First Experience

**What you'll see:**
1. Press Play → HomeScreen appears (may be blank if no UI)
2. Click Start → Loading happens (1-2 seconds)
3. GameScreen appears (may be blank if no UI)
4. See console messages about patterns
5. Click/tap → See "SUCCESS" or "FAILED" in console
6. After failure → Results screen (may be blank)

**What you WON'T see yet:**
- Pretty UI (need to add buttons/text)
- Pattern icons (need to generate assets)
- Sound effects (need to generate audio)
- Smooth animations (need to add)

**But the LOGIC will work!**

---

## ✅ Done!

If you complete this guide successfully, you have:
- ✅ Proven code compiles
- ✅ Proven backend works
- ✅ Proven gameplay loop works
- ✅ Foundation for polish

**Next:** Add UI polish, test all screens, build to mobile, replace placeholders with real assets.

**Total Time Spent:** 30 minutes
**Result:** Working prototype

---

## 🎯 Your Mission

Complete this guide and report back:
- "✅ All 4 phases passed - prototype works!"
- "❌ Stuck at Phase X, Step Y - here's the error: [screenshot]"

Either way, we'll know exactly what to fix next!
