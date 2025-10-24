// Edge Function: weekly-seed-reset
// Cron job to create new weekly season every Monday at 00:00 UTC
// Invoke via: curl -X POST https://your-project.supabase.co/functions/v1/weekly-seed-reset \
//   -H "Authorization: Bearer SERVICE_ROLE_KEY"

import { serve } from 'https://deno.land/std@0.168.0/http/server.ts';
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2';

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
};

serve(async (req) => {
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders });
  }

  try {
    // This function should only be called by cron or admin
    const authHeader = req.headers.get('Authorization');
    const serviceRoleKey = Deno.env.get('SUPABASE_SERVICE_ROLE_KEY');

    if (!authHeader || !authHeader.includes(serviceRoleKey || '')) {
      return new Response(JSON.stringify({ error: 'Unauthorized' }), {
        status: 401,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    const supabaseClient = createClient(
      Deno.env.get('SUPABASE_URL') ?? '',
      serviceRoleKey ?? '',
      {
        auth: {
          autoRefreshToken: false,
          persistSession: false,
        },
      }
    );

    // Calculate week_id for current week (YYYYWW format)
    const now = new Date();
    const { data: weekId } = await supabaseClient.rpc('calculate_week_id', { ts: now.toISOString() });

    if (!weekId) {
      throw new Error('Failed to calculate week ID');
    }

    // Check if season already exists for this week
    const { data: existing } = await supabaseClient
      .from('season')
      .select('week_id')
      .eq('week_id', weekId)
      .single();

    if (existing) {
      return new Response(
        JSON.stringify({
          message: 'Season already exists for this week',
          weekId,
        }),
        {
          status: 200,
          headers: { ...corsHeaders, 'Content-Type': 'application/json' },
        }
      );
    }

    // Generate cryptographically secure random seed
    const seedArray = new BigInt64Array(1);
    crypto.getRandomValues(seedArray);
    const seed = seedArray[0];

    // Calculate start and end times (Monday 00:00 UTC to next Monday 00:00 UTC)
    const startOfWeek = getStartOfWeek(now);
    const endOfWeek = new Date(startOfWeek);
    endOfWeek.setDate(endOfWeek.getDate() + 7);

    // Insert new season
    const { data: newSeason, error: insertError } = await supabaseClient
      .from('season')
      .insert({
        week_id: weekId,
        seed: seed.toString(), // BigInt as string for JSON compatibility
        starts_at: startOfWeek.toISOString(),
        ends_at: endOfWeek.toISOString(),
      })
      .select()
      .single();

    if (insertError) {
      console.error('Error creating season:', insertError);
      return new Response(JSON.stringify({ error: 'Failed to create season' }), {
        status: 500,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    console.log('Created new season:', newSeason);

    return new Response(
      JSON.stringify({
        message: 'New season created',
        season: newSeason,
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

// Helper: Get start of current ISO week (Monday 00:00 UTC)
function getStartOfWeek(date: Date): Date {
  const d = new Date(date);
  const day = d.getUTCDay();
  const diff = (day === 0 ? -6 : 1) - day; // Adjust to Monday
  d.setUTCDate(d.getUTCDate() + diff);
  d.setUTCHours(0, 0, 0, 0);
  return d;
}
