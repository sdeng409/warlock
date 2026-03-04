import { GAME_MODE, ROUND_STATES } from './constants.js';
import { validateRoomSettings } from './room-settings.js';
import { RoundStateMachine } from './round-state-machine.js';
import { SKILL_BY_ID } from './skill-catalog.js';
import { createEmptyLoadout, equipSkill } from './loadout.js';
import { applyRoundEconomy, decideFinalWinner } from './economy.js';

export class MvpMatchCore {
  constructor({ roomSettings, playerIds, mode = GAME_MODE.FFA }) {
    if (mode !== GAME_MODE.FFA) {
      throw new Error('Only FFA is allowed in MVP reference core');
    }

    const validation = validateRoomSettings(roomSettings);
    if (!validation.ok) {
      throw new Error(`Invalid room settings: ${validation.errors.join('; ')}`);
    }

    if (playerIds.length < 2 || playerIds.length > roomSettings.maxPlayers) {
      throw new Error('Player count must be within room maxPlayers and MVP range');
    }

    this.mode = mode;
    this.roomSettings = validation.settings;
    this.roundsTotal = validation.settings.rounds;
    this.stateMachine = new RoundStateMachine(this.roundsTotal);
    this.players = new Map(
      playerIds.map((id) => [
        id,
        {
          id,
          gold: 0,
          totalPoints: 0,
          firstPlaceCount: 0,
          lastRoundRank: Number.POSITIVE_INFINITY,
          ownedSkills: new Set(),
          loadout: createEmptyLoadout(),
        },
      ]),
    );
    this.roundResults = [];
  }

  get phase() {
    return this.stateMachine.state;
  }

  startRound() {
    this.stateMachine.startRound();
    this.stateMachine.beginCombat();
  }

  endRoundWithRanks(roundRanks) {
    if (this.phase !== ROUND_STATES.COMBAT) {
      throw new Error('Round can end only during Combat');
    }

    this.stateMachine.endCombat();
    const playerList = [...this.players.values()];
    applyRoundEconomy(playerList, roundRanks);
    this.roundResults.push(roundRanks);

    const next = this.stateMachine.advanceAfterRoundEnd();
    if (next === ROUND_STATES.SHOP) {
      return { nextPhase: ROUND_STATES.SHOP, round: this.stateMachine.currentRound };
    }

    const final = decideFinalWinner(playerList);
    return {
      nextPhase: ROUND_STATES.MATCH_END,
      winner: final.winner,
      standings: final.standings,
    };
  }

  purchaseSkill(playerId, skillId) {
    if (this.phase !== ROUND_STATES.SHOP) {
      return { ok: false, reason: 'SHOP_PHASE_REQUIRED' };
    }

    const player = this.players.get(playerId);
    const skill = SKILL_BY_ID[skillId];
    if (!player || !skill) {
      return { ok: false, reason: 'INVALID_PLAYER_OR_SKILL' };
    }

    if (player.gold < skill.price) {
      return { ok: false, reason: 'INSUFFICIENT_GOLD' };
    }

    player.gold -= skill.price;
    player.ownedSkills.add(skillId);
    return { ok: true, remainingGold: player.gold };
  }

  equipSkill(playerId, slot, skillId) {
    const player = this.players.get(playerId);
    if (!player) {
      return { ok: false, reason: 'INVALID_PLAYER' };
    }

    return equipSkill({
      ownedSkills: player.ownedSkills,
      loadout: player.loadout,
      slot,
      skillId,
    });
  }

  closeShopAndStartNextRound() {
    if (this.phase !== ROUND_STATES.SHOP) {
      throw new Error('Can only close shop during Shop phase');
    }
    this.startRound();
  }

  getPlayer(playerId) {
    return this.players.get(playerId);
  }

  getStandingsSnapshot() {
    return [...this.players.values()].map((p) => ({
      id: p.id,
      gold: p.gold,
      totalPoints: p.totalPoints,
      firstPlaceCount: p.firstPlaceCount,
      lastRoundRank: p.lastRoundRank,
    }));
  }
}
