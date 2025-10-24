#!/bin/bash

# Tower Climb - Deployment Verification Script
# This script verifies backend and provides checklist for frontend testing

set -e

echo "🎮 Tower Climb - Deployment Verification"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track results
BACKEND_PASS=0
BACKEND_FAIL=0
WARNINGS=0

echo "📋 PART 1: Backend Verification"
echo "================================"
echo ""

# Check if we're in the right directory
if [ ! -d "server" ]; then
    echo -e "${RED}❌ Error: Must run from /home/sam/Projects/game-app${NC}"
    exit 1
fi

cd server

# Check 1: Verify directory structure
echo "1️⃣  Checking directory structure..."
REQUIRED_DIRS=(
    "supabase/functions/start-run"
    "supabase/functions/submit-run"
    "supabase/functions/get-leaderboard"
    "supabase/functions/weekly-seed-reset"
    "shared/utils"
    "shared/types"
)

for dir in "${REQUIRED_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        echo -e "   ${GREEN}✓${NC} $dir"
        ((BACKEND_PASS++))
    else
        echo -e "   ${RED}✗${NC} $dir (MISSING)"
        ((BACKEND_FAIL++))
    fi
done
echo ""

# Check 2: Verify TypeScript files exist
echo "2️⃣  Checking TypeScript files..."
REQUIRED_FILES=(
    "supabase/functions/start-run/index.ts"
    "supabase/functions/submit-run/index.ts"
    "supabase/functions/get-leaderboard/index.ts"
    "supabase/functions/weekly-seed-reset/index.ts"
    "shared/utils/prng.ts"
    "shared/utils/pattern-generator.ts"
    "shared/types/game-types.ts"
)

for file in "${REQUIRED_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo -e "   ${GREEN}✓${NC} $file"
        ((BACKEND_PASS++))
    else
        echo -e "   ${RED}✗${NC} $file (MISSING)"
        ((BACKEND_FAIL++))
    fi
done
echo ""

# Check 3: Verify import statements are correct (.ts not .js)
echo "3️⃣  Checking import statements..."
BAD_IMPORTS=$(grep -r "from.*\.js['\"]" --include="*.ts" . 2>/dev/null || true)
if [ -z "$BAD_IMPORTS" ]; then
    echo -e "   ${GREEN}✓${NC} All imports use .ts extensions"
    ((BACKEND_PASS++))
else
    echo -e "   ${RED}✗${NC} Found .js imports in TypeScript files:"
    echo "$BAD_IMPORTS"
    ((BACKEND_FAIL++))
fi
echo ""

# Check 4: Check for Deno installation
echo "4️⃣  Checking runtime dependencies..."
if command -v deno &> /dev/null; then
    DENO_VERSION=$(deno --version | head -n1)
    echo -e "   ${GREEN}✓${NC} Deno installed: $DENO_VERSION"
    ((BACKEND_PASS++))

    # Run tests if Deno is available
    echo ""
    echo "5️⃣  Running backend tests..."
    if deno run --allow-read test-backend.ts; then
        echo -e "   ${GREEN}✓${NC} All backend tests passed"
        ((BACKEND_PASS++))
    else
        echo -e "   ${RED}✗${NC} Some tests failed"
        ((BACKEND_FAIL++))
    fi
else
    echo -e "   ${YELLOW}⚠${NC}  Deno not installed (optional for testing)"
    echo "   To install: curl -fsSL https://deno.land/install.sh | sh"
    ((WARNINGS++))
fi
echo ""

# Check 5: Check for Supabase CLI
echo "6️⃣  Checking Supabase CLI..."
if command -v supabase &> /dev/null; then
    SUPABASE_VERSION=$(supabase --version)
    echo -e "   ${GREEN}✓${NC} Supabase CLI installed: $SUPABASE_VERSION"
    ((BACKEND_PASS++))

    # Check if initialized
    if [ -f "supabase/config.toml" ]; then
        echo -e "   ${GREEN}✓${NC} Supabase project initialized"
        ((BACKEND_PASS++))
    else
        echo -e "   ${YELLOW}⚠${NC}  Supabase not initialized (run: npx supabase init)"
        ((WARNINGS++))
    fi
else
    echo -e "   ${YELLOW}⚠${NC}  Supabase CLI not installed"
    echo "   To install: npm install -g supabase"
    echo "   Or use npx: npx supabase <command>"
    ((WARNINGS++))
fi
echo ""

# Check 6: Check Unity client structure
echo ""
echo "📋 PART 2: Unity Client Verification"
echo "====================================="
echo ""

cd ../client

echo "7️⃣  Checking Unity project structure..."
UNITY_DIRS=(
    "Assets/Scripts/Core"
    "Assets/Scripts/Gameplay"
    "Assets/Scripts/UI"
    "Assets/Scripts/API"
    "Assets/Scripts/Analytics"
    "Assets/Scripts/Utils"
)

for dir in "${UNITY_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        echo -e "   ${GREEN}✓${NC} $dir"
        ((BACKEND_PASS++))
    else
        echo -e "   ${RED}✗${NC} $dir (MISSING)"
        ((BACKEND_FAIL++))
    fi
done
echo ""

echo "8️⃣  Checking C# scripts..."
CSHARP_COUNT=$(find Assets/Scripts -name "*.cs" -type f | wc -l)
if [ "$CSHARP_COUNT" -ge 25 ]; then
    echo -e "   ${GREEN}✓${NC} Found $CSHARP_COUNT C# scripts (expected 27)"
    ((BACKEND_PASS++))
else
    echo -e "   ${RED}✗${NC} Only found $CSHARP_COUNT C# scripts (expected 27)"
    ((BACKEND_FAIL++))
fi
echo ""

# Summary
cd ..
echo ""
echo "=========================================="
echo "📊 VERIFICATION SUMMARY"
echo "=========================================="
echo ""
echo -e "Backend Checks:  ${GREEN}✓ $BACKEND_PASS passed${NC}  ${RED}✗ $BACKEND_FAIL failed${NC}  ${YELLOW}⚠ $WARNINGS warnings${NC}"
echo ""

if [ $BACKEND_FAIL -eq 0 ]; then
    echo -e "${GREEN}✅ All critical checks passed!${NC}"
    echo ""
    echo "📝 NEXT STEPS:"
    echo ""
    echo "1. Deploy Backend (15 min):"
    echo "   cd server"
    echo "   npx supabase db push"
    echo "   npx supabase functions deploy --all"
    echo "   npx supabase status  # Get your API URL and keys"
    echo ""
    echo "2. Test Unity (30 min):"
    echo "   - Follow START_HERE.md"
    echo "   - Open Unity Hub → Add project → /home/sam/Projects/game-app/client"
    echo "   - Install TextMeshPro when prompted"
    echo "   - Create test scene (see START_HERE.md)"
    echo "   - Set Supabase credentials in Inspector"
    echo "   - Press Play"
    echo ""
    echo "3. Run Pattern Tests (if Deno installed):"
    echo "   cd server"
    echo "   deno run --allow-read test-backend.ts"
    echo ""
else
    echo -e "${RED}❌ Some checks failed - please review errors above${NC}"
    exit 1
fi
