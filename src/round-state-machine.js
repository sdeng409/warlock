import { ROUND_STATES } from './constants.js';

const ALLOWED_TRANSITIONS = Object.freeze({
  [ROUND_STATES.WAITING]: [ROUND_STATES.ROUND_START],
  [ROUND_STATES.ROUND_START]: [ROUND_STATES.COMBAT],
  [ROUND_STATES.COMBAT]: [ROUND_STATES.ROUND_END],
  [ROUND_STATES.ROUND_END]: [ROUND_STATES.SHOP, ROUND_STATES.MATCH_END],
  [ROUND_STATES.SHOP]: [ROUND_STATES.ROUND_START, ROUND_STATES.MATCH_END],
  [ROUND_STATES.MATCH_END]: [],
});

export class RoundStateMachine {
  constructor(totalRounds = 5) {
    this.totalRounds = totalRounds;
    this.currentRound = 0;
    this.state = ROUND_STATES.WAITING;
    this.history = [this.state];
  }

  canTransition(nextState) {
    return ALLOWED_TRANSITIONS[this.state].includes(nextState);
  }

  transition(nextState) {
    if (!this.canTransition(nextState)) {
      throw new Error(`Invalid transition: ${this.state} -> ${nextState}`);
    }
    this.state = nextState;
    this.history.push(nextState);
  }

  startRound() {
    if (this.state === ROUND_STATES.WAITING || this.state === ROUND_STATES.SHOP) {
      this.currentRound += 1;
    }
    this.transition(ROUND_STATES.ROUND_START);
  }

  beginCombat() {
    this.transition(ROUND_STATES.COMBAT);
  }

  endCombat() {
    this.transition(ROUND_STATES.ROUND_END);
  }

  openShop() {
    this.transition(ROUND_STATES.SHOP);
  }

  endMatch() {
    if (!this.canTransition(ROUND_STATES.MATCH_END)) {
      throw new Error(`Cannot end match from state: ${this.state}`);
    }
    this.transition(ROUND_STATES.MATCH_END);
  }

  advanceAfterRoundEnd() {
    if (this.currentRound >= this.totalRounds) {
      this.endMatch();
      return this.state;
    }
    this.openShop();
    return this.state;
  }
}
