import { LOADOUT_SLOT_KEYS } from './constants.js';
import { SKILL_BY_ID } from './skill-catalog.js';

export function createEmptyLoadout() {
  return Object.fromEntries(LOADOUT_SLOT_KEYS.map((slot) => [slot, null]));
}

export function equipSkill({ ownedSkills, loadout, slot, skillId }) {
  if (!LOADOUT_SLOT_KEYS.includes(slot)) {
    return { ok: false, reason: 'INVALID_SLOT' };
  }

  if (!SKILL_BY_ID[skillId]) {
    return { ok: false, reason: 'UNKNOWN_SKILL' };
  }

  if (!ownedSkills.has(skillId)) {
    return { ok: false, reason: 'UNOWNED_SKILL' };
  }

  for (const key of LOADOUT_SLOT_KEYS) {
    if (key !== slot && loadout[key] === skillId) {
      return { ok: false, reason: 'DUPLICATE_SKILL_NOT_ALLOWED' };
    }
  }

  loadout[slot] = skillId;
  return { ok: true };
}

export function unequipSkill({ loadout, slot }) {
  if (!LOADOUT_SLOT_KEYS.includes(slot)) {
    return { ok: false, reason: 'INVALID_SLOT' };
  }

  loadout[slot] = null;
  return { ok: true };
}

export function isSkillEquipped(loadout, skillId) {
  return LOADOUT_SLOT_KEYS.some((slot) => loadout[slot] === skillId);
}
