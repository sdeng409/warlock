export function validateRank(rank, totalPlayers) {
  if (!Number.isInteger(rank) || rank < 1 || rank > totalPlayers) {
    throw new Error(`rank must be between 1 and ${totalPlayers}`);
  }
}

export function rankPointsFor(totalPlayers, rank) {
  validateRank(rank, totalPlayers);
  return totalPlayers - rank + 1;
}

export function goldPayoutFor(totalPlayers, rank) {
  validateRank(rank, totalPlayers);
  return totalPlayers - rank + 1;
}

export function applyRoundEconomy(players, roundRanks) {
  const totalPlayers = players.length;
  const rankByPlayer = new Map(roundRanks.map((r) => [r.playerId, r.rank]));

  for (const player of players) {
    const rank = rankByPlayer.get(player.id);
    if (!rank) {
      throw new Error(`Missing rank for player ${player.id}`);
    }

    const points = rankPointsFor(totalPlayers, rank);
    const gold = goldPayoutFor(totalPlayers, rank);

    player.totalPoints += points;
    player.gold += gold;
    player.lastRoundRank = rank;
    if (rank === 1) {
      player.firstPlaceCount += 1;
    }
  }
}

export function decideFinalWinner(players) {
  if (!players.length) {
    throw new Error('players cannot be empty');
  }

  const sorted = [...players].sort((a, b) => {
    if (b.totalPoints !== a.totalPoints) {
      return b.totalPoints - a.totalPoints;
    }
    if (b.firstPlaceCount !== a.firstPlaceCount) {
      return b.firstPlaceCount - a.firstPlaceCount;
    }
    if (a.lastRoundRank !== b.lastRoundRank) {
      return a.lastRoundRank - b.lastRoundRank;
    }
    return String(a.id).localeCompare(String(b.id));
  });

  return {
    winner: sorted[0],
    standings: sorted,
  };
}
