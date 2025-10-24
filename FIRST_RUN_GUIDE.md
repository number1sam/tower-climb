# 🚀 Tower Climb - First Run Guide

**Get your game running in 15 minutes!**

This guide will take you from zero to a playable game in Unity. Follow these steps in order.

---

## Prerequisites (5 minutes)

### ✅ Install Unity

1. Download **Unity Hub**: https://unity.com/download
2. Install **Unity 2022.3 LTS** (or newer 2022 LTS version)
3. During install, select:
   - ☑️ Android Build Support (for mobile)
   - ☑️ iOS Build Support (for iPhone/iPad)
   - ☑️ Visual Studio (for code editing)

### ✅ Install Supabase CLI (for backend)

```bash
# macOS/Linux:
brew install supabase/tap/supabase

# Windows:
scoop bucket add supabase https://github.com/supabase/scoop-bucket.git
scoop install supabase

# Or use npm:
npm install -g supabase
```

Verify:
```bash
supabase --version
```

---

## Step 1: Deploy Backend (5 minutes)

### 1.1 Create Supabase Project

1. Go to https://supabase.com/dashboard
2. Click "New Project"
3. Enter:
   - Name: `tower-climb`
   - Database Password: (save this!)
   - Region: (choose closest)
4. Wait ~2 minutes for provisioning

### 1.2 Get Credentials

1. Go to **Settings → API**
2. Copy:
   - **Project URL** (e.g., `https://abcdefgh.supabase.co`)
   - **anon public** key

**Save these!** You'll need them in Step 3.

### 1.3 Deploy Backend

```bash
cd game-app/server

# Link to your Supabase project
supabase link --project-ref YOUR_PROJECT_REF
# (Get PROJECT_REF from your Supabase URL: https://YOUR_PROJECT_REF.supabase.co)

# Deploy everything
./scripts/deploy.sh
```

Expected output:
```
✅ Applying migration 20250101000000_initial_schema.sql...
✅ Applying migration 20250102000000_remote_config.sql...
✅ Deploying start-run...
✅ Deploying submit-run...
✅ Deploying get-leaderboard...
✅ Deploying weekly-seed-reset...
✅ All tests passed!
```

### 1.4 Enable Anonymous Auth

1. Go to **Supabase Dashboard → Authentication → Providers**
2. Find "Anonymous sign-ins"
3. Toggle **ON**
4. Click **Save**

### 1.5 Create First Season

1. Go to **Supabase Dashboard → SQL Editor**
2. Click **New Query**
3. Paste:
   ```sql
   insert into season (week_id, seed, starts_at, ends_at)
   values (
     extract(year from now())::int * 100 + extract(week from now())::int,
     floor(random() * 9223372036854775807)::bigint,
     now(),
     now() + interval '7 days'
   );
   ```
4. Click **Run**

**✅ Backend is now live!**

---

## Step 2: Open Unity Project (2 minutes)

### 2.1 Add Project to Unity Hub

1. Open **Unity Hub**
2. Click **Add** (top right)
3. Navigate to: `game-app/client`
4. Click **Select Folder**

### 2.2 Open Project

1. In Unity Hub, click on **Tower Climb**
2. Wait for Unity to open (~1-2 minutes)
3. First open will import packages

**Expected:** Unity Editor opens with empty scene

---

## Step 3: Configure Supabase (1 minute)

### 3.1 Set Credentials

1. In **Project** panel (bottom), navigate to:
   ```
   Assets → Scripts → API
   ```

2. Double-click `SupabaseClient.cs`

3. Find these lines:
   ```csharp
   [SerializeField] private string supabaseUrl = "https://your-project.supabase.co";
   [SerializeField] private string supabaseAnonKey = "your-anon-key";
   ```

4. Replace with YOUR credentials from Step 1.2

5. Save file (**Ctrl+S** / **Cmd+S**)

**⚠️ CRITICAL:** Without this, the game won't connect to your backend!

---

## Step 4: Quick Setup Scene (5 minutes)

We'll create a minimal scene to test the game.

### 4.1 Create Scene

1. **File → New Scene → 2D (Core)**
2. **File → Save As**
   - Name: `MainScene`
   - Location: `Assets/Scenes/`

### 4.2 Create Managers

In **Hierarchy**, create these GameObjects:

#### A. GameStateMachine

```
Right-click Hierarchy → Create Empty
Name: GameStateMachine
Add Component: GameStateMachine.cs (search in Add Component)
```

#### B. SupabaseClient

```
Create Empty → Name: SupabaseClient
Add Component: SupabaseClient.cs
```

**Inspector:**
- Supabase Url: `https://YOUR_PROJECT_REF.supabase.co`
- Supabase Anon Key: `YOUR_ANON_KEY`

#### C. SessionManager

```
Create Empty → Name: SessionManager
Add Component: SessionManager.cs
```

### 4.3 Create Simple UI

```
Right-click Hierarchy → UI → Canvas
Name: UICanvas
```

**Canvas Scaler:**
- UI Scale Mode: **Scale With Screen Size**
- Reference Resolution: **1080 x 1920**

#### Add Play Button

```
Right-click UICanvas → UI → Button - TextMeshPro
Name: PlayButton
```

If prompted "Import TMP Essentials", click **Import**.

**Text:** Change to "PLAY"

#### Add Script to PlayButton

1. With **PlayButton** selected, click **Add Component**
2. Search for `HomeScreen`
3. Click **HomeScreen.cs**

**Wire References:**
- Drag **PlayButton** to → Play Button field
- Drag **SessionManager** (from Hierarchy) to → Session Manager field (if needed)

### 4.4 Test Connection

1. Press **Play ▶️** (top center)
2. Click the **PLAY** button
3. Check **Console** (Window → General → Console)

**Expected:**
```
[SessionManager] Authenticating...
[SessionManager] Authentication successful: <user_id>
[SessionManager] Starting new run...
[GameStateMachine] Initialized run with seed=<seed>, week=<week>
```

**✅ If you see this, IT WORKS!**

---

## Step 5: Run Tests (1 minute)

Verify pattern generation:

1. **Window → General → Test Runner**
2. Select **PlayMode** tab
3. Click **Run All**

**Expected:**
```
✅ 10 passed in <1s
```

**If any fail:** Check console for errors. Most common issue is pattern generator mismatch.

---

## Step 6: Full Scene Setup (Optional - 30 minutes)

For the complete game experience, follow:

📘 **`client/SCENE_SETUP.md`** - Full step-by-step UI setup

This includes:
- Complete UI (Home, Game, Results screens)
- Audio system
- VFX system
- Input handling

---

## Quick Troubleshooting

### "SupabaseClient not found"

**Fix:**
- Ensure GameObject named `SupabaseClient` exists in Hierarchy
- Check it has `SupabaseClient.cs` component

### "Authentication failed"

**Fix:**
- Verify Supabase URL and anon key are correct
- Check backend deployed: `supabase functions list`
- Verify anonymous auth enabled in Supabase Dashboard

### "No active season"

**Fix:**
- Run the SQL query from Step 1.5 again
- Verify in Supabase Dashboard → Database → season table

### Pattern tests failing

**Fix:**
- Check `PatternGenerator.cs` hasn't been modified
- Verify `SeededRandom.cs` PRNG is correct
- Compare with server version: `server/shared/utils/pattern-generator.ts`

### Play button does nothing

**Fix:**
- Check `HomeScreen.cs` is attached to a UI element
- Verify `SessionManager` exists in scene
- Check Console for errors

---

## Next Steps

### ✅ You have a working game!

**What's working:**
- Backend (database, API, leaderboard)
- Pattern generation (deterministic)
- Basic Unity scene
- Authentication

**To complete the game:**

1. **Add UI Screens** (30 min)
   - Follow `client/SCENE_SETUP.md`
   - Create HomeScreen, GameScreen, ResultsScreen

2. **Add Input Handling** (included in scene setup)
   - PatternExecutor displays challenges
   - InputHandler detects taps/swipes

3. **Add Audio** (5 min)
   - Assign placeholder audio clips to AudioManager
   - Test SFX/music playback

4. **Add VFX** (5 min)
   - Configure particle systems
   - Test screen shake/flash

5. **Polish** (ongoing)
   - Improve visuals
   - Add animations
   - Create custom assets

### 📱 Test on Device

**Android:**
```bash
File → Build Settings → Android
Click "Switch Platform" (if needed)
Click "Build and Run"
```

**iOS:**
```bash
File → Build Settings → iOS
Click "Switch Platform"
Click "Build"
# Then open Xcode project and build to device
```

---

## Verify Everything Works

### ✅ Backend Checklist

- [ ] Supabase project created
- [ ] Backend deployed (`./scripts/deploy.sh`)
- [ ] Anonymous auth enabled
- [ ] Season created (SQL query)
- [ ] Tests pass (`deno test`)

### ✅ Unity Checklist

- [ ] Project opens in Unity
- [ ] Credentials configured (SupabaseClient.cs)
- [ ] Managers in scene (GameStateMachine, SupabaseClient, SessionManager)
- [ ] Play button works
- [ ] Console shows authentication success
- [ ] Tests pass (Window → Test Runner)

---

## Full Documentation

📘 **Backend:**
- `server/README.md` - Full backend docs
- `server/QUICKSTART.md` - 10-minute backend setup
- `BACKEND_SUMMARY.md` - Architecture overview

📘 **Client:**
- `client/README.md` - Full Unity guide
- `client/SCENE_SETUP.md` - Complete scene setup
- `CLIENT_SUMMARY.md` - Architecture overview

📘 **Project:**
- `PROJECT_COMPLETE.md` - Full project overview

---

## Getting Help

**Common Issues:**

1. **"Can't connect to backend"**
   → Check Supabase URL/key
   → Verify functions deployed: `supabase functions list`

2. **"Pattern mismatch cheat flag"**
   → Verify client PRNG matches server
   → Run tests on both: `deno test` and Unity Test Runner

3. **"UI not showing"**
   → Check Canvas exists
   → Verify screens are children of Canvas
   → Check screen is ENABLED

4. **Need more help?**
   → Check console logs (Unity + Supabase)
   → Review documentation in `/docs` folder
   → Check GitHub issues (if applicable)

---

## Success! 🎉

You now have:
- ✅ Backend running on Supabase
- ✅ Unity project configured
- ✅ Basic game flow working
- ✅ Tests passing

**Time to build your game!** 🚀

Next: Follow `client/SCENE_SETUP.md` for full game UI setup.
