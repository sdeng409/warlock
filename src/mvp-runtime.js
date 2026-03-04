import { LOADOUT_SLOT_KEYS, ROUND_STATES } from './constants.js';
import { equipSkill, unequipSkill } from './loadout.js';
import { NETWORK_EVENTS } from './network-contract.js';
import { validateRoomSettings } from './room-settings.js';

const ROUND_UI_DEFINITION = Object.freeze({
  [ROUND_STATES.WAITING]: Object.freeze({
    title: 'Waiting For Players',
    primaryAction: 'HostStartMatchWhenReady',
  }),
  [ROUND_STATES.ROUND_START]: Object.freeze({
    title: 'Round Start',
    primaryAction: 'RoundIntroCountdown',
  }),
  [ROUND_STATES.COMBAT]: Object.freeze({
    title: 'Combat',
    primaryAction: 'EnableSkillCasting',
  }),
  [ROUND_STATES.ROUND_END]: Object.freeze({
    title: 'Round End',
    primaryAction: 'ShowRoundRanking',
  }),
  [ROUND_STATES.SHOP]: Object.freeze({
    title: 'Shop',
    primaryAction: 'PurchaseAndEquipSkills',
  }),
  [ROUND_STATES.MATCH_END]: Object.freeze({
    title: 'Match End',
    primaryAction: 'ShowFinalWinner',
  }),
});

function assertKnownRoundState(phase) {
  if (!ROUND_UI_DEFINITION[phase]) {
    throw new Error(`Unknown round phase: ${phase}`);
  }
}

export function presentRoundTransition({ phase, currentRound = 0, roundsTotal = 0 } = {}) {
  assertKnownRoundState(phase);
  const base = ROUND_UI_DEFINITION[phase];
  const roundText =
    roundsTotal > 0 && currentRound > 0 ? `Round ${currentRound}/${roundsTotal}` : 'Round N/A';
  return {
    phase,
    title: base.title,
    primaryAction: base.primaryAction,
    roundText,
  };
}

export function createDefaultSlotBindings() {
  return Object.fromEntries(LOADOUT_SLOT_KEYS.map((slot) => [slot, slot]));
}

export function validateSlotBindings(slotToKey) {
  const errors = [];
  const slots = Object.keys(slotToKey ?? {});

  for (const slot of LOADOUT_SLOT_KEYS) {
    if (!slots.includes(slot)) {
      errors.push(`MISSING_SLOT_${slot}`);
    }
  }

  if (slots.length !== LOADOUT_SLOT_KEYS.length) {
    errors.push('SLOT_COUNT_MISMATCH');
  }

  const keys = LOADOUT_SLOT_KEYS.map((slot) => slotToKey?.[slot]);
  const normalizedKeys = keys.map((key) => (typeof key === 'string' ? key.trim().toUpperCase() : ''));

  if (normalizedKeys.some((key) => key.length === 0)) {
    errors.push('INVALID_KEY');
  }

  if (new Set(normalizedKeys).size !== normalizedKeys.length) {
    errors.push('DUPLICATE_KEY_BINDING');
  }

  return errors.length === 0
    ? { ok: true, bindings: Object.freeze({ ...slotToKey }) }
    : { ok: false, errors };
}

export function rebindSlot(slotToKey, { slot, key }) {
  if (!LOADOUT_SLOT_KEYS.includes(slot)) {
    return { ok: false, reason: 'INVALID_SLOT' };
  }

  if (typeof key !== 'string' || key.trim().length === 0) {
    return { ok: false, reason: 'INVALID_KEY' };
  }

  const next = { ...slotToKey, [slot]: key.trim().toUpperCase() };
  const validation = validateSlotBindings(next);
  if (!validation.ok) {
    return { ok: false, reason: validation.errors[0], errors: validation.errors };
  }

  return { ok: true, bindings: next };
}

export class LoadoutUiModel {
  constructor({ ownedSkills, loadout }) {
    this.ownedSkills = ownedSkills;
    this.loadout = loadout;
  }

  equip(slot, skillId) {
    return equipSkill({
      ownedSkills: this.ownedSkills,
      loadout: this.loadout,
      slot,
      skillId,
    });
  }

  unequip(slot) {
    return unequipSkill({ loadout: this.loadout, slot });
  }

  snapshot() {
    return Object.freeze({ ...this.loadout });
  }
}

const ALL_NETWORK_EVENT_TYPES = new Set([
  ...NETWORK_EVENTS.match,
  ...NETWORK_EVENTS.round,
  ...NETWORK_EVENTS.combat,
  ...NETWORK_EVENTS.economy,
  ...NETWORK_EVENTS.arena,
]);

let roomSequence = 0;

function nextRoomId() {
  roomSequence += 1;
  return `room-${String(roomSequence).padStart(4, '0')}`;
}

function inviteCodeFromRoomId(roomId) {
  const suffix = roomId.replace('room-', '');
  return `WLK${suffix}`;
}

export class HostRelaySession {
  constructor({ hostId }) {
    if (!hostId) {
      throw new Error('hostId is required');
    }

    this.hostId = hostId;
    this.room = null;
    this.players = new Set();
    this.disconnectedPlayers = new Set();
    this.events = [];
    this.lastSeq = 0;
  }

  createRoom({ maxPlayers, rounds } = {}) {
    const validation = validateRoomSettings({ maxPlayers, rounds });
    if (!validation.ok) {
      return { ok: false, reason: validation.errors[0], errors: validation.errors };
    }

    const roomId = nextRoomId();
    this.room = {
      id: roomId,
      inviteCode: inviteCodeFromRoomId(roomId),
      settings: validation.settings,
    };
    this.players = new Set([this.hostId]);
    this.disconnectedPlayers.clear();

    this._pushEvent('RoomCreated', {
      roomId: this.room.id,
      inviteCode: this.room.inviteCode,
      hostId: this.hostId,
    });

    return { ok: true, room: this.room };
  }

  joinPlayer(playerId) {
    if (!this.room) {
      return { ok: false, reason: 'ROOM_NOT_CREATED' };
    }

    if (!playerId) {
      return { ok: false, reason: 'INVALID_PLAYER_ID' };
    }

    if (this.players.has(playerId)) {
      return { ok: true, alreadyJoined: true };
    }

    if (this.players.size >= this.room.settings.maxPlayers) {
      return { ok: false, reason: 'ROOM_FULL' };
    }

    this.players.add(playerId);
    this._pushEvent('PlayerJoined', { roomId: this.room.id, playerId });
    return { ok: true };
  }

  syncEvent(eventType, payload = {}) {
    if (!ALL_NETWORK_EVENT_TYPES.has(eventType)) {
      return { ok: false, reason: 'UNKNOWN_EVENT_TYPE' };
    }

    const event = this._pushEvent(eventType, payload);
    return { ok: true, event };
  }

  readEventsSince(seq = 0) {
    return this.events.filter((event) => event.seq > seq);
  }

  onDisconnect(playerId) {
    if (!this.players.has(playerId)) {
      return { ok: false, action: 'REJECT_UNKNOWN_PLAYER' };
    }

    if (playerId === this.hostId) {
      this._pushEvent('MatchEnded', {
        reason: 'HOST_DISCONNECTED',
      });
      return {
        ok: true,
        action: 'SAFE_TERMINATE',
      };
    }

    this.disconnectedPlayers.add(playerId);
    this._pushEvent('PlayerEliminated', {
      playerId,
      reason: 'PLAYER_DISCONNECTED',
    });
    return {
      ok: true,
      action: 'MARK_INACTIVE',
    };
  }

  onReconnect(playerId) {
    if (!this.players.has(playerId)) {
      return { ok: false, action: 'REJECT_UNKNOWN_PLAYER' };
    }

    if (!this.disconnectedPlayers.has(playerId)) {
      return { ok: true, action: 'NO_OP' };
    }

    this.disconnectedPlayers.delete(playerId);
    this._pushEvent('PlayerJoined', {
      playerId,
      reconnected: true,
      resumeFromSeq: this.lastSeq,
    });
    return {
      ok: true,
      action: 'RESUME_FROM_LAST_SEQUENCE',
      resumeFromSeq: this.lastSeq,
    };
  }

  _pushEvent(type, payload) {
    this.lastSeq += 1;
    const event = {
      seq: this.lastSeq,
      type,
      payload,
    };
    this.events.push(event);
    return event;
  }
}
