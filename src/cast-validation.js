import { ROUND_STATES } from './constants.js';
import { SKILL_BY_ID } from './skill-catalog.js';
import { isSkillEquipped } from './loadout.js';

export class CastValidator {
  constructor() {
    this.nextCastReadyAtMs = new Map();
  }

  getKey(playerId, skillId) {
    return `${playerId}:${skillId}`;
  }

  validateAndCommit({ phase, playerId, skillId, nowMs, ownedSkills, loadout }) {
    if (phase !== ROUND_STATES.COMBAT) {
      return { ok: false, reason: 'CAST_ONLY_ALLOWED_IN_COMBAT' };
    }

    const skill = SKILL_BY_ID[skillId];
    if (!skill) {
      return { ok: false, reason: 'UNKNOWN_SKILL' };
    }

    if (!ownedSkills.has(skillId)) {
      return { ok: false, reason: 'SKILL_NOT_OWNED' };
    }

    if (!isSkillEquipped(loadout, skillId)) {
      return { ok: false, reason: 'SKILL_NOT_EQUIPPED' };
    }

    const key = this.getKey(playerId, skillId);
    const readyAtMs = this.nextCastReadyAtMs.get(key) ?? 0;

    if (nowMs < readyAtMs) {
      return {
        ok: false,
        reason: 'COOLDOWN_ACTIVE',
        retryAfterMs: readyAtMs - nowMs,
      };
    }

    this.nextCastReadyAtMs.set(key, nowMs + skill.cooldownSec * 1000);
    return {
      ok: true,
      committedCooldownUntilMs: nowMs + skill.cooldownSec * 1000,
    };
  }
}
