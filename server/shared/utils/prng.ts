// Seeded PRNG using xoshiro128** algorithm
// Deterministic random number generation for pattern consistency across client/server

export class SeededRandom {
  private state: Uint32Array;

  constructor(seed: bigint | number) {
    this.state = new Uint32Array(4);
    this.seed(BigInt(seed));
  }

  // Initialize state from seed using SplitMix64
  private seed(seed: bigint): void {
    let s = seed;

    for (let i = 0; i < 4; i++) {
      s += BigInt('0x9e3779b97f4a7c15');
      let z = s;
      z = (z ^ (z >> BigInt(30))) * BigInt('0xbf58476d1ce4e5b9');
      z = (z ^ (z >> BigInt(27))) * BigInt('0x94d049bb133111eb');
      z = z ^ (z >> BigInt(31));
      this.state[i] = Number(z & BigInt(0xffffffff));
    }
  }

  // xoshiro128** next() - returns 32-bit integer
  private next(): number {
    const result = this.rotl(this.state[1] * 5, 7) * 9;
    const t = this.state[1] << 9;

    this.state[2] ^= this.state[0];
    this.state[3] ^= this.state[1];
    this.state[1] ^= this.state[2];
    this.state[0] ^= this.state[3];

    this.state[2] ^= t;
    this.state[3] = this.rotl(this.state[3], 11);

    return result >>> 0; // convert to unsigned
  }

  private rotl(x: number, k: number): number {
    return ((x << k) | (x >>> (32 - k))) >>> 0;
  }

  // Returns float in [0, 1)
  nextFloat(): number {
    return this.next() / 0x100000000;
  }

  // Returns integer in [0, max)
  nextInt(max: number): number {
    return Math.floor(this.nextFloat() * max);
  }

  // Returns integer in [min, max)
  nextRange(min: number, max: number): number {
    return min + this.nextInt(max - min);
  }
}

// Weighted random choice
export function weightedChoice<T>(
  items: T[],
  weights: number[],
  rng: SeededRandom
): T {
  if (items.length !== weights.length) {
    throw new Error('Items and weights must have same length');
  }

  const totalWeight = weights.reduce((sum, w) => sum + w, 0);
  let random = rng.nextFloat() * totalWeight;

  for (let i = 0; i < items.length; i++) {
    random -= weights[i];
    if (random <= 0) {
      return items[i];
    }
  }

  return items[items.length - 1]; // fallback
}
