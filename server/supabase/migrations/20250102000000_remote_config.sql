-- Remote configuration table for live-tuning without deploys
-- Supports versioning and A/B testing

create table remote_config (
  key text primary key,
  value jsonb not null,
  description text,
  version int default 1,
  active boolean default true,
  ab_test_group text, -- null = all users, 'A' or 'B' for split testing
  updated_at timestamptz default now()
);

-- Enable RLS
alter table remote_config enable row level security;

-- Everyone can read active config
create policy "Remote config is public"
  on remote_config for select
  using (active = true);

-- Insert default difficulty config
insert into remote_config (key, value, description) values
(
  'difficulty',
  '{
    "v0": 1.0,
    "deltaV": 0.05,
    "minWindow": 0.3,
    "maxWindow": 2.0,
    "baseWindow": 1.5,
    "adaptiveEpsilon": 0.1,
    "baseWeights": {
      "tap": 0.3,
      "swipe": 0.3,
      "hold": 0.2,
      "rhythm": 0.1,
      "tilt": 0.05,
      "doubleTap": 0.05
    }
  }'::jsonb,
  'Core difficulty configuration'
),
(
  'missions_daily',
  '[
    {
      "id": "3_perfect_holds",
      "description": "Achieve 3 perfect holds in a row",
      "reward": "sfx_pack_minimal",
      "target": 3,
      "trackingKey": "consecutive_perfect_holds"
    },
    {
      "id": "survive_60s",
      "description": "Survive for 60 seconds",
      "reward": "theme_ocean",
      "target": 60,
      "trackingKey": "runtime_seconds"
    },
    {
      "id": "floor_20_no_miss",
      "description": "Reach floor 20 without missing",
      "reward": "theme_fire",
      "target": 20,
      "trackingKey": "floors_without_miss"
    }
  ]'::jsonb,
  'Daily mission definitions'
),
(
  'unlocks_milestones',
  '{
    "10": "theme_retro",
    "20": "theme_neon",
    "30": "sfx_pack_cyber",
    "50": "theme_minimal",
    "75": "sfx_pack_orchestral",
    "100": "theme_galaxy"
  }'::jsonb,
  'Floor milestone unlocks'
);

-- Function to get config for client
create or replace function get_remote_config(config_keys text[] default null)
returns table (key text, value jsonb, version int)
language sql
stable
as $$
  select key, value, version
  from remote_config
  where active = true
    and (config_keys is null or key = any(config_keys));
$$;

-- Index for fast config lookups
create index idx_remote_config_active on remote_config(active, key);
