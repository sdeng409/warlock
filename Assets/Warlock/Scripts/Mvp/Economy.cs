using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock.Mvp
{
    public sealed class PlayerProgress
    {
        public PlayerProgress(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public int Gold { get; set; }
        public int TotalPoints { get; set; }
        public int FirstPlaceCount { get; set; }
        public int LastRoundRank { get; set; } = int.MaxValue;
        public HashSet<string> OwnedSkills { get; } = new();
        public Dictionary<string, string?> Loadout { get; } = LoadoutRules.CreateEmptyLoadout();
    }

    public readonly struct RoundRank
    {
        public RoundRank(string playerId, int rank)
        {
            PlayerId = playerId;
            Rank = rank;
        }

        public string PlayerId { get; }
        public int Rank { get; }
    }

    public readonly struct WinnerDecision
    {
        public WinnerDecision(PlayerProgress winner, IReadOnlyList<PlayerProgress> standings)
        {
            Winner = winner;
            Standings = standings;
        }

        public PlayerProgress Winner { get; }
        public IReadOnlyList<PlayerProgress> Standings { get; }
    }

    public static class EconomyRules
    {
        public static int RankPointsFor(int totalPlayers, int rank)
        {
            ValidateRank(totalPlayers, rank);
            return totalPlayers - rank + 1;
        }

        public static int GoldPayoutFor(int totalPlayers, int rank)
        {
            ValidateRank(totalPlayers, rank);
            return totalPlayers - rank + 1;
        }

        public static void ApplyRoundEconomy(IReadOnlyList<PlayerProgress> players, IReadOnlyList<RoundRank> roundRanks)
        {
            var rankByPlayer = roundRanks.ToDictionary(r => r.PlayerId, r => r.Rank);
            var totalPlayers = players.Count;

            foreach (var player in players)
            {
                if (!rankByPlayer.TryGetValue(player.Id, out var rank))
                {
                    throw new InvalidOperationException($"Missing rank for player {player.Id}");
                }

                var points = RankPointsFor(totalPlayers, rank);
                var gold = GoldPayoutFor(totalPlayers, rank);

                player.TotalPoints += points;
                player.Gold += gold;
                player.LastRoundRank = rank;

                if (rank == 1)
                {
                    player.FirstPlaceCount += 1;
                }
            }
        }

        public static WinnerDecision DecideFinalWinner(IReadOnlyList<PlayerProgress> players)
        {
            if (players.Count == 0)
            {
                throw new InvalidOperationException("players cannot be empty");
            }

            var standings = players
                .OrderByDescending(p => p.TotalPoints)
                .ThenByDescending(p => p.FirstPlaceCount)
                .ThenBy(p => p.LastRoundRank)
                .ThenBy(p => p.Id, StringComparer.Ordinal)
                .ToList();

            return new WinnerDecision(standings[0], standings);
        }

        private static void ValidateRank(int totalPlayers, int rank)
        {
            if (rank < 1 || rank > totalPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), $"rank must be between 1 and {totalPlayers}");
            }
        }
    }
}
