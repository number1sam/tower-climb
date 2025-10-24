#!/usr/bin/env -S deno run --allow-net --allow-env --allow-read

/**
 * Backend Test Suite
 * Tests pattern generation, PRNG consistency, and Edge Functions
 */

import { SeededRandom, weightedChoice } from './shared/utils/prng.ts';
import { PatternGenerator } from './shared/utils/pattern-generator.ts';
import { PatternType, DifficultyConfig } from './shared/types/game-types.ts';

console.log('🧪 Tower Climb Backend Test Suite\n');

let passCount = 0;
let failCount = 0;

function test(name: string, fn: () => boolean | Promise<boolean>) {
  try {
    const result = fn();
    if (result instanceof Promise) {
      result.then((passed) => {
        if (passed) {
          console.log(`✅ ${name}`);
          passCount++;
        } else {
          console.log(`❌ ${name}`);
          failCount++;
        }
      });
    } else {
      if (result) {
        console.log(`✅ ${name}`);
        passCount++;
      } else {
        console.log(`❌ ${name}`);
        failCount++;
      }
    }
  } catch (error) {
    console.log(`❌ ${name} - Error: ${error.message}`);
    failCount++;
  }
}

// Test 1: PRNG Determinism
test('PRNG produces consistent results for same seed', () => {
  const rng1 = new SeededRandom(12345n);
  const rng2 = new SeededRandom(12345n);

  for (let i = 0; i < 100; i++) {
    if (rng1.nextFloat() !== rng2.nextFloat()) {
      return false;
    }
  }
  return true;
});

// Test 2: PRNG Different Seeds
test('PRNG produces different results for different seeds', () => {
  const rng1 = new SeededRandom(12345n);
  const rng2 = new SeededRandom(54321n);

  let same = 0;
  for (let i = 0; i < 100; i++) {
    if (rng1.nextFloat() === rng2.nextFloat()) {
      same++;
    }
  }
  // Should be extremely unlikely to match more than once
  return same < 5;
});

// Test 3: PRNG Range
test('PRNG nextFloat produces values in [0, 1)', () => {
  const rng = new SeededRandom(99999n);
  for (let i = 0; i < 1000; i++) {
    const val = rng.nextFloat();
    if (val < 0 || val >= 1) {
      return false;
    }
  }
  return true;
});

// Test 4: Weighted Choice Distribution
test('Weighted choice respects probabilities', () => {
  const rng = new SeededRandom(42n);
  const items = ['A', 'B', 'C'];
  const weights = [10, 1, 1]; // A should be chosen ~83% of time

  let counts = { A: 0, B: 0, C: 0 };
  for (let i = 0; i < 1000; i++) {
    const choice = weightedChoice(items, weights, rng);
    counts[choice]++;
  }

  // A should be roughly 10x more common than B or C
  return counts.A > 700 && counts.A < 900;
});

// Test 5: Pattern Generator Determinism
test('Pattern generator produces same patterns for same seed', () => {
  const config: DifficultyConfig = {
    v0: 1.0,
    deltaV: 0.05,
    minWindow: 0.3,
    maxWindow: 2.0,
    baseWindow: 1.5,
    adaptiveEpsilon: 0.1,
    baseWeights: {
      tap: 1.0,
      swipe: 1.0,
      hold: 0.8,
      rhythm: 0.5,
      tilt: 0.3,
      doubleTap: 0.4,
    },
  };

  const gen1 = new PatternGenerator(config);
  const gen2 = new PatternGenerator(config);

  const patterns1 = gen1.generateSequence(1000n, 1, 50);
  const patterns2 = gen2.generateSequence(1000n, 1, 50);

  if (patterns1.length !== patterns2.length) return false;

  for (let i = 0; i < patterns1.length; i++) {
    const p1 = patterns1[i];
    const p2 = patterns2[i];
    if (
      p1.type !== p2.type ||
      p1.direction !== p2.direction ||
      Math.abs(p1.timeWindow - p2.timeWindow) > 0.001
    ) {
      console.log(`  Mismatch at floor ${i + 1}: ${p1.type} vs ${p2.type}`);
      return false;
    }
  }
  return true;
});

// Test 6: Pattern Speed Increases
test('Pattern speed increases with floor number', () => {
  const config: DifficultyConfig = {
    v0: 1.0,
    deltaV: 0.05,
    minWindow: 0.3,
    maxWindow: 2.0,
    baseWindow: 1.5,
    adaptiveEpsilon: 0,
    baseWeights: { tap: 1, swipe: 0, hold: 0, rhythm: 0, tilt: 0, doubleTap: 0 },
  };

  const gen = new PatternGenerator(config);
  const floor1 = gen.generate(1000n, 1);
  const floor10 = gen.generate(1000n, 10);
  const floor50 = gen.generate(1000n, 50);

  // Speed should increase
  return (
    floor1.speed < floor10.speed &&
    floor10.speed < floor50.speed &&
    floor1.timeWindow > floor50.timeWindow
  );
});

// Test 7: Pattern Types Distribution
test('Pattern generator respects weight distribution', () => {
  const config: DifficultyConfig = {
    v0: 1.0,
    deltaV: 0.05,
    minWindow: 0.3,
    maxWindow: 2.0,
    baseWindow: 1.5,
    adaptiveEpsilon: 0,
    baseWeights: {
      tap: 10, // Should dominate
      swipe: 1,
      hold: 1,
      rhythm: 1,
      tilt: 1,
      doubleTap: 1,
    },
  };

  const gen = new PatternGenerator(config);
  let tapCount = 0;
  let totalCount = 100;

  for (let i = 1; i <= totalCount; i++) {
    const pattern = gen.generate(5000n, i);
    if (pattern.type === PatternType.Tap) {
      tapCount++;
    }
  }

  // Tap should be roughly 66% (10 out of 15 total weight)
  return tapCount > 50 && tapCount < 80;
});

// Test 8: Floor Seed Mixing
test('Different floors produce different patterns', () => {
  const gen = new PatternGenerator({
    v0: 1.0,
    deltaV: 0,
    minWindow: 0.5,
    maxWindow: 2.0,
    baseWindow: 1.5,
    adaptiveEpsilon: 0,
    baseWeights: { tap: 1, swipe: 1, hold: 1, rhythm: 1, tilt: 1, doubleTap: 1 },
  });

  const same = 12345n;
  const floor1 = gen.generate(same, 1);
  const floor2 = gen.generate(same, 2);
  const floor3 = gen.generate(same, 3);

  // At least 2 should be different types
  const types = [floor1.type, floor2.type, floor3.type];
  const unique = new Set(types);
  return unique.size >= 2;
});

// Test 9: Time Window Constraints
test('Time windows respect min/max bounds', () => {
  const config: DifficultyConfig = {
    v0: 1.0,
    deltaV: 0.1, // Aggressive increase
    minWindow: 0.3,
    maxWindow: 2.0,
    baseWindow: 1.5,
    adaptiveEpsilon: 0,
    baseWeights: { tap: 1, swipe: 1, hold: 1, rhythm: 1, tilt: 1, doubleTap: 1 },
  };

  const gen = new PatternGenerator(config);

  // Test floor 1 (slow)
  const floor1 = gen.generate(100n, 1);
  if (floor1.timeWindow < 0.3 || floor1.timeWindow > 2.0) return false;

  // Test floor 100 (very fast)
  const floor100 = gen.generate(100n, 100);
  if (floor100.timeWindow < 0.3 || floor100.timeWindow > 2.0) return false;

  return true;
});

// Test 10: Adaptive Difficulty
test('Adaptive difficulty increases spawn rate of weak patterns', () => {
  const config: DifficultyConfig = {
    v0: 1.0,
    deltaV: 0.05,
    minWindow: 0.5,
    maxWindow: 2.0,
    baseWindow: 1.5,
    adaptiveEpsilon: 0.5, // Strong adaptation
    baseWeights: { tap: 1, swipe: 1, hold: 1, rhythm: 1, tilt: 1, doubleTap: 1 },
  };

  const gen = new PatternGenerator(config);

  // Simulate player weak at Hold patterns
  const playerModel = {
    weaknesses: {
      [PatternType.Hold]: 0.8, // 80% failure rate
      [PatternType.Tap]: 0.1,
      [PatternType.Swipe]: 0.1,
      [PatternType.Rhythm]: 0.1,
      [PatternType.Tilt]: 0.1,
      [PatternType.DoubleTap]: 0.1,
    },
    last5: [],
  };

  let holdCount = 0;
  const total = 100;

  for (let i = 1; i <= total; i++) {
    const pattern = gen.generate(2000n, i, playerModel);
    if (pattern.type === PatternType.Hold) {
      holdCount++;
    }
  }

  // Hold should appear more than 1/6 (16.67%) due to adaptation
  // With epsilon 0.5, should be around 25-35%
  return holdCount > 20 && holdCount < 50;
});

// Summary
console.log('\n📊 Test Results:');
console.log(`✅ Passed: ${passCount}`);
console.log(`❌ Failed: ${failCount}`);
console.log(`📈 Success Rate: ${((passCount / (passCount + failCount)) * 100).toFixed(1)}%`);

if (failCount === 0) {
  console.log('\n🎉 All backend tests passed!');
  Deno.exit(0);
} else {
  console.log('\n⚠️  Some tests failed - review output above');
  Deno.exit(1);
}
