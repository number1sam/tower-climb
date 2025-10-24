// Deterministic pattern generator for server-side validation and client-side gameplay
// CRITICAL: Must produce identical results on client and server for anti-cheat

import { SeededRandom, weightedChoice } from './prng.ts';
import {
  Pattern,
  PatternType,
  Direction,
  DifficultyConfig,
  PlayerModel,
  PatternWeights,
} from '../types/game-types.ts';

const ALL_PATTERN_TYPES: PatternType[] = [
  'tap',
  'swipe',
  'hold',
  'rhythm',
  'tilt',
  'doubleTap',
];

const SWIPE_DIRECTIONS: Direction[] = ['L', 'R', 'U', 'D'];

export class PatternGenerator {
  constructor(private config: DifficultyConfig) {}

  // Main generation function - deterministic based on seed and floor
  generate(
    seed: bigint,
    floor: number,
    playerModel?: PlayerModel
  ): Pattern {
    // Mix seed with floor for unique RNG state per floor
    const floorSeed = BigInt(seed) ^ BigInt(floor) * BigInt(0x9e3779b9);
    const rng = new SeededRandom(floorSeed);

    // Calculate pattern weights (adapt based on player weaknesses)
    const weights = this.calculateWeights(playerModel);

    // Choose pattern type
    const patternType = weightedChoice(ALL_PATTERN_TYPES, weights, rng);

    // Calculate speed and time window
    const speed = this.calculateSpeed(floor, playerModel);
    const timeWindow = this.calculateTimeWindow(speed);

    // Build pattern
    const pattern: Pattern = {
      type: patternType,
      direction: null,
      duration: 0,
      complexity: 1,
      timeWindow,
      speed,
    };

    // Set type-specific properties
    switch (patternType) {
      case 'swipe':
        pattern.direction = SWIPE_DIRECTIONS[rng.nextInt(4)];
        break;

      case 'hold':
        // Hold duration: 0.5s to 1.5s based on floor
        pattern.duration = 0.5 + Math.min(floor / 50, 1.0);
        break;

      case 'rhythm':
        // Complexity: 2-4 taps, increases with floor
        pattern.complexity = 2 + Math.min(Math.floor(floor / 15), 2);
        break;

      case 'tilt':
        // Tilt direction
        pattern.direction = SWIPE_DIRECTIONS[rng.nextInt(4)];
        break;

      case 'doubleTap':
        pattern.complexity = 2;
        break;

      case 'tap':
      default:
        // Simple tap - no extra properties
        break;
    }

    // Apply cooldown pattern every N floors to prevent fatigue deaths
    if (this.isCooldownFloor(floor)) {
      pattern.type = 'tap';
      pattern.direction = null;
      pattern.timeWindow = this.config.maxWindow; // generous window
    }

    return pattern;
  }

  // Calculate pattern weights with player weakness bias
  private calculateWeights(playerModel?: PlayerModel): number[] {
    const base = this.config.baseWeights;
    const weights: number[] = [];

    for (const type of ALL_PATTERN_TYPES) {
      let weight = base[type] || 0.1; // default weight for new types

      // Increase weight for patterns player struggles with
      if (playerModel?.weaknesses?.[type]) {
        const weakness = playerModel.weaknesses[type]!;
        // weakness is 0-1, where 1 = many mistakes
        // Increase spawn rate by up to 50% for weak patterns
        weight *= 1 + weakness * 0.5;
      }

      weights.push(weight);
    }

    return weights;
  }

  // Calculate speed based on floor and recent player performance
  private calculateSpeed(floor: number, playerModel?: PlayerModel): number {
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
  private calculateTimeWindow(speed: number): number {
    const { baseWindow, minWindow, maxWindow } = this.config;
    const window = baseWindow / speed;
    return Math.max(minWindow, Math.min(maxWindow, window));
  }

  // Check if this floor should be a cooldown (easier pattern)
  private isCooldownFloor(floor: number): boolean {
    // Every 20 floors, give a break
    return floor > 0 && floor % 20 === 0;
  }
}

// Validate that client-submitted patterns match expected patterns
export function validatePatterns(
  seed: bigint,
  floors: number,
  config: DifficultyConfig,
  playerModel?: PlayerModel
): Pattern[] {
  const generator = new PatternGenerator(config);
  const patterns: Pattern[] = [];

  for (let floor = 1; floor <= floors; floor++) {
    patterns.push(generator.generate(seed, floor, playerModel));
  }

  return patterns;
}

// Default difficulty configuration
export const DEFAULT_DIFFICULTY_CONFIG: DifficultyConfig = {
  v0: 1.0,
  deltaV: 0.05,
  minWindow: 0.3,
  maxWindow: 2.0,
  baseWindow: 1.5,
  adaptiveEpsilon: 0.1,
  baseWeights: {
    tap: 0.3,
    swipe: 0.3,
    hold: 0.2,
    rhythm: 0.1,
    tilt: 0.05,
    doubleTap: 0.05,
  },
};
