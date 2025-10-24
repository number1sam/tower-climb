/**
 * Supabase Client for Tower Climb
 * Handles authentication and backend communication
 */

class SupabaseClient {
    constructor() {
        // You'll need to set these after deploying your backend
        this.supabaseUrl = ''; // Set in config.js or leave empty for offline mode
        this.supabaseAnonKey = '';
        this.userId = null;
        this.accessToken = null;
    }

    // Check if backend is configured
    isConfigured() {
        return this.supabaseUrl && this.supabaseAnonKey;
    }

    // Anonymous sign in
    async signInAnonymously() {
        if (!this.isConfigured()) {
            console.log('[Supabase] Running in offline mode');
            this.userId = 'offline-user-' + Date.now();
            return { userId: this.userId };
        }

        try {
            const anonymousId = this.generateUUID();
            const response = await fetch(`${this.supabaseUrl}/auth/v1/signup`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'apikey': this.supabaseAnonKey
                },
                body: JSON.stringify({
                    email: `${anonymousId}@anon.tower-climb.com`,
                    password: anonymousId
                })
            });

            const data = await response.json();

            if (data.access_token) {
                this.accessToken = data.access_token;
                this.userId = data.user?.id;
                console.log('[Supabase] Authenticated:', this.userId);
                return { userId: this.userId };
            }

            throw new Error('Authentication failed');
        } catch (error) {
            console.error('[Supabase] Auth error:', error);
            // Fallback to offline mode
            this.userId = 'offline-user-' + Date.now();
            return { userId: this.userId };
        }
    }

    // Start a new run
    async startRun() {
        if (!this.isConfigured()) {
            // Offline mode - generate local seed
            return {
                userId: this.userId,
                weekId: 1,
                seed: String(Date.now()),
                currentBest: this.getLocalBest()
            };
        }

        try {
            const response = await fetch(`${this.supabaseUrl}/functions/v1/start-run`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${this.accessToken}`,
                    'apikey': this.supabaseAnonKey
                }
            });

            return await response.json();
        } catch (error) {
            console.error('[Supabase] Start run error:', error);
            return {
                userId: this.userId,
                weekId: 1,
                seed: String(Date.now()),
                currentBest: this.getLocalBest()
            };
        }
    }

    // Submit run results
    async submitRun(runData) {
        if (!this.isConfigured()) {
            // Offline mode - save to localStorage
            this.saveLocalBest(runData.floors);
            return { success: true, newBest: true };
        }

        try {
            const response = await fetch(`${this.supabaseUrl}/functions/v1/submit-run`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.accessToken}`,
                    'apikey': this.supabaseAnonKey
                },
                body: JSON.stringify(runData)
            });

            const result = await response.json();

            // Also save locally
            this.saveLocalBest(runData.floors);

            return result;
        } catch (error) {
            console.error('[Supabase] Submit run error:', error);
            this.saveLocalBest(runData.floors);
            return { success: true, newBest: true };
        }
    }

    // Get leaderboard
    async getLeaderboard(scope = 'global') {
        if (!this.isConfigured()) {
            return { entries: [], userEntry: null };
        }

        try {
            const response = await fetch(
                `${this.supabaseUrl}/functions/v1/get-leaderboard?scope=${scope}`,
                {
                    headers: {
                        'Authorization': `Bearer ${this.accessToken}`,
                        'apikey': this.supabaseAnonKey
                    }
                }
            );

            return await response.json();
        } catch (error) {
            console.error('[Supabase] Leaderboard error:', error);
            return { entries: [], userEntry: null };
        }
    }

    // Local storage helpers
    getLocalBest() {
        return parseInt(localStorage.getItem('tower-climb-best') || '0');
    }

    saveLocalBest(floors) {
        const current = this.getLocalBest();
        if (floors > current) {
            localStorage.setItem('tower-climb-best', floors.toString());
        }
    }

    generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}
