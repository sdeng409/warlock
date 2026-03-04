import { ROUND_STATES } from './constants.js';

export const NETWORK_EVENTS = Object.freeze({
  match: Object.freeze(['RoomCreated', 'PlayerJoined', 'RoomSettingsUpdated']),
  round: Object.freeze(['RoundStarted', 'RoundEnded', 'MatchEnded']),
  combat: Object.freeze([
    'SkillCastRequested',
    'SkillCastConfirmed',
    'DamageApplied',
    'KnockbackApplied',
    'PlayerEliminated',
  ]),
  economy: Object.freeze([
    'RankPointsGranted',
    'GoldGranted',
    'ShopPurchaseRequested',
    'ShopPurchaseConfirmed',
    'ShopPurchaseRejected',
    'LoadoutUpdated',
  ]),
  arena: Object.freeze(['ShrinkStateUpdated', 'BoundaryDotTick']),
});

export function validateHostSkillCastRequest({ phase, cooldownValidation }) {
  if (phase !== ROUND_STATES.COMBAT) {
    return { ok: false, reason: 'ROUND_PHASE_INVALID' };
  }

  if (!cooldownValidation.ok) {
    return { ok: false, reason: cooldownValidation.reason };
  }

  return { ok: true };
}

export function validateHostShopPurchase({ playerGold, price, phase }) {
  if (phase !== ROUND_STATES.SHOP) {
    return { ok: false, reason: 'SHOP_PHASE_REQUIRED' };
  }

  if (playerGold < 0) {
    return { ok: false, reason: 'NEGATIVE_GOLD_INVALID' };
  }

  if (price < 0) {
    return { ok: false, reason: 'NEGATIVE_PRICE_INVALID' };
  }

  if (playerGold < price) {
    return { ok: false, reason: 'INSUFFICIENT_GOLD' };
  }

  return { ok: true };
}
