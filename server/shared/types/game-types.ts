// Shared type definitions for game logic
// These types are used by both client (Unity via code generation) and server (Edge Functions)

export type PatternType = 'tap' | 'swipe' | 'hold' | 'rhythm' | 'tilt' | 'doubleTap';

export type Direction = 'L' | 'R' | 'U' | 'D' | null;

export interface Pattern {
  type: PatternType;
  direction: Direction;
  duration: number; // for hold patterns
  complexity: number; // 1-3, for rhythm patterns (number of taps)
  timeWindow: number; // seconds allowed to complete
  speed: number; // current difficulty multiplier
}

export interface DifficultyConfig {
  v0: number; // base speed
  deltaV: number; // speed increment per floor
  minWindow: number; // minimum time window in seconds
  maxWindow: number; // maximum time window in seconds
  baseWindow: number; // base time window before speed adjustment
  adaptiveEpsilon: number; // extra speed boost for skilled players
  baseWeights: PatternWeights;
}

export interface PatternWeights {
  tap: number;
  swipe: number;
  hold: number;
  rhythm: number;
  tilt: number;
  doubleTap: number;
}

export interface PlayerModel {
  weaknesses: Partial<Record<PatternType, number>>; // 0-1, higher = more mistakes
  last5: FloorStats[]; // last 5 floor performances
}

export interface FloorStats {
  floor: number;
  reactionMs: number;
  success: boolean;
  accuracy: number; // 0-1, perfect = 1.0
}

export interface PatternResult {
  floor: number;
  patternType: PatternType;
  reactionMs: number;
  success: boolean;
  accuracy: number; // 0-1 based on timing precision
  timestamp: number; // client timestamp
}

export interface RunSubmission {
  weekId: number;
  floors: number;
  runtimeSeconds: number;
  avgReactionMs: number;
  breakdown: Record<PatternType, PatternBreakdown>;
  timings: PatternResult[];
  playerModel: PlayerModel;
  clientVersion: string;
}

export interface PatternBreakdown {
  attempts: number;
  perfects: number;
  goods: number;
  misses: number;
  avgReactionMs: number;
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  handle: string;
  bestFloor: number;
  bestReactionMs: number;
  perfectRate: number;
  country?: string;
}

export interface SessionData {
  userId: string;
  weekId: number;
  seed: bigint;
  startsAt: string;
  endsAt: string;
  currentBest?: number;
}
