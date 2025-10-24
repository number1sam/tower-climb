# Quick Start Guide

Get the backend running in 10 minutes.

## Prerequisites

```bash
# Install Supabase CLI
npm install -g supabase

# Install Deno (for Edge Functions)
curl -fsSL https://deno.land/install.sh | sh
```

## 1. Create Supabase Project

1. Go to [supabase.com/dashboard](https://supabase.com/dashboard)
2. Click "New Project"
3. Name it "tower-climb" and set a database password
4. Wait for project to provision (~2 minutes)

## 2. Get Project Credentials

From your project dashboard:

1. Go to Settings → API
2. Copy:
   - Project URL (e.g., `https://abcdefgh.supabase.co`)
   - `anon` public key
   - `service_role` secret key (keep this secret!)
3. Copy your Project Reference from Settings → General

## 3. Link Local Project

```bash
cd server

# Initialize Supabase config
supabase init

# Link to your cloud project
supabase link --project-ref YOUR_PROJECT_REF
# Enter your database password when prompted
```

## 4. Configure Environment

```bash
# Copy example env file
cp .env.example .env.local

# Edit .env.local and add your credentials
nano .env.local  # or use any text editor
```

Fill in:
```
SUPABASE_URL=https://YOUR_PROJECT_REF.supabase.co
SUPABASE_ANON_KEY=your_anon_key_here
SUPABASE_SERVICE_ROLE_KEY=your_service_role_key_here
```

## 5. Deploy Database

```bash
# Apply migrations (creates all tables, functions, policies)
supabase db push
```

You should see:
```
✅ Applying migration 20250101000000_initial_schema.sql...
✅ Applying migration 20250102000000_remote_config.sql...
✅ Finished supabase db push.
```

## 6. Deploy Edge Functions

```bash
# Deploy all functions at once
./scripts/deploy.sh
```

Or manually:
```bash
supabase functions deploy start-run
supabase functions deploy submit-run
supabase functions deploy get-leaderboard
supabase functions deploy weekly-seed-reset
```

## 7. Enable Anonymous Auth

1. Go to your Supabase Dashboard → Authentication → Providers
2. Find "Anonymous sign-ins"
3. Toggle it **ON**

This allows guest players without requiring signup.

## 8. Create Initial Season

Run in Supabase SQL Editor (Dashboard → SQL Editor → New Query):

```sql
insert into season (week_id, seed, starts_at, ends_at)
values (
  extract(year from now())::int * 100 + extract(week from now())::int,
  floor(random() * 9223372036854775807)::bigint,
  now(),
  now() + interval '7 days'
);
```

Click "Run" to create the first season.

## 9. Test the API

```bash
# Get your anon key from .env.local
ANON_KEY="your_anon_key_here"
PROJECT_URL="https://YOUR_PROJECT_REF.supabase.co"

# Test anonymous signup
curl -X POST "$PROJECT_URL/auth/v1/signup" \
  -H "apikey: $ANON_KEY" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"testpass123"}'

# Copy the access_token from response

# Test start-run
curl "$PROJECT_URL/functions/v1/start-run" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "apikey: $ANON_KEY"
```

You should see a JSON response with `weekId`, `seed`, etc.

## 10. Set Up Weekly Cron (Optional)

Add GitHub repository secrets:
1. Go to your GitHub repo → Settings → Secrets → Actions
2. Add:
   - `SUPABASE_URL`: Your project URL
   - `SUPABASE_SERVICE_ROLE_KEY`: Your service role key

The workflow in `.github/workflows/weekly-reset.yml` will automatically run every Monday.

## Verify Everything Works

```bash
# Run pattern generator tests
deno test --allow-all shared/utils/pattern-generator.test.ts
```

You should see:
```
✅ All pattern generator tests passed!
```

## Next Steps

1. **Client Integration:** Build Unity client (see plan in main README)
2. **Customize Config:** Edit `server/config/difficulty.json` and update in Supabase
3. **Monitor:** Check Supabase Dashboard → Database → Logs for activity
4. **Analytics:** Query `run` table for player statistics

## Common Issues

**"relation does not exist" error**
→ Run `supabase db push` to apply migrations

**"Invalid JWT" error**
→ Check Authorization header includes "Bearer " prefix

**"No active season" error**
→ Run the SQL query in Step 8 to create a season

**Edge Function timeout**
→ Check function logs in Supabase Dashboard → Edge Functions

## Get Help

- Full docs: `server/README.md`
- Supabase docs: https://supabase.com/docs
- Issues: Open a GitHub issue

---

**Estimated time:** 10 minutes
**Difficulty:** Beginner-friendly
