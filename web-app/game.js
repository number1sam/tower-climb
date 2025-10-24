/**
 * Tower Climb - Main Game Logic
 * Standalone web version - like Wordle, just open in browser!
 */

class TowerClimbGame {
    constructor() {
        this.supabase = new SupabaseClient();
        this.generator = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);

        // Game state
        this.currentFloor = 0;
        this.currentPattern = null;
        this.patterns = [];
        this.runStartTime = 0;
        this.patternStartTime = 0;
        this.timerInterval = null;
        this.sessionData = null;
        this.runTimings = [];

        // Input tracking
        this.touchStartPos = null;
        this.touchStartTime = 0;
        this.tapCount = 0;
        this.lastTapTime = 0;

        // Initialize
        this.init();
    }

    async init() {
        console.log('🎮 Tower Climb initializing...');

        // Authenticate
        await this.supabase.signInAnonymously();

        // Setup UI
        this.setupEventListeners();

        // Load best scores
        this.updateStats();

        // Hide loading screen
        this.showScreen('home-screen');

        console.log('✅ Game ready!');
    }

    setupEventListeners() {
        // Home screen
        document.getElementById('start-btn').addEventListener('click', () => this.startRun());
        document.getElementById('practice-btn').addEventListener('click', () => this.startPractice());
        document.getElementById('leaderboard-btn').addEventListener('click', () => this.showLeaderboard());

        // Results screen
        document.getElementById('play-again-btn').addEventListener('click', () => this.startRun());
        document.getElementById('home-btn').addEventListener('click', () => this.showScreen('home-screen'));

        // Leaderboard screen
        document.getElementById('close-leaderboard-btn').addEventListener('click', () => this.showScreen('home-screen'));

        // Game input
        const touchArea = document.getElementById('touch-area');

        touchArea.addEventListener('mousedown', (e) => this.handleTouchStart(e.clientX, e.clientY));
        touchArea.addEventListener('mousemove', (e) => this.handleTouchMove(e.clientX, e.clientY));
        touchArea.addEventListener('mouseup', (e) => this.handleTouchEnd(e.clientX, e.clientY));

        touchArea.addEventListener('touchstart', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            this.handleTouchStart(touch.clientX, touch.clientY);
        });

        touchArea.addEventListener('touchmove', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            this.handleTouchMove(touch.clientX, touch.clientY);
        });

        touchArea.addEventListener('touchend', (e) => {
            e.preventDefault();
            const touch = e.changedTouches[0];
            this.handleTouchEnd(touch.clientX, touch.clientY);
        });
    }

    async startRun() {
        console.log('[Game] Starting new run...');

        // Get session data from backend
        this.sessionData = await this.supabase.startRun();
        console.log('[Game] Session data:', this.sessionData);

        // Generate patterns
        this.patterns = this.generator.generateSequence(
            BigInt(this.sessionData.seed),
            1,
            100 // Pre-generate 100 floors
        );

        // Reset state
        this.currentFloor = 0;
        this.runStartTime = Date.now();
        this.runTimings = [];

        // Start first floor
        this.showScreen('game-screen');
        this.nextFloor();
    }

    startPractice() {
        // Practice mode with random seed
        this.sessionData = {
            userId: 'practice',
            weekId: 0,
            seed: String(Date.now()),
            currentBest: 0
        };

        this.patterns = this.generator.generateSequence(BigInt(this.sessionData.seed), 1, 100);
        this.currentFloor = 0;
        this.runStartTime = Date.now();
        this.runTimings = [];

        this.showScreen('game-screen');
        this.nextFloor();
    }

    nextFloor() {
        this.currentFloor++;

        if (this.currentFloor > this.patterns.length) {
            // Shouldn't happen, but handle gracefully
            this.endRun();
            return;
        }

        this.currentPattern = this.patterns[this.currentFloor - 1];
        this.patternStartTime = Date.now();
        this.tapCount = 0;

        // Update UI
        document.getElementById('current-floor').textContent = this.currentFloor;
        this.displayPattern(this.currentPattern);

        // Start timer
        this.startTimer(this.currentPattern.timeWindow);
    }

    displayPattern(pattern) {
        const iconEl = document.getElementById('pattern-icon');
        const textEl = document.getElementById('pattern-text');
        const directionEl = document.getElementById('pattern-direction');

        // Reset
        directionEl.textContent = '';

        switch (pattern.type) {
            case PatternType.TAP:
                iconEl.textContent = '👆';
                textEl.textContent = 'TAP';
                break;

            case PatternType.SWIPE:
                iconEl.textContent = '👉';
                textEl.textContent = 'SWIPE';
                directionEl.textContent = this.getDirectionText(pattern.direction);
                break;

            case PatternType.HOLD:
                iconEl.textContent = '✋';
                textEl.textContent = 'HOLD';
                directionEl.textContent = `${pattern.duration.toFixed(1)}s`;
                break;

            case PatternType.RHYTHM:
                iconEl.textContent = '👏';
                textEl.textContent = 'RHYTHM';
                directionEl.textContent = `${pattern.complexity} TAPS`;
                break;

            case PatternType.DOUBLE_TAP:
                iconEl.textContent = '👆👆';
                textEl.textContent = 'DOUBLE TAP';
                break;
        }
    }

    getDirectionText(direction) {
        const arrows = {
            'L': '← LEFT',
            'R': '→ RIGHT',
            'U': '↑ UP',
            'D': '↓ DOWN'
        };
        return arrows[direction] || '';
    }

    startTimer(duration) {
        const timerBar = document.getElementById('timer-bar');
        timerBar.style.transition = 'none';
        timerBar.style.width = '100%';

        // Trigger reflow
        timerBar.offsetHeight;

        setTimeout(() => {
            timerBar.style.transition = `width ${duration}s linear`;
            timerBar.style.width = '0%';
        }, 10);

        // Auto-fail after time window
        if (this.timerInterval) clearTimeout(this.timerInterval);
        this.timerInterval = setTimeout(() => {
            this.patternFailed('Time out!');
        }, duration * 1000);
    }

    handleTouchStart(x, y) {
        this.touchStartPos = { x, y };
        this.touchStartTime = Date.now();
    }

    handleTouchMove(x, y) {
        // For swipe detection
    }

    handleTouchEnd(x, y) {
        if (!this.currentPattern || !this.touchStartPos) return;

        const reactionMs = Date.now() - this.patternStartTime;
        const touchDuration = Date.now() - this.touchStartTime;

        const dx = x - this.touchStartPos.x;
        const dy = y - this.touchStartPos.y;
        const distance = Math.sqrt(dx * dx + dy * dy);

        // Detect input type
        let inputType = null;
        let inputDirection = null;

        // Check for double tap
        if (Date.now() - this.lastTapTime < 300) {
            this.tapCount++;
        } else {
            this.tapCount = 1;
        }
        this.lastTapTime = Date.now();

        if (distance < 20 && touchDuration < 200) {
            // Tap or double tap
            if (this.tapCount >= 2) {
                inputType = PatternType.DOUBLE_TAP;
                this.tapCount = 0;
            } else {
                inputType = PatternType.TAP;
            }
        } else if (distance > 50) {
            // Swipe
            inputType = PatternType.SWIPE;
            if (Math.abs(dx) > Math.abs(dy)) {
                inputDirection = dx > 0 ? Direction.RIGHT : Direction.LEFT;
            } else {
                inputDirection = dy > 0 ? Direction.DOWN : Direction.UP;
            }
        } else if (touchDuration > 500) {
            // Hold
            inputType = PatternType.HOLD;
        }

        // Validate against pattern
        this.validateInput(inputType, inputDirection, reactionMs);

        this.touchStartPos = null;
    }

    validateInput(inputType, inputDirection, reactionMs) {
        const pattern = this.currentPattern;

        // Check if input matches pattern
        let success = false;

        if (pattern.type === PatternType.TAP && inputType === PatternType.TAP) {
            success = true;
        } else if (pattern.type === PatternType.SWIPE && inputType === PatternType.SWIPE) {
            success = (pattern.direction === inputDirection);
        } else if (pattern.type === PatternType.HOLD && inputType === PatternType.HOLD) {
            success = true;
        } else if (pattern.type === PatternType.DOUBLE_TAP && inputType === PatternType.DOUBLE_TAP) {
            success = true;
        } else if (pattern.type === PatternType.RHYTHM && inputType === PatternType.TAP) {
            // Simplified rhythm - just need to tap
            success = true;
        }

        // Calculate accuracy
        const timeWindow = pattern.timeWindow * 1000; // ms
        const accuracy = Math.max(0, 1 - (reactionMs / timeWindow));

        // Record timing
        this.runTimings.push({
            floor: this.currentFloor,
            patternType: pattern.type,
            reactionMs,
            success,
            accuracy
        });

        if (success) {
            this.patternSuccess(reactionMs, accuracy);
        } else {
            this.patternFailed('Wrong input!');
        }
    }

    patternSuccess(reactionMs, accuracy) {
        clearTimeout(this.timerInterval);

        // Show feedback
        const feedbackEl = document.getElementById('feedback');
        if (accuracy > 0.9) {
            feedbackEl.textContent = '✨ PERFECT!';
            feedbackEl.className = 'feedback show perfect';
        } else if (accuracy > 0.7) {
            feedbackEl.textContent = '✓ GOOD';
            feedbackEl.className = 'feedback show good';
        } else {
            feedbackEl.textContent = '✓ OK';
            feedbackEl.className = 'feedback show ok';
        }

        setTimeout(() => {
            feedbackEl.className = 'feedback';
            this.nextFloor();
        }, 500);
    }

    patternFailed(reason) {
        clearTimeout(this.timerInterval);

        // Show feedback
        const feedbackEl = document.getElementById('feedback');
        feedbackEl.textContent = `✗ ${reason}`;
        feedbackEl.className = 'feedback show fail';

        setTimeout(() => {
            feedbackEl.className = 'feedback';
            this.endRun();
        }, 1000);
    }

    async endRun() {
        const finalFloor = Math.max(1, this.currentFloor - 1);
        const runtimeSeconds = (Date.now() - this.runStartTime) / 1000;

        // Calculate stats
        const avgReactionMs = this.runTimings.length > 0
            ? this.runTimings.reduce((sum, t) => sum + t.reactionMs, 0) / this.runTimings.length
            : 0;

        const perfectCount = this.runTimings.filter(t => t.accuracy >= 0.9).length;
        const perfectRate = this.runTimings.length > 0
            ? (perfectCount / this.runTimings.length) * 100
            : 0;

        // Submit to backend (if not practice mode)
        let isNewBest = false;
        if (this.sessionData.weekId > 0) {
            const result = await this.supabase.submitRun({
                weekId: this.sessionData.weekId,
                floors: finalFloor,
                runtimeSeconds,
                avgReactionMs,
                timings: this.runTimings,
                breakdown: {},
                playerModel: { weaknesses: {}, last5: [] },
                clientVersion: '1.0.0-web'
            });
            isNewBest = result.newBest;
        }

        // Show results
        this.showResults(finalFloor, runtimeSeconds, avgReactionMs, perfectRate, isNewBest);

        // Update stats
        this.updateStats();
    }

    showResults(floors, runtimeSeconds, avgReactionMs, perfectRate, isNewBest) {
        document.getElementById('final-floor').textContent = floors;
        document.getElementById('final-time').textContent = runtimeSeconds.toFixed(1) + 's';
        document.getElementById('final-reaction').textContent = Math.round(avgReactionMs) + 'ms';
        document.getElementById('final-perfect').textContent = perfectRate.toFixed(0) + '%';

        const newBestEl = document.getElementById('new-best');
        newBestEl.style.display = isNewBest ? 'block' : 'none';

        this.showScreen('results-screen');
    }

    updateStats() {
        const best = this.supabase.getLocalBest();
        document.getElementById('best-floor').textContent = best;
        document.getElementById('week-best').textContent = best;
    }

    async showLeaderboard() {
        this.showScreen('leaderboard-screen');

        const listEl = document.getElementById('leaderboard-list');
        listEl.innerHTML = '<div class="loading">Loading leaderboard...</div>';

        const data = await this.supabase.getLeaderboard('global');

        if (data.entries.length === 0) {
            listEl.innerHTML = '<div class="empty">No entries yet. Be the first!</div>';
            return;
        }

        listEl.innerHTML = data.entries.map((entry, index) => `
            <div class="leaderboard-entry">
                <span class="rank">#${entry.rank || index + 1}</span>
                <span class="handle">${entry.handle || 'Player'}</span>
                <span class="floor">Floor ${entry.bestFloor}</span>
            </div>
        `).join('');
    }

    showScreen(screenId) {
        document.querySelectorAll('.screen').forEach(screen => {
            screen.classList.remove('active');
        });
        document.getElementById(screenId).classList.add('active');
    }
}

// Start game when page loads
window.addEventListener('DOMContentLoaded', () => {
    window.game = new TowerClimbGame();
});
