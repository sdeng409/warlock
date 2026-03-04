using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock.Mvp
{
    public sealed class MvpMatchCore
    {
        private readonly Dictionary<string, PlayerProgress> _players;

        public MvpMatchCore(int? maxPlayers, int? rounds, IReadOnlyList<string> playerIds, string mode = MvpConstants.ModeFfa)
        {
            MvpScopeGuard.EnsureFfaOnly(mode);

            var validation = RoomSettingsValidator.Validate(maxPlayers, rounds);
            if (!validation.Ok)
            {
                throw new InvalidOperationException($"Invalid room settings: {string.Join("; ", validation.Errors)}");
            }

            if (playerIds.Count < MvpConstants.MinPlayers || playerIds.Count > validation.Settings.MaxPlayers)
            {
                throw new InvalidOperationException("Player count must be within room maxPlayers and MVP range");
            }

            Mode = mode;
            Settings = validation.Settings;
            StateMachine = new RoundStateMachine(Settings.Rounds);
            _players = playerIds.Distinct(StringComparer.Ordinal).ToDictionary(
                id => id,
                id => new PlayerProgress(id)
            );
        }

        public string Mode { get; }
        public RoomSettings Settings { get; }
        public RoundStateMachine StateMachine { get; }
        public RoundState Phase => StateMachine.State;

        public void StartRound()
        {
            StateMachine.StartRound();
            StateMachine.BeginCombat();
        }

        public (RoundState nextPhase, WinnerDecision? final) EndRoundWithRanks(IReadOnlyList<RoundRank> roundRanks)
        {
            if (Phase != RoundState.Combat)
            {
                throw new InvalidOperationException("Round can end only during Combat");
            }

            StateMachine.EndCombat();
            EconomyRules.ApplyRoundEconomy(_players.Values.ToList(), roundRanks);
            var next = StateMachine.AdvanceAfterRoundEnd();

            if (next == RoundState.Shop)
            {
                return (RoundState.Shop, null);
            }

            return (RoundState.MatchEnd, EconomyRules.DecideFinalWinner(_players.Values.ToList()));
        }

        public RuleResult PurchaseSkill(string playerId, string skillId)
        {
            if (Phase != RoundState.Shop)
            {
                return RuleResult.Fail("SHOP_PHASE_REQUIRED");
            }

            if (!_players.TryGetValue(playerId, out var player) || !SkillCatalog.ById.TryGetValue(skillId, out var skill))
            {
                return RuleResult.Fail("INVALID_PLAYER_OR_SKILL");
            }

            if (player.Gold < skill.Price)
            {
                return RuleResult.Fail("INSUFFICIENT_GOLD");
            }

            player.Gold -= skill.Price;
            player.OwnedSkills.Add(skillId);
            return RuleResult.Success();
        }

        public RuleResult EquipSkill(string playerId, string slot, string skillId)
        {
            if (!_players.TryGetValue(playerId, out var player))
            {
                return RuleResult.Fail("INVALID_PLAYER");
            }

            return LoadoutRules.EquipSkill(player.OwnedSkills, player.Loadout, slot, skillId);
        }

        public void CloseShopAndStartNextRound()
        {
            if (Phase != RoundState.Shop)
            {
                throw new InvalidOperationException("Can only close shop during Shop phase");
            }

            StartRound();
        }

        public PlayerProgress GetPlayer(string playerId) => _players[playerId];

        public IReadOnlyList<PlayerProgress> GetStandingsSnapshot() => _players.Values.ToList();
    }
}
