import { ARENA_SHRINK, BOUNDARY_DOT } from './constants.js';

export function createBoundaryState(maxHp) {
  return {
    maxHp,
    hp: maxHp,
    outsideAccumulatedSec: 0,
    insideContinuousSec: 0,
    // DoT tick 누적치. 경계 안 복귀 즉시 리셋하지 않고
    // "연속 3초 inside" 조건을 만족할 때만 리셋한다.
    outsideTickAccumulatorSec: 0,
  };
}

export function stepBoundaryState(state, { isOutside, deltaSec }) {
  if (isOutside) {
    state.insideContinuousSec = 0;
    state.outsideAccumulatedSec += deltaSec;
    state.outsideTickAccumulatorSec += deltaSec;

    const tickCount = Math.floor(state.outsideTickAccumulatorSec / BOUNDARY_DOT.tickSeconds);
    if (tickCount > 0) {
      state.outsideTickAccumulatorSec -= tickCount * BOUNDARY_DOT.tickSeconds;
      const perTickDamage = state.maxHp * BOUNDARY_DOT.maxHpPercentPerTick;
      state.hp = Math.max(0, state.hp - perTickDamage * tickCount);
    }
  } else {
    // 복귀 즉시 DoT는 중단되지만 누적치는 유지한다.
    state.insideContinuousSec += deltaSec;

    if (state.insideContinuousSec >= BOUNDARY_DOT.insideResetSeconds) {
      state.outsideAccumulatedSec = 0;
      state.outsideTickAccumulatorSec = 0;
    }
  }

  return state;
}

export function radiusAtTime(initialRadius, elapsedSec) {
  if (elapsedSec <= ARENA_SHRINK.startAfterSeconds) {
    return initialRadius;
  }

  const shrinkElapsed = elapsedSec - ARENA_SHRINK.startAfterSeconds;
  const shrinkRatio = Math.max(ARENA_SHRINK.minRadiusRatio, 1 - shrinkElapsed * ARENA_SHRINK.shrinkPerSecond);
  return initialRadius * shrinkRatio;
}
