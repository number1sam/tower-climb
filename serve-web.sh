#!/bin/bash

# Serve Unity WebGL build on localhost

echo "🎮 Tower Climb - Web Server"
echo "============================="
echo ""

# Check if build exists
if [ ! -d "web-build" ]; then
    echo "❌ Error: web-build folder not found"
    echo ""
    echo "Please build the game first:"
    echo "1. Open Unity"
    echo "2. File → Build Settings → WebGL"
    echo "3. Click 'Build' and save to: /home/sam/Projects/game-app/web-build"
    exit 1
fi

cd web-build

echo "🚀 Starting local web server..."
echo ""
echo "Game will be available at:"
echo "  → http://localhost:8000"
echo ""
echo "Press Ctrl+C to stop server"
echo ""

# Try different servers in order of preference
if command -v python3 &> /dev/null; then
    echo "Using Python 3..."
    python3 -m http.server 8000
elif command -v python &> /dev/null; then
    echo "Using Python 2..."
    python -m SimpleHTTPServer 8000
elif command -v npx &> /dev/null; then
    echo "Using Node.js..."
    npx http-server -p 8000
elif command -v php &> /dev/null; then
    echo "Using PHP..."
    php -S localhost:8000
else
    echo "❌ Error: No web server available"
    echo ""
    echo "Please install one:"
    echo "  - Python: sudo apt install python3"
    echo "  - Node.js: sudo apt install nodejs npm"
    echo "  - PHP: sudo apt install php"
    exit 1
fi
