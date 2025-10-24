-- Initial schema for tower climb game
-- Creates core tables: app_user, season, season_score, run

-- Enable UUID extension
create extension if not exists "uuid-ossp";

-- Users table
create table app_user (
  id uuid primary key default uuid_generate_v4(),
  created_at timestamptz default now() not null,
  handle text unique,
  country text,
  device_hash text, -- hashed device identifier for basic fraud detection
  last_seen_at timestamptz default now()
);

-- Weekly seasons (world tower resets)
create table season (
  id uuid primary key default uuid_generate_v4(),
  week_id int unique not null, -- ISO week number (YYYYWW format)
  seed bigint not null, -- PRNG seed for this week
  starts_at timestamptz not null,
  ends_at timestamptz not null,
  created_at timestamptz default now()
);

-- Leaderboard scores per season
create table season_score (
  user_id uuid references app_user(id) on delete cascade,
  week_id int references season(week_id) on delete cascade,
  best_floor int not null default 0,
  best_reaction_ms int, -- average reaction time of best run
  perfect_rate real, -- percentage of perfect hits in best run
  cheat_flags int default 0, -- bitfield for cheat detection
  breakdown jsonb, -- detailed stats: {tap: {attempts, perfects}, swipe: {...}}
  updated_at timestamptz default now(),
  primary key (user_id, week_id)
);

-- Individual runs (for analytics and player improvement tracking)
create table run (
  id uuid primary key default uuid_generate_v4(),
  user_id uuid references app_user(id) on delete cascade,
  week_id int references season(week_id) on delete cascade,
  floors int not null,
  avg_reaction_ms int,
  runtime_seconds real, -- total time from start to end
  breakdown jsonb, -- per-pattern performance: {hold: {attempts: 5, perfects: 3, avgMs: 450}}
  timings jsonb, -- array of {floor, patternType, reactionMs, success}
  client_version text,
  created_at timestamptz default now()
);

-- Player model (tracks weaknesses for adaptive difficulty)
create table player_model (
  user_id uuid primary key references app_user(id) on delete cascade,
  weaknesses jsonb, -- {hold: 0.7, swipeL: 0.4, tap: 0.2} (higher = more mistakes)
  last_5_floors jsonb, -- array of recent floor performance for speed adaptation
  updated_at timestamptz default now()
);

-- Unlocks (cosmetics, themes, sound packs)
create table unlock (
  user_id uuid references app_user(id) on delete cascade,
  item_id text not null, -- e.g., "theme_neon", "sfx_pack_cyber"
  unlocked_at timestamptz default now(),
  primary key (user_id, item_id)
);

-- Daily missions tracking
create table mission_progress (
  user_id uuid references app_user(id) on delete cascade,
  mission_id text not null,
  progress int default 0, -- current progress toward goal
  completed_at timestamptz,
  day_id date default current_date,
  primary key (user_id, mission_id, day_id)
);

-- Indexes for performance
create index idx_season_week on season(week_id);
create index idx_season_active on season(starts_at, ends_at) where ends_at > now();

create index idx_season_score_week on season_score(week_id, best_floor desc);
create index idx_season_score_user on season_score(user_id, week_id);

create index idx_run_user on run(user_id, created_at desc);
create index idx_run_week on run(week_id, created_at desc);

create index idx_player_model_user on player_model(user_id);

create index idx_unlock_user on unlock(user_id);

create index idx_mission_progress_user on mission_progress(user_id, day_id);

-- Row Level Security (RLS) policies

-- Enable RLS on all tables
alter table app_user enable row level security;
alter table season enable row level security;
alter table season_score enable row level security;
alter table run enable row level security;
alter table player_model enable row level security;
alter table unlock enable row level security;
alter table mission_progress enable row level security;

-- app_user: users can read their own data, insert on signup
create policy "Users can view own profile"
  on app_user for select
  using (auth.uid() = id);

create policy "Users can update own profile"
  on app_user for update
  using (auth.uid() = id);

create policy "Anyone can insert user (for anonymous signup)"
  on app_user for insert
  with check (true);

-- season: everyone can read (public leaderboard data)
create policy "Seasons are public"
  on season for select
  using (true);

-- season_score: everyone can read, users can upsert their own
create policy "Leaderboard is public"
  on season_score for select
  using (true);

create policy "Users can upsert own scores"
  on season_score for insert
  with check (auth.uid() = user_id);

create policy "Users can update own scores"
  on season_score for update
  using (auth.uid() = user_id);

-- run: users can insert and read their own runs
create policy "Users can view own runs"
  on run for select
  using (auth.uid() = user_id);

create policy "Users can insert own runs"
  on run for insert
  with check (auth.uid() = user_id);

-- player_model: users can read/write their own model
create policy "Users can view own player model"
  on player_model for select
  using (auth.uid() = user_id);

create policy "Users can upsert own player model"
  on player_model for insert
  with check (auth.uid() = user_id);

create policy "Users can update own player model"
  on player_model for update
  using (auth.uid() = user_id);

-- unlock: users can view their own unlocks
create policy "Users can view own unlocks"
  on unlock for select
  using (auth.uid() = user_id);

create policy "Users can insert own unlocks"
  on unlock for insert
  with check (auth.uid() = user_id);

-- mission_progress: users can view/update their own progress
create policy "Users can view own mission progress"
  on mission_progress for select
  using (auth.uid() = user_id);

create policy "Users can upsert own mission progress"
  on mission_progress for insert
  with check (auth.uid() = user_id);

create policy "Users can update own mission progress"
  on mission_progress for update
  using (auth.uid() = user_id);

-- Helper function: get current active season
create or replace function get_current_season()
returns table (
  id uuid,
  week_id int,
  seed bigint,
  starts_at timestamptz,
  ends_at timestamptz
)
language sql
stable
as $$
  select id, week_id, seed, starts_at, ends_at
  from season
  where now() between starts_at and ends_at
  order by starts_at desc
  limit 1;
$$;

-- Helper function: calculate ISO week ID (YYYYWW format)
create or replace function calculate_week_id(ts timestamptz default now())
returns int
language sql
immutable
as $$
  select (extract(year from ts)::int * 100) + extract(week from ts)::int;
$$;

-- Helper function: check if run timing is plausible
create or replace function is_plausible_run(
  floors int,
  runtime_seconds real,
  avg_reaction_ms int
)
returns boolean
language plpgsql
as $$
declare
  min_time_per_floor constant real := 5.0; -- minimum 5s per floor
  max_time_per_floor constant real := 10.0; -- maximum 10s per floor
  min_reaction constant int := 100; -- minimum 100ms human reaction time
  theoretical_min real;
  theoretical_max real;
begin
  theoretical_min := floors * min_time_per_floor;
  theoretical_max := floors * max_time_per_floor;

  -- Check runtime bounds
  if runtime_seconds < theoretical_min or runtime_seconds > theoretical_max then
    return false;
  end if;

  -- Check reaction time
  if avg_reaction_ms < min_reaction then
    return false;
  end if;

  return true;
end;
$$;
