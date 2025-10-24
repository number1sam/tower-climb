#!/bin/bash

# Check what's needed to run the game

echo "🔍 Tower Climb - System Check"
echo "=============================="
echo ""

# Check Unity
echo "1️⃣  Checking for Unity..."
if command -v unity-editor &> /dev/null || [ -d "/Applications/Unity" ] || [ -d "$HOME/Unity/Hub/Editor" ]; then
    echo "✅ Unity appears to be installed"
    echo "   You can open the project in Unity Hub"
else
    echo "❌ Unity NOT installed"
    echo "   Download from: https://unity.com/download"
fi
echo ""

# Check if project exists
echo "2️⃣  Checking for Unity project..."
if [ -d "/home/sam/Projects/game-app/client/Assets" ]; then
    echo "✅ Unity project exists at: /home/sam/Projects/game-app/client"
else
    echo "❌ Unity project not found"
fi
echo ""

# Check for WebGL build
echo "3️⃣  Checking for WebGL build..."
if [ -d "/home/sam/Projects/game-app/web-build" ]; then
    echo "✅ WebGL build exists - can serve with ./serve-web.sh"
else
    echo "❌ No WebGL build found"
    echo "   Must build in Unity first: File → Build Settings → WebGL → Build"
fi
echo ""

# Check web servers
echo "4️⃣  Checking for web servers (to serve localhost)..."
SERVERS_FOUND=0

if command -v python3 &> /dev/null; then
    echo "✅ Python 3 installed - can serve WebGL builds"
    ((SERVERS_FOUND++))
fi

if command -v node &> /dev/null; then
    echo "✅ Node.js installed - can serve WebGL builds"
    ((SERVERS_FOUND++))
fi

if command -v php &> /dev/null; then
    echo "✅ PHP installed - can serve WebGL builds"
    ((SERVERS_FOUND++))
fi

if [ $SERVERS_FOUND -eq 0 ]; then
    echo "❌ No web server found"
    echo "   Install: sudo apt install python3"
fi
echo ""

# Check backend
echo "5️⃣  Checking backend dependencies..."
if command -v deno &> /dev/null; then
    echo "✅ Deno installed - can run backend tests"
else
    echo "❌ Deno not installed"
    echo "   Install: curl -fsSL https://deno.land/install.sh | sh"
fi

if command -v supabase &> /dev/null || command -v npx &> /dev/null; then
    echo "✅ Can deploy to Supabase"
else
    echo "⚠️  Supabase CLI not installed (optional)"
fi
echo ""

# Summary
echo "=============================="
echo "📋 SUMMARY"
echo "=============================="
echo ""
echo "To play the game, you need to:"
echo ""
echo "Option A: Test in Unity Editor (Recommended)"
echo "  1. Install Unity Hub: https://unity.com/download"
echo "  2. Open project: /home/sam/Projects/game-app/client"
echo "  3. Press Play button"
echo ""
echo "Option B: Build to WebGL for Browser"
echo "  1. Open project in Unity"
echo "  2. File → Build Settings → WebGL → Build and Run"
echo "  3. Game opens in browser at localhost"
echo ""
echo "Option C: Build to Mobile"
echo "  1. Open project in Unity"
echo "  2. File → Build Settings → Android/iOS"
echo "  3. Build and Run → Installs on connected phone"
echo ""
