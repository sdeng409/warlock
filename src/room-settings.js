import { DEFAULT_ROUNDS, MAX_PLAYERS, MIN_PLAYERS, ROOM_ROUND_OPTIONS } from './constants.js';

export function normalizeRoomSettings(input = {}) {
  const rounds = input.rounds ?? DEFAULT_ROUNDS;
  const maxPlayers = input.maxPlayers ?? MAX_PLAYERS;

  return {
    rounds,
    maxPlayers,
  };
}

export function validateRoomSettings(input = {}) {
  const settings = normalizeRoomSettings(input);
  const errors = [];

  if (!Number.isInteger(settings.maxPlayers)) {
    errors.push('maxPlayers must be an integer');
  } else if (settings.maxPlayers < MIN_PLAYERS || settings.maxPlayers > MAX_PLAYERS) {
    errors.push(`maxPlayers must be between ${MIN_PLAYERS} and ${MAX_PLAYERS}`);
  }

  if (!ROOM_ROUND_OPTIONS.includes(settings.rounds)) {
    errors.push(`rounds must be one of: ${ROOM_ROUND_OPTIONS.join(', ')}`);
  }

  return {
    ok: errors.length === 0,
    errors,
    settings,
  };
}
