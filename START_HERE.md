# 🎮 START HERE - Test Your Game in 30 Minutes

**Current Status:** ✅ All code complete and bug-fixed
**Your Mission:** Get it running in Unity

---

## ⚡ Quick Start (30 Minutes)

### Step 1: Open Unity (5 min)
```bash
# Open Unity Hub
# Click "Add"
# Browse to: /home/sam/Projects/game-app/client/
# Select Unity 2022.3+ LTS
# Wait for import (2-3 min)
```

---

### Step 2: Install TextMeshPro (1 min)
```
Unity Editor → Window → Package Manager
Search: "TextMesh Pro"
Click: Install
```

---

### Step 3: Check Console (1 min)
```
Ctrl+Shift+C (Windows) or Cmd+Shift+C (Mac)
```

**Expected:** 0 errors (might have warnings - that's OK)
**If errors:** Screenshot and send to me

---

### Step 4: Create Minimal Test Scene (10 min)

#### 4A: Create Managers GameObject
```
1. GameObject → Create Empty → Name: "_Managers"
2. Add these components (click Add Component, type name):
   - GameStateMachine
   - SessionManager
   - SupabaseClient
   - InputHandler
   - MissionsManager
   - PracticeMode
   - AnalyticsManager
   - AnalyticsIntegration
```

#### 4B: Create Canvas
```
1. GameObject → UI → Canvas
2. Select Canvas in Hierarchy
3. Inspector → Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: X=1080, Y=1920
```

#### 4C: Create HomeScreen
```
1. Right-click Canvas → Create Empty → Name: "HomeScreen"
2. Select HomeScreen
3. Add Component → HomeScreen (script)
4. Right-click HomeScreen → UI → Button → Name: "StartButton"
5. Drag StartButton to HomeScreen's "Start Button" field
```

#### 4D: Create GameScreen
```
1. Right-click Canvas → Create Empty → Name: "GameScreen"
2. Add Component → GameScreen
3. Add Component → PatternExecutor
```

#### 4E: Save Scene
```
File → Save As → "MainScene.unity"
```

---

### Step 5: Configure Supabase (3 min)

```bash
# Terminal:
cd /home/sam/Projects/game-app/server
npx supabase status

# Copy these values:
# - API URL
# - anon key
```

**In Unity:**
```
1. Select "_Managers" in Hierarchy
2. Find "Supabase Client" component in Inspector
3. Paste:
   - Supabase Url: <your-api-url>
   - Supabase Anon Key: <your-anon-key>
```

---

### Step 6: PRESS PLAY! (10 min)

```
Click ► Play button at top of Unity Editor
```

#### What You Should See:

**Console Output:**
```
[SessionManager] Authenticating...
[SessionManager] Authentication successful: <user-id>
```

**Game View:**
- HomeScreen visible
- Start button clickable

#### Click Start Button:

**Console should show:**
```
[SessionManager] Starting new run...
[SessionManager] Run started: Week X, Seed Y
[GameStateMachine] Initialized run with seed=...
[GameStateMachine] State: Idle → PreRun
[GameStateMachine] State: PreRun → PlayingFloor
[PatternExecutor] Started pattern: ...
```

#### Click/Tap Screen:

**Console should show:**
```
[PatternExecutor] Pattern SUCCESS: 245ms, accuracy=0.95
[GameStateMachine] Pattern completed
```

---

## ✅ Success Criteria

After 30 minutes, you should have:

1. ✅ Unity opens project without errors
2. ✅ Press Play → See authentication success
3. ✅ Click Start → Run starts
4. ✅ See pattern in console
5. ✅ Click screen → Pattern detects input

**If ALL 5 work:** 🎉 **GAME WORKS!** Continue to full setup
**If ANY fail:** Send me screenshot of error

---

## 🐛 Common Issues & Fixes

### "TextMeshPro not found"
```
Window → Package Manager → Search "TextMesh Pro" → Install
```

### "SupabaseClient not found"
```
Check that _Managers GameObject has SupabaseClient component
```

### "No authentication message"
```
Check Supabase credentials are pasted correctly
Check internet connection
```

### "Button click does nothing"
```
Select StartButton in Hierarchy
Inspector → Button → On Click ()
Should point to HomeScreen.OnStartClicked
If empty, drag HomeScreen to it, select OnStartClicked
```

---

## 📊 Progress Tracker

- [ ] Unity project opens
- [ ] TextMeshPro installed
- [ ] 0 compilation errors
- [ ] Scene created with managers
- [ ] Supabase credentials set
- [ ] Press Play → Auth success
- [ ] Click Start → Run starts
- [ ] Pattern appears (console)
- [ ] Click detects input

**Score: __ / 9**

---

## 🎯 After Success

Once basic test works:

### Next Steps (2 hours):
1. **Build Full Scene** - Follow `docs/UNITY_SCENE_SETUP.md`
2. **Generate Assets** - Run placeholder generators
3. **Wire All Screens** - Connect all 8 UI screens
4. **Test All Features** - Practice, Shop, Missions, etc.
5. **Build to Device** - Test mobile input

### Documentation:
- `docs/QUICK_TEST_GUIDE.md` - Detailed 30-min guide
- `docs/UNITY_SCENE_SETUP.md` - Complete scene setup
- `docs/BUG_FIXES_APPLIED.md` - All fixes explained
- `docs/SYSTEM_INTEGRATION_CHECK.md` - How systems work together

---

## 💬 Communication

### If It Works:
Reply: "✅ Works! [X/9 steps passed]"

### If It Breaks:
Reply with:
1. Which step failed
2. Screenshot of Console errors
3. Screenshot of Inspector (which GameObject selected)

---

## 🚀 YOU'RE READY!

**Estimated Time:** 30 minutes
**Difficulty:** Easy (just follow steps)
**Result:** Working game prototype

**LET'S GO!** 🎮
