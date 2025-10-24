# 🏔️ Tower Climb - Standalone Web App

**Like Wordle** - Just open `index.html` in your browser and play!

No Unity, no installation, no complex setup. Just a simple web game.

---

## 🚀 Quick Start (2 Ways)

### Option 1: Open Directly (Instant!)

```bash
cd /home/sam/Projects/game-app/web-app
# Then just double-click index.html
# OR
open index.html  # Mac
xdg-open index.html  # Linux
start index.html  # Windows
```

**That's it!** Game opens in your browser.

---

### Option 2: Use Local Server (Better for Testing)

```bash
cd /home/sam/Projects/game-app/web-app

# Python
python3 -m http.server 8000

# Node.js
npx http-server -p 8000

# Then visit: http://localhost:8000
```

---

## 🎮 How to Play

1. **Open the game** (one of the methods above)
2. **Click "Start Run"**
3. **Follow the pattern** shown on screen
4. **React quickly:**
   - TAP: Click once
   - DOUBLE TAP: Click twice quickly
   - SWIPE: Click and drag in direction
   - HOLD: Click and hold for duration
5. **Keep going** until you fail!
6. **See your score** and try again

---

## 🌐 Deploy to the Internet

Want to share with friends? Deploy it for FREE:

### Deploy to Netlify (Easiest - 2 minutes)

1. Go to https://app.netlify.com/drop
2. Drag the `/web-app` folder into the page
3. Get a public URL like: `https://tower-climb-xxxxx.netlify.app`
4. Share the link!

**No signup required for basic hosting!**

---

### Deploy to GitHub Pages (3 minutes)

```bash
# 1. Create GitHub repo
gh repo create tower-climb --public

# 2. Push web-app folder
cd /home/sam/Projects/game-app/web-app
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/tower-climb.git
git push -u origin main

# 3. Enable GitHub Pages
# Go to: Settings → Pages → Source: main branch
# Get URL: https://YOUR_USERNAME.github.io/tower-climb
```

---

### Deploy to Vercel (2 minutes)

1. Install Vercel CLI: `npm i -g vercel`
2. Run: `cd web-app && vercel`
3. Follow prompts
4. Get URL like: `https://tower-climb.vercel.app`

---

## ⚙️ Optional: Connect to Backend

Currently runs in **offline mode** (saves best score locally).

To enable online features (leaderboards, weekly competition):

**1. Deploy your Supabase backend first:**
```bash
cd /home/sam/Projects/game-app/server
npx supabase db push
npx supabase functions deploy --all
npx supabase status
```

**2. Add credentials to `supabase-client.js`:**
```javascript
// In supabase-client.js, update these lines:
this.supabaseUrl = 'https://YOUR_PROJECT.supabase.co';
this.supabaseAnonKey = 'YOUR_ANON_KEY';
```

**3. Refresh the page** - now online features work!

---

## 📂 File Structure

```
web-app/
├── index.html           # Main HTML file
├── styles.css           # All styles
├── game.js              # Main game logic
├── prng.js              # Random number generator
├── pattern-generator.js # Pattern generation
├── supabase-client.js   # Backend integration
└── README.md            # This file
```

**Total: 6 files, ~1000 lines of code**

---

## ✨ Features

✅ **Works offline** - No internet needed
✅ **Mobile-friendly** - Touch controls work
✅ **Saves progress** - Best score in localStorage
✅ **Fast loading** - No big frameworks
✅ **Clean UI** - Modern, dark theme
✅ **Deterministic** - Same seed = same patterns
✅ **Backend ready** - Add Supabase credentials for online play

---

## 🎯 Differences from Unity Version

| Feature | Unity Version | Web Version |
|---------|---------------|-------------|
| **Installation** | Requires Unity | None - just open |
| **Build Time** | 20-30 min | Instant |
| **File Size** | 50-100MB | ~50KB |
| **Platform** | Mobile app | Browser |
| **Graphics** | Unity effects | CSS animations |
| **Tilt Input** | ✅ Gyroscope | ❌ Not supported |
| **Performance** | 60 FPS native | 60 FPS browser |
| **Deployment** | App stores | Web hosting |

---

## 🚀 Try It Now!

**Fastest way:**
```bash
cd /home/sam/Projects/game-app/web-app
python3 -m http.server 8000
# Visit: http://localhost:8000
```

**Or just double-click `index.html`!**

---

## 🐛 Troubleshooting

### "Game doesn't load"
- Make sure all 6 files are in the same folder
- Try using a local server instead of opening directly
- Check browser console (F12) for errors

### "Can't connect to backend"
- Normal! Game works offline by default
- To enable online: add Supabase credentials

### "Touch doesn't work"
- Use mouse on desktop
- Swipe = click and drag
- Hold = click and hold

### "Leaderboard is empty"
- Expected in offline mode
- Deploy backend + add credentials to enable

---

## 📊 What This Includes

- ✅ Full game loop (start → play → fail → results)
- ✅ 5 pattern types (tap, swipe, hold, rhythm, double-tap)
- ✅ Deterministic pattern generation (same as Unity/backend)
- ✅ Progress tracking (best floor saved)
- ✅ Results screen with stats
- ✅ Leaderboard UI (shows when backend connected)
- ✅ Responsive design (works on phone/tablet/desktop)

---

## 🎮 Play Now!

No Unity, no build, no wait. Just open and play!

**Next Steps:**
1. Open `index.html` in browser
2. Click "Start Run"
3. Enjoy!
4. (Optional) Deploy to share with friends

---

**Made with ❤️ - Simple standalone version, no dependencies, pure vanilla JavaScript**
