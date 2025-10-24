# Tower Climb Game - Backend

Supabase-based backend for the tower climb mobile game with deterministic pattern generation, anti-cheat validation, and weekly leaderboards.

## Architecture Overview

```
┌─────────────┐
│   Client    │ (Unity/Phaser)
└──────┬──────┘
       │
       ├─ start-run → Get weekly seed
       ├─ submit-run → Validate & update leaderboard
       └─ get-leaderboard → Fetch rankings
       │
┌──────▼──────────────────────────┐
│   Supabase Edge Functions       │
│  - start-run                    │
│  - submit-run (anti-cheat)      │
│  - get-leaderboard              │
│  - weekly-seed-reset (cron)     │
└──────┬──────────────────────────┘
       │
┌──────▼──────────────────────────┐
│   PostgreSQL Database           │
│  - app_user                     │
│  - season (weekly seeds)        │
│  - season_score (leaderboard)   │
│  - run (telemetry)              │
│  - player_model (adaptive AI)   │
│  - unlock, mission_progress     │
│  - remote_config                │
└─────────────────────────────────┘
```

## Directory Structure

```
server/
├── supabase/
│   ├── migrations/
│   │   ├── 20250101000000_initial_schema.sql
│   │   └── 20250102000000_remote_config.sql
│   └── functions/
│       ├── start-run/
│       │   └── index.ts
│       ├── submit-run/
│       │   └── index.ts
│       ├── get-leaderboard/
│       │   └── index.ts
│       └── weekly-seed-reset/
│           └── index.ts
├── shared/
│   ├── types/
│   │   └── game-types.ts
│   └── utils/
│       ├── prng.ts (xoshiro128** PRNG)
│       └── pattern-generator.ts
├── config/
│   └── difficulty.json
└── README.md (this file)
```

## Setup Instructions

### Prerequisites

- [Supabase CLI](https://supabase.com/docs/guides/cli) (`npm install -g supabase`)
- [Deno](https://deno.land/) (for local Edge Function testing)
- A Supabase project ([create one](https://app.supabase.com))

### 1. Initialize Supabase Project

```bash
cd server
supabase init
```

Link to your Supabase project:

```bash
supabase link --project-ref YOUR_PROJECT_REF
```

Get your project ref from: https://app.supabase.com/project/YOUR_PROJECT/settings/general

### 2. Run Database Migrations

Apply the schema migrations:

```bash
supabase db push
```

This creates all tables, indexes, RLS policies, and helper functions.

### 3. Deploy Edge Functions

Deploy all Edge Functions:

```bash
# Deploy individual functions
supabase functions deploy start-run
supabase functions deploy submit-run
supabase functions deploy get-leaderboard
supabase functions deploy weekly-seed-reset

# Or deploy all at once
supabase functions deploy start-run submit-run get-leaderboard weekly-seed-reset
```

### 4. Set Environment Variables

Edge Functions need access to Supabase credentials. These are automatically set, but verify:

```bash
# View current secrets
supabase secrets list

# Should include:
# - SUPABASE_URL
# - SUPABASE_ANON_KEY
# - SUPABASE_SERVICE_ROLE_KEY
```

### 5. Set Up Weekly Cron Job

Use GitHub Actions, Supabase Cron (when available), or an external service like cron-job.org:

**Option A: GitHub Actions (recommended)**

Create `.github/workflows/weekly-reset.yml` in your repo:

```yaml
name: Weekly Season Reset
on:
  schedule:
    - cron: '0 0 * * 1' # Every Monday at 00:00 UTC
  workflow_dispatch: # Manual trigger for testing

jobs:
  reset-season:
    runs-on: ubuntu-latest
    steps:
      - name: Call weekly-seed-reset function
        run: |
          curl -X POST https://YOUR_PROJECT_REF.supabase.co/functions/v1/weekly-seed-reset \
            -H "Authorization: Bearer ${{ secrets.SUPABASE_SERVICE_ROLE_KEY }}" \
            -H "Content-Type: application/json"
```

Add `SUPABASE_SERVICE_ROLE_KEY` to your GitHub repository secrets.

**Option B: Manual Trigger (for testing)**

```bash
curl -X POST https://YOUR_PROJECT_REF.supabase.co/functions/v1/weekly-seed-reset \
  -H "Authorization: Bearer YOUR_SERVICE_ROLE_KEY" \
  -H "Content-Type: application/json"
```

### 6. Seed Initial Season

Create the first season manually:

```sql
-- Run in Supabase SQL Editor
insert into season (week_id, seed, starts_at, ends_at)
values (
  202501, -- YYYYWW format for week 1 of 2025
  1234567890123456789, -- random bigint seed
  now(),
  now() + interval '7 days'
);
```

Or trigger the cron function once (see Option B above).

### 7. Enable Anonymous Auth

In Supabase Dashboard → Authentication → Providers:
- Enable "Anonymous sign-ins"

This allows guest players to play without signup (can upgrade later).

## API Reference

### 1. Start Run

**Endpoint:** `POST /functions/v1/start-run`

**Headers:**
```
Authorization: Bearer USER_JWT
```

**Response:**
```json
{
  "userId": "uuid",
  "weekId": 202501,
  "seed": "1234567890123456789",
  "startsAt": "2025-01-06T00:00:00Z",
  "endsAt": "2025-01-13T00:00:00Z",
  "currentBest": 42
}
```

### 2. Submit Run

**Endpoint:** `POST /functions/v1/submit-run`

**Headers:**
```
Authorization: Bearer USER_JWT
Content-Type: application/json
```

**Body:**
```json
{
  "weekId": 202501,
  "floors": 25,
  "runtimeSeconds": 187.3,
  "avgReactionMs": 342,
  "breakdown": {
    "tap": { "attempts": 10, "perfects": 8, "goods": 2, "misses": 0, "avgReactionMs": 320 },
    "swipe": { "attempts": 8, "perfects": 6, "goods": 1, "misses": 1, "avgReactionMs": 380 }
  },
  "timings": [
    { "floor": 1, "patternType": "tap", "reactionMs": 345, "success": true, "accuracy": 0.95, "timestamp": 1704556800000 }
  ],
  "playerModel": {
    "weaknesses": { "hold": 0.6, "rhythm": 0.4 },
    "last5": [
      { "floor": 21, "reactionMs": 340, "success": true, "accuracy": 0.92 }
    ]
  },
  "clientVersion": "1.0.0"
}
```

**Response:**
```json
{
  "success": true,
  "cheatFlags": 0,
  "newBest": true,
  "unlocks": ["theme_neon"]
}
```

**Cheat Flags (bitfield):**
- `0` - No flags
- `1` - Impossible timing (reaction < 100ms)
- `2` - Impossible floors (too many for runtime)
- `4` - Pattern mismatch
- `8` - Suspicious distribution (bot-like consistency)
- `16` - Replay attack (duplicate submission)

### 3. Get Leaderboard

**Endpoint:** `GET /functions/v1/get-leaderboard?weekId=202501&scope=global&limit=100&offset=0`

**Headers:**
```
Authorization: Bearer USER_JWT (optional)
```

**Query Parameters:**
- `weekId` (optional): Week to fetch (defaults to current)
- `scope`: `global`, `country`, or `friends`
- `limit`: Number of entries (default 100, max 500)
- `offset`: Pagination offset
- `country`: Filter by country (only for scope=country)

**Response:**
```json
{
  "weekId": 202501,
  "scope": "global",
  "entries": [
    {
      "rank": 1,
      "userId": "uuid",
      "handle": "ProGamer123",
      "bestFloor": 127,
      "bestReactionMs": 298,
      "perfectRate": 0.89
    }
  ],
  "userEntry": {
    "rank": 456,
    "userId": "uuid",
    "handle": "You",
    "bestFloor": 42,
    "bestReactionMs": 352,
    "perfectRate": 0.78
  },
  "hasMore": true
}
```

## Pattern Generation (Critical for Anti-Cheat)

The pattern generator **must** produce identical results on client and server for the same seed and floor.

**Algorithm:**
1. Mix `seed` with `floor` using XOR and prime multiplication
2. Initialize xoshiro128** PRNG with mixed seed
3. Calculate pattern weights (base + player weakness bias)
4. Weighted random choice of pattern type
5. Calculate speed: `v0 + floor * deltaV + adaptiveBoost`
6. Calculate time window: `clamp(baseWindow / speed, minWindow, maxWindow)`

**Example (TypeScript):**
```typescript
import { PatternGenerator } from './shared/utils/pattern-generator.ts';

const generator = new PatternGenerator(difficultyConfig);
const pattern = generator.generate(BigInt(weekSeed), 10, playerModel);
// pattern = { type: 'swipe', direction: 'L', timeWindow: 0.8, speed: 1.5 }
```

**Unity C# Port (coming soon):**
- Same xoshiro128** PRNG implementation
- Identical weighted choice logic
- Byte-for-byte determinism verified by unit tests

## Remote Configuration

Tune difficulty without redeploying:

```sql
-- Update difficulty in Supabase SQL Editor
update remote_config
set value = jsonb_set(value, '{deltaV}', '0.06')
where key = 'difficulty';
```

Client fetches config on app start:

```typescript
const { data } = await supabase.rpc('get_remote_config', {
  config_keys: ['difficulty', 'missions_daily']
});
```

## Testing

### Local Edge Function Testing

```bash
# Start Supabase locally
supabase start

# Test start-run function
supabase functions serve start-run --env-file .env.local

# Call it
curl http://localhost:54321/functions/v1/start-run \
  -H "Authorization: Bearer ANON_KEY"
```

### Unit Tests for Pattern Generator

Create `server/shared/utils/pattern-generator.test.ts`:

```typescript
import { assertEquals } from 'https://deno.land/std@0.168.0/testing/asserts.ts';
import { PatternGenerator, DEFAULT_DIFFICULTY_CONFIG } from './pattern-generator.ts';

Deno.test('Pattern generation is deterministic', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(12345);

  const pattern1 = gen.generate(seed, 10);
  const pattern2 = gen.generate(seed, 10);

  assertEquals(pattern1, pattern2);
});

Deno.test('Different floors produce different patterns', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(12345);

  const p1 = gen.generate(seed, 1);
  const p2 = gen.generate(seed, 2);

  // Patterns should differ (very high probability)
  assertEquals(p1.type !== p2.type || p1.timeWindow !== p2.timeWindow, true);
});
```

Run tests:
```bash
deno test server/shared/utils/
```

## Monitoring & Analytics

### Key Metrics to Track

1. **Performance:**
   - Edge Function response times
   - Database query latency
   - Cron job success rate

2. **Game Metrics:**
   - Daily/weekly active users
   - Average floors reached per run
   - Cheat flag distribution
   - Unlock progression rates

3. **Business:**
   - Retention (D1, D7, D30)
   - Session length
   - Retry rate after fail

### Supabase Dashboard

Monitor in real-time:
- Database → Logs → Slow queries
- Edge Functions → Logs → Errors
- Auth → Users → Growth charts

### Export Telemetry

Query `run` table for analytics:

```sql
-- Average floors by day
select
  date_trunc('day', created_at) as day,
  avg(floors) as avg_floors,
  count(*) as total_runs
from run
group by day
order by day desc;

-- Pattern performance
select
  jsonb_object_keys(breakdown) as pattern_type,
  avg((breakdown->jsonb_object_keys(breakdown)->>'avgReactionMs')::int) as avg_reaction
from run
group by pattern_type;
```

## Security Considerations

1. **RLS Policies:** All tables have Row Level Security enabled
2. **Auth Required:** All Edge Functions validate JWT tokens
3. **Service Role Protection:** Cron job requires service role key
4. **Rate Limiting:** Enable in Supabase Dashboard → Settings → API
5. **CORS:** Configured for production domain only (update in Edge Functions)

## Deployment Checklist

- [ ] Database migrations applied
- [ ] Edge Functions deployed
- [ ] Anonymous auth enabled
- [ ] Initial season created
- [ ] Weekly cron job configured
- [ ] Remote config populated
- [ ] Rate limits configured (1000 req/min recommended)
- [ ] Monitoring alerts set up
- [ ] CORS updated for production domain
- [ ] Service role key secured (not in client code!)

## Troubleshooting

**Issue:** "No active season" error

**Fix:**
```sql
-- Check if season exists
select * from season where now() between starts_at and ends_at;

-- If none, create one
insert into season (week_id, seed, starts_at, ends_at)
values (extract(year from now())::int * 100 + extract(week from now())::int,
        floor(random() * 9223372036854775807)::bigint,
        date_trunc('week', now()),
        date_trunc('week', now()) + interval '7 days');
```

**Issue:** Edge Functions timing out

**Fix:** Check Supabase logs, likely slow query. Add indexes or optimize RLS policies.

**Issue:** Pattern mismatch cheat flags

**Fix:** Ensure client PRNG implementation exactly matches server. Run cross-platform tests.

## Next Steps

1. **Client Integration:** See `client/README.md` for Unity setup
2. **Analytics Dashboard:** Build custom dashboard querying `run` table
3. **A/B Testing:** Use `remote_config.ab_test_group` for split testing difficulty
4. **Push Notifications:** Integrate with OneSignal/FCM for weekly reset alerts
5. **Social Features:** Add friends table and friend leaderboards

## Support

- Supabase Docs: https://supabase.com/docs
- Edge Functions: https://supabase.com/docs/guides/functions
- Discord: https://discord.supabase.com

---

Built with ❤️ using Supabase
