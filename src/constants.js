export const GAME_MODE = Object.freeze({
  FFA: 'FFA',
});

export const EXCLUDED_FEATURES = Object.freeze([
  'team-mode',
  'random-matchmaking',
  'dedicated-server',
]);

export const ROUND_STATES = Object.freeze({
  WAITING: 'Waiting',
  ROUND_START: 'RoundStart',
  COMBAT: 'Combat',
  ROUND_END: 'RoundEnd',
  SHOP: 'Shop',
  MATCH_END: 'MatchEnd',
});

export const LOADOUT_SLOT_KEYS = Object.freeze('QWERASDFZXCV'.split(''));

export const ROOM_ROUND_OPTIONS = Object.freeze([3, 5, 7]);
export const DEFAULT_ROUNDS = 5;
export const MIN_PLAYERS = 2;
export const MAX_PLAYERS = 8;

export const BOUNDARY_DOT = Object.freeze({
  tickSeconds: 1,
  maxHpPercentPerTick: 0.05,
  insideResetSeconds: 3,
});

export const ARENA_SHRINK = Object.freeze({
  startAfterSeconds: 30,
  shrinkPerSecond: 0.015,
  minRadiusRatio: 0.35,
});
