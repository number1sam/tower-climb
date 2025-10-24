# 🌐 How to Play Tower Climb in Your Browser

This guide shows you how to build and play the game at **localhost:8000** in your web browser.

---

## 🚀 Quick Start (30-40 minutes)

### Step 1: Open Unity (5 min)

1. **Download Unity Hub** (if not installed):
   - Visit: https://unity.com/download
   - Download and install Unity Hub
   - Install Unity Editor 2022.3 LTS

2. **Open Project:**
   ```
   Unity Hub → Projects → Add
   Browse to: /home/sam/Projects/game-app/client
   Click on project name to open
   ```

3. **Wait for import** (2-3 minutes first time)

---

### Step 2: Build to WebGL (20-30 min)

1. **Switch Platform:**
   ```
   Unity Editor → File → Build Settings
   Click "WebGL" in Platform list
   Click "Switch Platform"
   Wait 5-10 minutes (one-time only)
   ```

2. **Configure Player Settings (Important!):**
   ```
   Still in Build Settings window:
   Click "Player Settings" button

   In Inspector panel:
   → Publishing Settings
   → Compression Format: Disabled (for localhost testing)
   → Decompression Fallback: ✓ Enabled
   ```

3. **Build the Game:**
   ```
   Build Settings → Click "Build and Run"

   Save location:
   /home/sam/Projects/game-app/web-build

   Wait 10-20 minutes (grab coffee!)
   ```

4. **Unity will auto-open browser** → Game loads!

---

### Step 3: Play the Game! 🎮

**If Unity auto-opened browser:**
- Game should be loading at `http://localhost:XXXXX`
- Wait for Unity loader to finish
- Click "Start" button
- Use mouse to tap/swipe patterns

**If browser didn't open:**
```bash
cd /home/sam/Projects/game-app
./serve-web.sh
```

Then visit: **http://localhost:8000**

---

## 🎮 How to Play in Browser

### Controls:
- **Tap:** Click mouse
- **Swipe:** Click and drag
- **Hold:** Click and hold mouse button
- **Double Tap:** Double-click quickly
- **Rhythm:** Click multiple times in rhythm
- **Tilt:** ⚠️ Not available in WebGL (use arrow keys if implemented)

### Gameplay:
1. Open http://localhost:8000
2. Wait for game to load (10-30 seconds)
3. Click "Start Run" button
4. Follow pattern instructions
5. React before time runs out
6. Try to climb as high as possible!

---

## 🔧 Alternative Method: Manual Build + Serve

If "Build and Run" doesn't work:

### Build Only:
```
Unity → File → Build Settings → WebGL
Click "Build" (not "Build and Run")
Choose: /home/sam/Projects/game-app/web-build
Wait for build to complete
```

### Serve Manually:

**Option A: Using Python (easiest)**
```bash
cd /home/sam/Projects/game-app/web-build
python3 -m http.server 8000

# Open browser: http://localhost:8000
```

**Option B: Using Node.js**
```bash
cd /home/sam/Projects/game-app/web-build
npx http-server -p 8000 -c-1

# Open browser: http://localhost:8000
```

**Option C: Using Our Script**
```bash
cd /home/sam/Projects/game-app
./serve-web.sh

# Opens: http://localhost:8000
```

---

## ⚠️ Common Issues & Fixes

### Issue 1: "Failed to load build"
**Cause:** Compression not compatible with localhost
**Fix:**
```
Unity → Build Settings → Player Settings
→ Publishing Settings
→ Compression Format: Disabled
→ Rebuild
```

### Issue 2: "The server is not responding"
**Cause:** CORS issues or wrong server
**Fix:**
```bash
# Use Unity's "Build and Run" instead
# Or add CORS headers if using custom server
```

### Issue 3: Blank white screen
**Cause:** WebGL template issues
**Fix:**
```
Unity → Build Settings → Player Settings
→ Resolution and Presentation
→ WebGL Template: Default
→ Rebuild
```

### Issue 4: Game loads but doesn't start
**Cause:** Supabase credentials not set or backend not deployed
**Fix:**
```
See BACKEND_TEST_REPORT.md to deploy backend first
Or test in offline mode (Practice Mode should work)
```

### Issue 5: Very slow loading
**Cause:** WebGL builds are large (30-100MB)
**Fix:**
```
Wait patiently (first load takes 30-60 seconds)
Subsequent loads use browser cache (faster)

Or enable compression:
Player Settings → Publishing Settings
→ Compression Format: Gzip
```

---

## 📊 Performance Expectations

| Aspect | WebGL Browser | Native Mobile |
|--------|---------------|---------------|
| **Load Time** | 30-60 seconds | 1-2 seconds |
| **FPS** | 30-60 fps | 60+ fps |
| **File Size** | 50-100 MB | 20-40 MB APK |
| **Input Lag** | Slight delay | Instant |
| **Battery** | High usage | Optimized |

**WebGL is great for testing, but mobile build is recommended for real play.**

---

## 🎯 What Works vs What Doesn't

### ✅ Works in WebGL:
- Pattern generation
- Tap patterns (mouse click)
- Swipe patterns (click + drag)
- Hold patterns (click + hold)
- Double tap (double-click)
- Rhythm patterns (multiple clicks)
- Backend integration (if deployed)
- Leaderboards
- Missions tracking
- Shop/Settings UI

### ❌ Doesn't Work in WebGL:
- Tilt/gyroscope patterns (no device orientation API in most browsers)
- Vibration feedback
- Push notifications
- Native performance
- Mobile-optimized UI sizing

### ⚠️ Works But Limited:
- Touch simulation (mouse works but not the same as finger)
- Performance (slower than native)
- Audio (may have autoplay restrictions)

---

## 🚀 Recommended Workflow

### For Quick Testing:
```
WebGL Build → localhost:8000 → Test in browser
```
**Pros:** Fast to test, easy to iterate
**Cons:** Not full mobile experience

### For Real Gameplay:
```
Android/iOS Build → Install on phone → Play natively
```
**Pros:** Full experience, better performance
**Cons:** Longer build times, needs device

### For Sharing with Others:
```
WebGL Build → Upload to itch.io → Share link
```
Anyone can play in browser (no download needed)

---

## 📱 After WebGL Testing

Once you've tested in browser and it works, build to mobile:

**Android:**
```
Unity → Build Settings → Android → Switch Platform
Connect phone via USB
Build and Run → Installs APK on phone
```

**iOS (Requires Mac):**
```
Unity → Build Settings → iOS → Switch Platform
Build → Creates Xcode project
Open in Xcode → Deploy to iPhone
```

---

## 🎓 Summary

**To play on localhost:**

1. Open Unity Hub
2. Add project: `/home/sam/Projects/game-app/client`
3. File → Build Settings → WebGL → Build and Run
4. Wait 10-20 min
5. Browser auto-opens to localhost
6. Play game in browser! 🎮

**Or if you already built:**

```bash
cd /home/sam/Projects/game-app
./serve-web.sh
# Visit: http://localhost:8000
```

---

**That's it! You can now play Tower Climb in your browser at localhost:8000**

Any issues? Check the "Common Issues & Fixes" section above.
