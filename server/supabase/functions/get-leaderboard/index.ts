// Edge Function: get-leaderboard
// Returns paginated leaderboard for a given week, with optional filters

import { serve } from 'https://deno.land/std@0.168.0/http/server.ts';
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';
import type { LeaderboardEntry } from '../../shared/types/game-types.ts';

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
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

    // Parse query parameters
    const url = new URL(req.url);
    const weekId = url.searchParams.get('weekId');
    const scope = url.searchParams.get('scope') || 'global'; // global, country, friends
    const limit = parseInt(url.searchParams.get('limit') || '100');
    const offset = parseInt(url.searchParams.get('offset') || '0');
    const country = url.searchParams.get('country');

    // Get current user (optional for friends scope)
    const {
      data: { user },
    } = await supabaseClient.auth.getUser();

    // Default to current week if not specified
    let targetWeekId = weekId ? parseInt(weekId) : null;

    if (!targetWeekId) {
      const { data: currentSeason } = await supabaseClient
        .rpc('get_current_season')
        .limit(1)
        .single();

      if (!currentSeason) {
        return new Response(JSON.stringify({ error: 'No active season' }), {
          status: 404,
          headers: { ...corsHeaders, 'Content-Type': 'application/json' },
        });
      }

      targetWeekId = currentSeason.week_id;
    }

    // Build base query
    let query = supabaseClient
      .from('season_score')
      .select(
        `
        user_id,
        best_floor,
        best_reaction_ms,
        perfect_rate,
        cheat_flags,
        app_user (
          handle,
          country
        )
      `
      )
      .eq('week_id', targetWeekId)
      .order('best_floor', { ascending: false })
      .range(offset, offset + limit - 1);

    // Apply scope filters
    if (scope === 'country' && country) {
      // Filter by country (requires join)
      // This is a simplified approach; for production, consider a materialized view
      query = query.filter('app_user.country', 'eq', country);
    }

    // Execute query
    const { data: scores, error: scoresError } = await query;

    if (scoresError) {
      console.error('Error fetching leaderboard:', scoresError);
      return new Response(JSON.stringify({ error: 'Failed to fetch leaderboard' }), {
        status: 500,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    // Format response
    const entries: LeaderboardEntry[] = scores.map((score: any, index: number) => ({
      rank: offset + index + 1,
      userId: score.user_id,
      handle: score.app_user?.handle || 'Anonymous',
      bestFloor: score.best_floor,
      bestReactionMs: score.best_reaction_ms,
      perfectRate: score.perfect_rate,
      country: scope === 'country' ? score.app_user?.country : undefined,
    }));

    // Get current user's rank and score if authenticated
    let userEntry: LeaderboardEntry | null = null;
    if (user) {
      const { data: userScore } = await supabaseClient
        .from('season_score')
        .select('best_floor, best_reaction_ms, perfect_rate')
        .eq('user_id', user.id)
        .eq('week_id', targetWeekId)
        .single();

      if (userScore) {
        // Calculate rank
        const { count: betterScores } = await supabaseClient
          .from('season_score')
          .select('*', { count: 'exact', head: true })
          .eq('week_id', targetWeekId)
          .gt('best_floor', userScore.best_floor);

        const { data: userData } = await supabaseClient
          .from('app_user')
          .select('handle, country')
          .eq('id', user.id)
          .single();

        userEntry = {
          rank: (betterScores || 0) + 1,
          userId: user.id,
          handle: userData?.handle || 'You',
          bestFloor: userScore.best_floor,
          bestReactionMs: userScore.best_reaction_ms,
          perfectRate: userScore.perfect_rate,
        };
      }
    }

    return new Response(
      JSON.stringify({
        weekId: targetWeekId,
        scope,
        entries,
        userEntry,
        hasMore: scores.length === limit,
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
