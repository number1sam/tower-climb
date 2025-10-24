// Edge Function: start-run
// Returns current season data (seed, week_id) for client to begin a run

import { serve } from 'https://deno.land/std@0.168.0/http/server.ts';
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';
import type { SessionData } from '../../shared/types/game-types.ts';

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
};

serve(async (req) => {
  // Handle CORS preflight
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders });
  }

  try {
    // Initialize Supabase client
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

    // Ensure user exists in app_user table
    const { error: upsertError } = await supabaseClient
      .from('app_user')
      .upsert(
        {
          id: user.id,
          last_seen_at: new Date().toISOString(),
        },
        {
          onConflict: 'id',
          ignoreDuplicates: false,
        }
      );

    if (upsertError) {
      console.error('Error upserting user:', upsertError);
    }

    // Get current active season
    const { data: seasons, error: seasonError } = await supabaseClient
      .rpc('get_current_season')
      .limit(1);

    if (seasonError) {
      console.error('Error fetching season:', seasonError);
      return new Response(JSON.stringify({ error: 'Failed to fetch season' }), {
        status: 500,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    if (!seasons || seasons.length === 0) {
      return new Response(JSON.stringify({ error: 'No active season' }), {
        status: 404,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    const season = seasons[0];

    // Get user's current best for this week
    const { data: scoreData } = await supabaseClient
      .from('season_score')
      .select('best_floor')
      .eq('user_id', user.id)
      .eq('week_id', season.week_id)
      .single();

    const sessionData: SessionData = {
      userId: user.id,
      weekId: season.week_id,
      seed: season.seed,
      startsAt: season.starts_at,
      endsAt: season.ends_at,
      currentBest: scoreData?.best_floor,
    };

    return new Response(JSON.stringify(sessionData), {
      status: 200,
      headers: { ...corsHeaders, 'Content-Type': 'application/json' },
    });
  } catch (error) {
    console.error('Unexpected error:', error);
    return new Response(JSON.stringify({ error: 'Internal server error' }), {
      status: 500,
      headers: { ...corsHeaders, 'Content-Type': 'application/json' },
    });
  }
});
