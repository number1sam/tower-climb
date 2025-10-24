/**
 * Deterministic pattern generator
 * CRITICAL: Must produce identical results to server for anti-cheat validation
 */

const PatternType = {
    TAP: 'tap',
    SWIPE: 'swipe',
    HOLD: 'hold',
    RHYTHM: 'rhythm',
    TILT: 'tilt',
    DOUBLE_TAP: 'doubleTap'
};

const Direction = {
    LEFT: 'L',
    RIGHT: 'R',
    UP: 'U',
    DOWN: 'D',
    NONE: null
};

const ALL_PATTERN_TYPES = [
    PatternType.TAP,
    PatternType.SWIPE,
    PatternType.HOLD,
    PatternType.RHYTHM,
    PatternType.DOUBLE_TAP
];

const SWIPE_DIRECTIONS = [
    Direction.LEFT,
    Direction.RIGHT,
    Direction.UP,
    Direction.DOWN
];

class PatternGenerator {
    constructor(config) {
        this.config = config || DEFAULT_DIFFICULTY_CONFIG;
    }

    // Main generation function - deterministic based on seed and floor
    generate(seed, floor, playerModel = null) {
        // Mix seed with floor for unique RNG state per floor
        const floorSeed = BigInt(seed) ^ (BigInt(floor) * BigInt(0x9e3779b9));
        const rng = new SeededRandom(floorSeed);

        // Calculate pattern weights (adapt based on player weaknesses)
        const weights = this.calculateWeights(playerModel);

        // Choose pattern type
        const patternType = weightedChoice(ALL_PATTERN_TYPES, weights, rng);

        // Calculate speed and time window
        const speed = this.calculateSpeed(floor, playerModel);
        const timeWindow = this.calculateTimeWindow(speed);

        // Build pattern
        const pattern = {
            type: patternType,
            direction: Direction.NONE,
            duration: 0,
            complexity: 1,
            timeWindow: timeWindow,
            speed: speed
        };

        // Set type-specific properties
        switch (patternType) {
            case PatternType.SWIPE:
                pattern.direction = SWIPE_DIRECTIONS[rng.nextInt(4)];
                break;

            case PatternType.HOLD:
                // Hold duration: 0.5s to 1.5s based on floor
                pattern.duration = 0.5 + Math.min(floor / 50, 1.0);
                break;

            case PatternType.RHYTHM:
                // Complexity: 2-4 taps, increases with floor
                pattern.complexity = 2 + Math.min(Math.floor(floor / 15), 2);
                break;

            case PatternType.DOUBLE_TAP:
                pattern.complexity = 2;
                break;

            case PatternType.TAP:
            default:
                // Simple tap - no extra properties
                break;
        }

        // Apply cooldown pattern every N floors to prevent fatigue deaths
        if (this.isCooldownFloor(floor)) {
            pattern.type = PatternType.TAP;
            pattern.direction = Direction.NONE;
            pattern.timeWindow = this.config.maxWindow; // generous window
        }

        return pattern;
    }

    // Calculate pattern weights with player weakness bias
    calculateWeights(playerModel) {
        const base = this.config.baseWeights;
        const weights = [];

        for (const type of ALL_PATTERN_TYPES) {
            let weight = base[type] || 0.1;

            // Increase weight for patterns player struggles with
            if (playerModel?.weaknesses?.[type]) {
                const weakness = playerModel.weaknesses[type];
                // Increase spawn rate by up to 50% for weak patterns
                weight *= 1 + weakness * 0.5;
            }

            weights.push(weight);
        }

        return weights;
    }

    // Calculate speed based on floor and recent player performance
    calculateSpeed(floor, playerModel) {
        const { v0, deltaV, adaptiveEpsilon } = this.config;

        // Base speed progression
        let speed = v0 + floor * deltaV;

        // Adaptive difficulty boost for skilled players
        if (playerModel?.last5 && playerModel.last5.length >= 3) {
            const recent = playerModel.last5.slice(-5);
            const avgAccuracy = recent.reduce((sum, s) => sum + s.accuracy, 0) / recent.length;
            const avgReaction = recent.reduce((sum, s) => sum + s.reactionMs, 0) / recent.length;

            // If player is performing well (>80% accuracy, <400ms reaction), increase speed
            if (avgAccuracy > 0.8 && avgReaction < 400) {
                speed += adaptiveEpsilon;
            }
        }

        return speed;
    }

    // Calculate time window based on speed
    calculateTimeWindow(speed) {
        const { baseWindow, minWindow, maxWindow } = this.config;
        const window = baseWindow / speed;
        return Math.max(minWindow, Math.min(maxWindow, window));
    }

    // Check if this floor should be a cooldown (easier pattern)
    isCooldownFloor(floor) {
        // Every 20 floors, give a break
        return floor > 0 && floor % 20 === 0;
    }

    // Pre-generate patterns for multiple floors
    generateSequence(seed, startFloor, count, playerModel = null) {
        const patterns = [];
        for (let i = 0; i < count; i++) {
            const floor = startFloor + i;
            patterns.push(this.generate(seed, floor, playerModel));
        }
        return patterns;
    }
}

// Default difficulty configuration
const DEFAULT_DIFFICULTY_CONFIG = {
    v0: 1.0,
    deltaV: 0.05,
    minWindow: 0.3,
    maxWindow: 2.0,
    baseWindow: 1.5,
    adaptiveEpsilon: 0.1,
    baseWeights: {
        [PatternType.TAP]: 0.3,
        [PatternType.SWIPE]: 0.3,
        [PatternType.HOLD]: 0.2,
        [PatternType.RHYTHM]: 0.1,
        [PatternType.DOUBLE_TAP]: 0.1
    }
};
