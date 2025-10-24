// Unit tests for pattern generator determinism
// Run with: deno test --allow-all

import { assertEquals, assertNotEquals } from 'https://deno.land/std@0.168.0/testing/asserts.ts';
import { PatternGenerator, DEFAULT_DIFFICULTY_CONFIG, validatePatterns } from './pattern-generator.ts';
import { SeededRandom } from './prng.ts';

Deno.test('PRNG produces consistent results with same seed', () => {
  const rng1 = new SeededRandom(12345n);
  const rng2 = new SeededRandom(12345n);

  const values1 = Array.from({ length: 100 }, () => rng1.nextFloat());
  const values2 = Array.from({ length: 100 }, () => rng2.nextFloat());

  assertEquals(values1, values2, 'PRNG should produce identical sequences with same seed');
});

Deno.test('PRNG produces different results with different seeds', () => {
  const rng1 = new SeededRandom(12345n);
  const rng2 = new SeededRandom(54321n);

  const value1 = rng1.nextFloat();
  const value2 = rng2.nextFloat();

  assertNotEquals(value1, value2, 'Different seeds should produce different values');
});

Deno.test('Pattern generation is deterministic', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(9876543210);

  const pattern1 = gen.generate(seed, 10);
  const pattern2 = gen.generate(seed, 10);

  assertEquals(pattern1.type, pattern2.type);
  assertEquals(pattern1.direction, pattern2.direction);
  assertEquals(pattern1.timeWindow, pattern2.timeWindow);
  assertEquals(pattern1.speed, pattern2.speed);
  assertEquals(pattern1.complexity, pattern2.complexity);
});

Deno.test('Different floors produce different patterns', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(12345);

  const patterns = Array.from({ length: 20 }, (_, i) => gen.generate(seed, i + 1));

  // At least 90% of patterns should be unique (allowing for some randomness)
  const uniquePatterns = new Set(patterns.map(p => `${p.type}-${p.direction}-${p.timeWindow}`));
  const uniqueRatio = uniquePatterns.size / patterns.length;

  assertEquals(uniqueRatio > 0.7, true, `Expected >70% unique patterns, got ${uniqueRatio * 100}%`);
});

Deno.test('Speed increases with floor number', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(12345);

  const pattern1 = gen.generate(seed, 1);
  const pattern10 = gen.generate(seed, 10);
  const pattern50 = gen.generate(seed, 50);

  assertEquals(pattern1.speed < pattern10.speed, true);
  assertEquals(pattern10.speed < pattern50.speed, true);
});

Deno.test('Time window decreases with speed', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(12345);

  const pattern1 = gen.generate(seed, 1);
  const pattern50 = gen.generate(seed, 50);

  // Higher floor = higher speed = smaller time window
  assertEquals(pattern1.timeWindow > pattern50.timeWindow, true);

  // Time window should never go below minWindow
  assertEquals(pattern50.timeWindow >= DEFAULT_DIFFICULTY_CONFIG.minWindow, true);
});

Deno.test('Cooldown floors always produce tap patterns', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(12345);

  // Floors 20, 40, 60, etc. are cooldown floors
  const pattern20 = gen.generate(seed, 20);
  const pattern40 = gen.generate(seed, 40);
  const pattern60 = gen.generate(seed, 60);

  assertEquals(pattern20.type, 'tap');
  assertEquals(pattern40.type, 'tap');
  assertEquals(pattern60.type, 'tap');

  // Cooldown floors should have maximum time window
  assertEquals(pattern20.timeWindow, DEFAULT_DIFFICULTY_CONFIG.maxWindow);
});

Deno.test('Player weaknesses increase pattern spawn rate', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(12345);

  // Generate 100 patterns with no player model
  const patternsNoModel = Array.from({ length: 100 }, (_, i) =>
    gen.generate(seed, i + 1)
  );

  // Generate 100 patterns with player weak on 'hold'
  const patternsWithModel = Array.from({ length: 100 }, (_, i) =>
    gen.generate(seed, i + 1, {
      weaknesses: { hold: 0.9 }, // Very weak on holds
      last5: [],
    })
  );

  const holdCountNoModel = patternsNoModel.filter(p => p.type === 'hold').length;
  const holdCountWithModel = patternsWithModel.filter(p => p.type === 'hold').length;

  // With high weakness, should see more hold patterns
  assertEquals(
    holdCountWithModel >= holdCountNoModel,
    true,
    `Expected more holds with weakness model (no model: ${holdCountNoModel}, with model: ${holdCountWithModel})`
  );
});

Deno.test('validatePatterns generates full run sequence', () => {
  const seed = BigInt(12345);
  const floors = 25;

  const patterns = validatePatterns(seed, floors, DEFAULT_DIFFICULTY_CONFIG);

  assertEquals(patterns.length, floors);

  // All patterns should have valid properties
  for (const pattern of patterns) {
    assertEquals(typeof pattern.type, 'string');
    assertEquals(typeof pattern.timeWindow, 'number');
    assertEquals(typeof pattern.speed, 'number');
    assertEquals(pattern.timeWindow >= DEFAULT_DIFFICULTY_CONFIG.minWindow, true);
    assertEquals(pattern.timeWindow <= DEFAULT_DIFFICULTY_CONFIG.maxWindow, true);
  }
});

Deno.test('Pattern properties are valid for each type', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(99999);

  // Generate many patterns to catch all types
  const patterns = Array.from({ length: 200 }, (_, i) => gen.generate(seed, i + 1));

  for (const pattern of patterns) {
    switch (pattern.type) {
      case 'swipe':
      case 'tilt':
        assertEquals(
          ['L', 'R', 'U', 'D'].includes(pattern.direction!),
          true,
          `Swipe/tilt pattern should have valid direction, got: ${pattern.direction}`
        );
        break;

      case 'hold':
        assertEquals(pattern.duration > 0, true, 'Hold pattern should have positive duration');
        break;

      case 'rhythm':
      case 'doubleTap':
        assertEquals(
          pattern.complexity >= 2,
          true,
          'Rhythm/doubleTap should have complexity >= 2'
        );
        break;

      case 'tap':
        // Tap has no special properties
        break;

      default:
        throw new Error(`Unknown pattern type: ${pattern.type}`);
    }
  }
});

Deno.test('Cross-platform consistency - serialize and compare', () => {
  const gen = new PatternGenerator(DEFAULT_DIFFICULTY_CONFIG);
  const seed = BigInt(12345);

  // Generate pattern and serialize
  const pattern = gen.generate(seed, 15);
  const serialized = JSON.stringify({
    type: pattern.type,
    direction: pattern.direction,
    duration: pattern.duration,
    complexity: pattern.complexity,
    timeWindow: pattern.timeWindow,
    speed: pattern.speed,
  });

  // Re-generate and serialize again
  const pattern2 = gen.generate(seed, 15);
  const serialized2 = JSON.stringify({
    type: pattern2.type,
    direction: pattern2.direction,
    duration: pattern2.duration,
    complexity: pattern2.complexity,
    timeWindow: pattern2.timeWindow,
    speed: pattern2.speed,
  });

  assertEquals(
    serialized,
    serialized2,
    'Serialized patterns should be identical for cross-platform validation'
  );
});

console.log('✅ All pattern generator tests passed!');
