// Edge Function: submit-run
// Validates run data with anti-cheat and updates leaderboard

import { serve } from 'https://deno.land/std@0.168.0/http/server.ts';
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';
import { validatePatterns, DEFAULT_DIFFICULTY_CONFIG } from '../../shared/utils/pattern-generator.ts';
import type { RunSubmission, PatternResult } from '../../shared/types/game-types.ts';

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
};

// Anti-cheat flags (bitfield)
const CHEAT_FLAGS = {
  NONE: 0,
  IMPOSSIBLE_TIMING: 1 << 0, // Reaction times too fast
  IMPOSSIBLE_FLOORS: 1 << 1, // Too many floors for runtime
  PATTERN_MISMATCH: 1 << 2, // Client patterns don't match server
  SUSPICIOUS_DISTRIBUTION: 1 << 3, // Timing distribution looks automated
  REPLAY_ATTACK: 1 << 4, // Duplicate submission detected
};

serve(async (req) => {
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders });
  }

  try {
    const supabaseClient = createClient(
      Deno.env.get('SUPABASE_URL') ?? '',
      Deno.env.get('SUPABASE_ANON_KEY') ?? '',
      {
        global: {
          headers: { Authorization: req.headers.get('Authorization')! },
        },
      }
    );

    // Get authenticated user
    const {
      data: { user },
      error: userError,
    } = await supabaseClient.auth.getUser();

    if (userError || !user) {
      return new Response(JSON.stringify({ error: 'Unauthorized' }), {
        status: 401,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    // Parse submission
    const submission: RunSubmission = await req.json();

    // Validate required fields
    if (!submission.weekId || !submission.floors || !submission.timings) {
      return new Response(JSON.stringify({ error: 'Invalid submission data' }), {
        status: 400,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    // Get season data
    const { data: season, error: seasonError } = await supabaseClient
      .from('season')
      .select('seed, week_id')
      .eq('week_id', submission.weekId)
      .single();

    if (seasonError || !season) {
      return new Response(JSON.stringify({ error: 'Invalid season' }), {
        status: 400,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    // Run anti-cheat validation
    let cheatFlags = CHEAT_FLAGS.NONE;

    // 1. Check plausibility using database function
    const { data: plausible } = await supabaseClient.rpc('is_plausible_run', {
      floors: submission.floors,
      runtime_seconds: submission.runtimeSeconds,
      avg_reaction_ms: submission.avgReactionMs,
    });

    if (!plausible) {
      cheatFlags |= CHEAT_FLAGS.IMPOSSIBLE_FLOORS;
    }

    // 2. Validate timing distribution
    const reactionTimes = submission.timings.map((t) => t.reactionMs);
    if (hasImpossibleTiming(reactionTimes)) {
      cheatFlags |= CHEAT_FLAGS.IMPOSSIBLE_TIMING;
    }

    if (hasSuspiciousDistribution(reactionTimes)) {
      cheatFlags |= CHEAT_FLAGS.SUSPICIOUS_DISTRIBUTION;
    }

    // 3. Validate patterns match server generation
    // NOTE: For full validation, we'd regenerate patterns and compare
    // For now, we'll do a simpler check
    const expectedPatterns = validatePatterns(
      BigInt(season.seed),
      submission.floors,
      DEFAULT_DIFFICULTY_CONFIG,
      submission.playerModel
    );

    // Check pattern count matches
    if (submission.timings.length !== submission.floors) {
      cheatFlags |= CHEAT_FLAGS.PATTERN_MISMATCH;
    }

    // 4. Check for replay attacks (duplicate submissions within short time)
    const { data: recentRuns } = await supabaseClient
      .from('run')
      .select('floors, avg_reaction_ms')
      .eq('user_id', user.id)
      .eq('week_id', submission.weekId)
      .gte('created_at', new Date(Date.now() - 60000).toISOString()) // last 60s
      .limit(5);

    if (recentRuns && recentRuns.length > 0) {
      const isDuplicate = recentRuns.some(
        (r) =>
          r.floors === submission.floors &&
          Math.abs(r.avg_reaction_ms - submission.avgReactionMs) < 10
      );
      if (isDuplicate) {
        cheatFlags |= CHEAT_FLAGS.REPLAY_ATTACK;
      }
    }

    // Calculate stats
    const perfectRate =
      submission.timings.filter((t) => t.accuracy >= 0.95).length / submission.timings.length;

    // Insert run record
    const { error: runError } = await supabaseClient.from('run').insert({
      user_id: user.id,
      week_id: submission.weekId,
      floors: submission.floors,
      avg_reaction_ms: submission.avgReactionMs,
      runtime_seconds: submission.runtimeSeconds,
      breakdown: submission.breakdown,
      timings: submission.timings,
      client_version: submission.clientVersion,
    });

    if (runError) {
      console.error('Error inserting run:', runError);
    }

    // Update leaderboard (only if not flagged for major cheats)
    const shouldUpdateLeaderboard =
      (cheatFlags & (CHEAT_FLAGS.IMPOSSIBLE_TIMING | CHEAT_FLAGS.IMPOSSIBLE_FLOORS)) === 0;

    if (shouldUpdateLeaderboard) {
      // Get current best
      const { data: currentScore } = await supabaseClient
        .from('season_score')
        .select('best_floor')
        .eq('user_id', user.id)
        .eq('week_id', submission.weekId)
        .single();

      // Only update if this is better
      if (!currentScore || submission.floors > currentScore.best_floor) {
        const { error: scoreError } = await supabaseClient
          .from('season_score')
          .upsert({
            user_id: user.id,
            week_id: submission.weekId,
            best_floor: submission.floors,
            best_reaction_ms: submission.avgReactionMs,
            perfect_rate: perfectRate,
            cheat_flags: cheatFlags,
            breakdown: submission.breakdown,
          });

        if (scoreError) {
          console.error('Error updating score:', scoreError);
        }
      }
    }

    // Update player model
    const { error: modelError } = await supabaseClient.from('player_model').upsert({
      user_id: user.id,
      weaknesses: submission.playerModel.weaknesses,
      last_5_floors: submission.playerModel.last5,
    });

    if (modelError) {
      console.error('Error updating player model:', modelError);
    }

    // Check for new unlocks
    const unlocks = await checkUnlocks(submission.floors, user.id, supabaseClient);

    return new Response(
      JSON.stringify({
        success: true,
        cheatFlags,
        newBest: shouldUpdateLeaderboard,
        unlocks,
      }),
      {
        status: 200,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      }
    );
  } catch (error) {
    console.error('Unexpected error:', error);
    return new Response(JSON.stringify({ error: 'Internal server error' }), {
      status: 500,
      headers: { ...corsHeaders, 'Content-Type': 'application/json' },
    });
  }
});

// Anti-cheat helper: check for impossible reaction times
function hasImpossibleTiming(reactionTimes: number[]): boolean {
  const MIN_HUMAN_REACTION = 100; // ms
  return reactionTimes.some((t) => t < MIN_HUMAN_REACTION && t > 0);
}

// Anti-cheat helper: check for bot-like timing consistency
function hasSuspiciousDistribution(reactionTimes: number[]): boolean {
  if (reactionTimes.length < 10) return false;

  // Calculate standard deviation
  const mean = reactionTimes.reduce((sum, t) => sum + t, 0) / reactionTimes.length;
  const variance =
    reactionTimes.reduce((sum, t) => sum + Math.pow(t - mean, 2), 0) / reactionTimes.length;
  const stdDev = Math.sqrt(variance);

  // Humans have variation; bots are too consistent
  // If stdDev is less than 10ms over many samples, likely automated
  return stdDev < 10 && reactionTimes.length > 20;
}

// Check for milestone unlocks
async function checkUnlocks(
  floors: number,
  userId: string,
  supabaseClient: any
): Promise<string[]> {
  const unlocks: string[] = [];
  const milestones: Record<number, string> = {
    10: 'theme_retro',
    20: 'theme_neon',
    30: 'sfx_pack_cyber',
    50: 'theme_minimal',
    100: 'sfx_pack_orchestral',
  };

  // Check if user reached any new milestones
  for (const [floor, itemId] of Object.entries(milestones)) {
    if (floors >= parseInt(floor)) {
      // Check if already unlocked
      const { data: existing } = await supabaseClient
        .from('unlock')
        .select('item_id')
        .eq('user_id', userId)
        .eq('item_id', itemId)
        .single();

      if (!existing) {
        // Grant unlock
        await supabaseClient.from('unlock').insert({
          user_id: userId,
          item_id: itemId,
        });
        unlocks.push(itemId);
      }
    }
  }

  return unlocks;
}
