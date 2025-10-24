#!/bin/bash

# Deployment script for Tower Climb backend
# Deploys database migrations and Edge Functions to Supabase

set -e  # Exit on error

echo "🚀 Tower Climb Backend Deployment"
echo "=================================="
echo ""

# Check if Supabase CLI is installed
if ! command -v supabase &> /dev/null; then
    echo "❌ Supabase CLI not found. Install with: npm install -g supabase"
    exit 1
fi

# Check if linked to a project
if [ ! -f .supabase/config.toml ]; then
    echo "❌ Not linked to a Supabase project."
    echo "Run: supabase link --project-ref YOUR_PROJECT_REF"
    exit 1
fi

echo "📊 Step 1: Applying database migrations..."
supabase db push

echo ""
echo "⚡ Step 2: Deploying Edge Functions..."

# Deploy each function
echo "  - Deploying start-run..."
supabase functions deploy start-run --no-verify-jwt

echo "  - Deploying submit-run..."
supabase functions deploy submit-run --no-verify-jwt

echo "  - Deploying get-leaderboard..."
supabase functions deploy get-leaderboard --no-verify-jwt

echo "  - Deploying weekly-seed-reset..."
supabase functions deploy weekly-seed-reset --no-verify-jwt

echo ""
echo "🧪 Step 3: Running tests..."
deno test --allow-all shared/utils/pattern-generator.test.ts

echo ""
echo "✅ Deployment complete!"
echo ""
echo "📋 Next steps:"
echo "  1. Create initial season (see README.md)"
echo "  2. Set up weekly cron job (GitHub Actions recommended)"
echo "  3. Update CORS in Edge Functions for production domain"
echo "  4. Configure rate limits in Supabase Dashboard"
echo ""
echo "🔗 Useful links:"
supabase_url=$(grep 'SUPABASE_URL' .env.local 2>/dev/null || echo "")
if [ -n "$supabase_url" ]; then
    project_ref=$(echo "$supabase_url" | grep -oP '(?<=https://)[^.]+')
    echo "  Dashboard: https://app.supabase.com/project/$project_ref"
    echo "  Functions: https://app.supabase.com/project/$project_ref/functions"
    echo "  Database: https://app.supabase.com/project/$project_ref/editor"
fi
