import test from 'node:test';
import assert from 'node:assert/strict';

import {
  ARENA_SHRINK,
  BOUNDARY_DOT,
  CastValidator,
  HostRelaySession,
  LoadoutUiModel,
  LOADOUT_SLOT_KEYS,
  MvpMatchCore,
  NETWORK_EVENTS,
  ROUND_STATES,
  SKILL_CATALOG,
  assertVersionLock,
  createDefaultSlotBindings,
  createEmptyLoadout,
  createBoundaryState,
  goldPayoutFor,
  presentRoundTransition,
  radiusAtTime,
  rebindSlot,
  rankPointsFor,
  stepBoundaryState,
  validateSlotBindings,
  validateHostShopPurchase,
  validateHostSkillCastRequest,
  validateRoomSettings,
} from '../src/index.js';

test('A: match loop repeats RoundStart->Combat->RoundEnd->Shop and ends at MatchEnd', () => {
  const core = new MvpMatchCore({
    roomSettings: { maxPlayers: 4 },
    playerIds: ['p1', 'p2', 'p3', 'p4'],
  });

  core.startRound();

  for (let r = 1; r <= 5; r += 1) {
    assert.equal(core.phase, ROUND_STATES.COMBAT);

    const result = core.endRoundWithRanks([
      { playerId: 'p1', rank: 1 },
      { playerId: 'p2', rank: 2 },
      { playerId: 'p3', rank: 3 },
      { playerId: 'p4', rank: 4 },
    ]);

    if (r < 5) {
      assert.equal(result.nextPhase, ROUND_STATES.SHOP);
      core.closeShopAndStartNextRound();
    } else {
      assert.equal(result.nextPhase, ROUND_STATES.MATCH_END);
      assert.equal(core.phase, ROUND_STATES.MATCH_END);
    }
  }
});

test('A: winner tie-break priority is first-place count then last-round rank', () => {
  const core = new MvpMatchCore({
    roomSettings: { maxPlayers: 4, rounds: 3 },
    playerIds: ['A', 'B', 'C', 'D'],
  });

  core.startRound();
  core.endRoundWithRanks([
    { playerId: 'A', rank: 1 },
    { playerId: 'B', rank: 2 },
    { playerId: 'C', rank: 3 },
    { playerId: 'D', rank: 4 },
  ]);
  core.closeShopAndStartNextRound();

  core.endRoundWithRanks([
    { playerId: 'A', rank: 1 },
    { playerId: 'B', rank: 3 },
    { playerId: 'C', rank: 2 },
    { playerId: 'D', rank: 4 },
  ]);
  core.closeShopAndStartNextRound();

  const final = core.endRoundWithRanks([
    { playerId: 'B', rank: 1 },
    { playerId: 'C', rank: 2 },
    { playerId: 'D', rank: 3 },
    { playerId: 'A', rank: 4 },
  ]);

  assert.equal(final.winner.id, 'A');
  assert.equal(final.winner.totalPoints, 9);
  assert.equal(core.getPlayer('B').totalPoints, 9);
  assert.equal(final.winner.firstPlaceCount, 2);
});

test('A: last-round rank breaks tie when points and first-place count are equal', () => {
  const core = new MvpMatchCore({
    roomSettings: { maxPlayers: 3, rounds: 3 },
    playerIds: ['A', 'B', 'C'],
  });

  core.startRound();
  core.endRoundWithRanks([
    { playerId: 'A', rank: 1 },
    { playerId: 'B', rank: 2 },
    { playerId: 'C', rank: 3 },
  ]);
  core.closeShopAndStartNextRound();

  core.endRoundWithRanks([
    { playerId: 'B', rank: 1 },
    { playerId: 'C', rank: 2 },
    { playerId: 'A', rank: 3 },
  ]);
  core.closeShopAndStartNextRound();

  const final = core.endRoundWithRanks([
    { playerId: 'C', rank: 1 },
    { playerId: 'A', rank: 2 },
    { playerId: 'B', rank: 3 },
  ]);

  assert.equal(core.getPlayer('A').totalPoints, 6);
  assert.equal(core.getPlayer('B').totalPoints, 6);
  assert.equal(core.getPlayer('C').totalPoints, 6);
  assert.equal(final.winner.id, 'C');
});

test('B: classless 12-slot loadout enforces unowned and duplicate checks', () => {
  const core = new MvpMatchCore({
    roomSettings: { maxPlayers: 2, rounds: 3 },
    playerIds: ['p1', 'p2'],
  });

  core.startRound();
  core.endRoundWithRanks([
    { playerId: 'p1', rank: 1 },
    { playerId: 'p2', rank: 2 },
  ]);
  core.getPlayer('p1').gold = 10;
  core.purchaseSkill('p1', 'S01');
  core.purchaseSkill('p1', 'S02');

  const player = core.getPlayer('p1');
  assert.equal(Object.keys(player.loadout).length, 12);
  assert.deepEqual(Object.keys(player.loadout), LOADOUT_SLOT_KEYS);

  assert.equal(core.equipSkill('p1', 'Q', 'S01').ok, true);
  assert.equal(core.equipSkill('p1', 'W', 'S02').ok, true);

  const duplicate = core.equipSkill('p1', 'E', 'S01');
  assert.equal(duplicate.ok, false);
  assert.equal(duplicate.reason, 'DUPLICATE_SKILL_NOT_ALLOWED');

  const unowned = core.equipSkill('p1', 'R', 'S03');
  assert.equal(unowned.ok, false);
  assert.equal(unowned.reason, 'UNOWNED_SKILL');
});

test('B: cast validation is cooldown-only and combat-only', () => {
  const core = new MvpMatchCore({
    roomSettings: { maxPlayers: 2, rounds: 3 },
    playerIds: ['p1', 'p2'],
  });

  core.startRound();
  core.endRoundWithRanks([
    { playerId: 'p1', rank: 1 },
    { playerId: 'p2', rank: 2 },
  ]);
  core.purchaseSkill('p1', 'S01');
  core.equipSkill('p1', 'Q', 'S01');
  core.closeShopAndStartNextRound();

  const validator = new CastValidator();
  const player = core.getPlayer('p1');

  const castOk = validator.validateAndCommit({
    phase: core.phase,
    playerId: 'p1',
    skillId: 'S01',
    nowMs: 0,
    ownedSkills: player.ownedSkills,
    loadout: player.loadout,
  });
  assert.equal(castOk.ok, true);

  const cooldownRejected = validator.validateAndCommit({
    phase: core.phase,
    playerId: 'p1',
    skillId: 'S01',
    nowMs: 1000,
    ownedSkills: player.ownedSkills,
    loadout: player.loadout,
  });
  assert.equal(cooldownRejected.ok, false);
  assert.equal(cooldownRejected.reason, 'COOLDOWN_ACTIVE');

  const nonCombat = validator.validateAndCommit({
    phase: ROUND_STATES.SHOP,
    playerId: 'p1',
    skillId: 'S01',
    nowMs: 6000,
    ownedSkills: player.ownedSkills,
    loadout: player.loadout,
  });
  assert.equal(nonCombat.ok, false);
  assert.equal(nonCombat.reason, 'CAST_ONLY_ALLOWED_IN_COMBAT');
});

test('B: input binding map keeps 12 unique slot-key bindings', () => {
  const defaults = createDefaultSlotBindings();
  const valid = validateSlotBindings(defaults);
  assert.equal(valid.ok, true);
  assert.equal(Object.keys(defaults).length, 12);
  assert.equal(new Set(Object.values(defaults)).size, 12);

  const duplicateRebind = rebindSlot(defaults, { slot: 'W', key: 'Q' });
  assert.equal(duplicateRebind.ok, false);
  assert.equal(duplicateRebind.reason, 'DUPLICATE_KEY_BINDING');

  const invalidSlot = rebindSlot(defaults, { slot: 'P', key: '1' });
  assert.equal(invalidSlot.ok, false);
  assert.equal(invalidSlot.reason, 'INVALID_SLOT');
});

test('B: loadout UI model supports equip and unequip flow', () => {
  const ownedSkills = new Set(['S01', 'S02']);
  const loadout = createEmptyLoadout();
  const ui = new LoadoutUiModel({ ownedSkills, loadout });

  const equipQ = ui.equip('Q', 'S01');
  assert.equal(equipQ.ok, true);
  assert.equal(ui.snapshot().Q, 'S01');

  const duplicate = ui.equip('W', 'S01');
  assert.equal(duplicate.ok, false);
  assert.equal(duplicate.reason, 'DUPLICATE_SKILL_NOT_ALLOWED');

  const unequipQ = ui.unequip('Q');
  assert.equal(unequipQ.ok, true);
  assert.equal(ui.snapshot().Q, null);
});

test('C: rank points and gold payout follow rank order', () => {
  assert.equal(rankPointsFor(8, 1), 8);
  assert.equal(rankPointsFor(8, 8), 1);
  assert.equal(goldPayoutFor(8, 1), 8);
  assert.equal(goldPayoutFor(8, 8), 1);
});

test('C: shop purchase applies gold deduction and ownership, with no reroll/resale behavior', () => {
  const core = new MvpMatchCore({
    roomSettings: { maxPlayers: 2, rounds: 3 },
    playerIds: ['p1', 'p2'],
  });

  core.startRound();
  core.endRoundWithRanks([
    { playerId: 'p1', rank: 1 },
    { playerId: 'p2', rank: 2 },
  ]);

  const buy = core.purchaseSkill('p1', 'S01');
  assert.equal(buy.ok, true);
  assert.equal(core.getPlayer('p1').gold, 0);
  assert.equal(core.getPlayer('p1').ownedSkills.has('S01'), true);

  assert.equal(typeof core.rerollShop, 'undefined');
  assert.equal(typeof core.resellSkill, 'undefined');
});

test('D: boundary DoT and outside accumulation reset after 3s inside', () => {
  const state = createBoundaryState(100);

  stepBoundaryState(state, { isOutside: true, deltaSec: 2.2 });
  assert.equal(state.hp, 90);
  assert.equal(state.outsideAccumulatedSec.toFixed(1), '2.2');

  stepBoundaryState(state, { isOutside: false, deltaSec: 2.0 });
  assert.equal(state.hp, 90);
  assert.equal(state.outsideAccumulatedSec.toFixed(1), '2.2');

  stepBoundaryState(state, { isOutside: true, deltaSec: 1.0 });
  assert.equal(state.hp, 85);
  assert.equal(state.outsideAccumulatedSec.toFixed(1), '3.2');

  stepBoundaryState(state, { isOutside: false, deltaSec: 3.0 });
  assert.equal(state.outsideAccumulatedSec, 0);
});

test('D: outside tick accumulator is preserved unless inside duration reaches 3s', () => {
  const state = createBoundaryState(100);

  stepBoundaryState(state, { isOutside: true, deltaSec: 0.6 });
  assert.equal(state.hp, 100);

  // 3초 미만 복귀는 누적 리셋 금지
  stepBoundaryState(state, { isOutside: false, deltaSec: 2.9 });
  stepBoundaryState(state, { isOutside: true, deltaSec: 0.5 });
  assert.equal(state.hp, 95);

  // 3초 연속 복귀 시 누적 리셋
  stepBoundaryState(state, { isOutside: false, deltaSec: 3.0 });
  stepBoundaryState(state, { isOutside: true, deltaSec: 0.5 });
  assert.equal(state.hp, 95);
});

test('D: arena shrink starts at 30s, shrinks 1.5%/s, and clamps to 35% radius', () => {
  assert.equal(ARENA_SHRINK.startAfterSeconds, 30);
  assert.equal(ARENA_SHRINK.shrinkPerSecond, 0.015);
  assert.equal(ARENA_SHRINK.minRadiusRatio, 0.35);

  assert.equal(radiusAtTime(100, 29), 100);
  assert.equal(radiusAtTime(100, 30), 100);
  assert.equal(radiusAtTime(100, 40), 85);
  assert.equal(radiusAtTime(100, 1000), 35);
});

test('E (minimal): contract constants and host-authoritative validation stubs exist', () => {
  assert.equal(BoundaryDotEventIncluded(), true);
  assert.equal(NETWORK_EVENTS.combat.includes('SkillCastRequested'), true);
  assert.equal(NETWORK_EVENTS.economy.includes('ShopPurchaseRejected'), true);

  const castReject = validateHostSkillCastRequest({
    phase: ROUND_STATES.SHOP,
    cooldownValidation: { ok: true },
  });
  assert.equal(castReject.ok, false);

  const shopReject = validateHostShopPurchase({
    phase: ROUND_STATES.SHOP,
    playerGold: -1,
    price: 1,
  });
  assert.equal(shopReject.ok, false);
  assert.equal(shopReject.reason, 'NEGATIVE_GOLD_INVALID');
});

test('E (minimal): room setting constraints and version lock are fixed', () => {
  const valid = validateRoomSettings({ maxPlayers: 8 });
  assert.equal(valid.ok, true);
  assert.equal(valid.settings.rounds, 5);

  const invalidPlayers = validateRoomSettings({ maxPlayers: 9, rounds: 5 });
  assert.equal(invalidPlayers.ok, false);

  const invalidRounds = validateRoomSettings({ maxPlayers: 4, rounds: 9 });
  assert.equal(invalidRounds.ok, false);

  const lock = assertVersionLock();
  assert.equal(lock.ok, true);
  assert.equal(lock.expected.fusionBuild, '1743');
  assert.equal(lock.expected.ugui, 'editor-core (Unity 6000.0.68f1)');
  assert.equal(BoundaryRulesFixed(), true);
  assert.equal(SKILL_CATALOG.length, 12);
});

test('E: host+relay room creation and event synchronization are deterministic', () => {
  const hostRelay = new HostRelaySession({ hostId: 'host-1' });
  const created = hostRelay.createRoom({ maxPlayers: 4, rounds: 5 });
  assert.equal(created.ok, true);
  assert.equal(created.room.id, 'room-0001');
  assert.equal(created.room.inviteCode, 'WLK0001');

  const joined = hostRelay.joinPlayer('p2');
  assert.equal(joined.ok, true);
  const combatEvent = hostRelay.syncEvent('RoundStarted', { round: 1 });
  assert.equal(combatEvent.ok, true);
  assert.equal(combatEvent.event.seq > 0, true);

  const feed = hostRelay.readEventsSince(0);
  assert.equal(feed.length, 3);
  assert.deepEqual(
    feed.map((event) => event.type),
    ['RoomCreated', 'PlayerJoined', 'RoundStarted'],
  );
});

test('E: reconnect/disconnect policy is explicit and host-fail-safe', () => {
  const hostRelay = new HostRelaySession({ hostId: 'host-1' });
  hostRelay.createRoom({ maxPlayers: 4, rounds: 5 });
  hostRelay.joinPlayer('p2');
  hostRelay.syncEvent('RoundStarted', { round: 1 });

  const playerDisconnect = hostRelay.onDisconnect('p2');
  assert.equal(playerDisconnect.ok, true);
  assert.equal(playerDisconnect.action, 'MARK_INACTIVE');

  const playerReconnect = hostRelay.onReconnect('p2');
  assert.equal(playerReconnect.ok, true);
  assert.equal(playerReconnect.action, 'RESUME_FROM_LAST_SEQUENCE');
  assert.equal(playerReconnect.resumeFromSeq > 0, true);

  const hostDisconnect = hostRelay.onDisconnect('host-1');
  assert.equal(hostDisconnect.ok, true);
  assert.equal(hostDisconnect.action, 'SAFE_TERMINATE');
});

test('A: round transition presenter exposes deterministic UI contract by phase', () => {
  const waiting = presentRoundTransition({
    phase: ROUND_STATES.WAITING,
    currentRound: 0,
    roundsTotal: 5,
  });
  assert.equal(waiting.title, 'Waiting For Players');
  assert.equal(waiting.primaryAction, 'HostStartMatchWhenReady');

  const shop = presentRoundTransition({
    phase: ROUND_STATES.SHOP,
    currentRound: 2,
    roundsTotal: 5,
  });
  assert.equal(shop.title, 'Shop');
  assert.equal(shop.roundText, 'Round 2/5');
});

test('Scope guard: non-FFA mode is rejected (team/dedicated/ranked out of MVP)', () => {
  assert.throws(
    () =>
      new MvpMatchCore({
        roomSettings: { maxPlayers: 2, rounds: 3 },
        playerIds: ['p1', 'p2'],
        mode: 'TEAM',
      }),
    /Only FFA is allowed in MVP reference core/,
  );
});

function BoundaryDotEventIncluded() {
  return NETWORK_EVENTS.arena.includes('BoundaryDotTick');
}

function BoundaryRulesFixed() {
  return BOUNDARY_DOT.tickSeconds === 1 && BOUNDARY_DOT.maxHpPercentPerTick === 0.05;
}
