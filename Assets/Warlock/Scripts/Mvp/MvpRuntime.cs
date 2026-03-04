using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock.Mvp
{
    public readonly struct RoundUiState
    {
        public RoundUiState(RoundState phase, string title, string primaryAction, string roundText)
        {
            Phase = phase;
            Title = title;
            PrimaryAction = primaryAction;
            RoundText = roundText;
        }

        public RoundState Phase { get; }
        public string Title { get; }
        public string PrimaryAction { get; }
        public string RoundText { get; }
    }

    public static class RoundTransitionPresenter
    {
        public static RoundUiState Present(RoundState phase, int currentRound, int roundsTotal)
        {
            var roundText = currentRound > 0 && roundsTotal > 0
                ? $"Round {currentRound}/{roundsTotal}"
                : "Round N/A";

            return phase switch
            {
                RoundState.Waiting => new RoundUiState(phase, "Waiting For Players", "HostStartMatchWhenReady", roundText),
                RoundState.RoundStart => new RoundUiState(phase, "Round Start", "RoundIntroCountdown", roundText),
                RoundState.Combat => new RoundUiState(phase, "Combat", "EnableSkillCasting", roundText),
                RoundState.RoundEnd => new RoundUiState(phase, "Round End", "ShowRoundRanking", roundText),
                RoundState.Shop => new RoundUiState(phase, "Shop", "PurchaseAndEquipSkills", roundText),
                RoundState.MatchEnd => new RoundUiState(phase, "Match End", "ShowFinalWinner", roundText),
                _ => throw new ArgumentOutOfRangeException(nameof(phase), $"Unknown phase: {phase}")
            };
        }
    }

    public readonly struct BindingValidationResult
    {
        public BindingValidationResult(bool ok, IReadOnlyList<string> errors)
        {
            Ok = ok;
            Errors = errors;
        }

        public bool Ok { get; }
        public IReadOnlyList<string> Errors { get; }
    }

    public static class InputBindingRules
    {
        public static Dictionary<string, string> CreateDefaultSlotBindings()
        {
            return MvpConstants.LoadoutSlotKeys.ToDictionary(slot => slot, slot => slot);
        }

        public static BindingValidationResult Validate(IReadOnlyDictionary<string, string> slotToKey)
        {
            var errors = new List<string>();

            foreach (var slot in MvpConstants.LoadoutSlotKeys)
            {
                if (!slotToKey.ContainsKey(slot))
                {
                    errors.Add($"MISSING_SLOT_{slot}");
                }
            }

            if (slotToKey.Count != MvpConstants.LoadoutSlotKeys.Count)
            {
                errors.Add("SLOT_COUNT_MISMATCH");
            }

            var normalizedKeys = MvpConstants.LoadoutSlotKeys
                .Select(slot => slotToKey.TryGetValue(slot, out var key) ? key?.Trim().ToUpperInvariant() ?? string.Empty : string.Empty)
                .ToList();

            if (normalizedKeys.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add("INVALID_KEY");
            }

            if (normalizedKeys.Distinct(StringComparer.Ordinal).Count() != normalizedKeys.Count)
            {
                errors.Add("DUPLICATE_KEY_BINDING");
            }

            return new BindingValidationResult(errors.Count == 0, errors);
        }

        public static (bool ok, string? reason, Dictionary<string, string>? bindings) Rebind(
            IReadOnlyDictionary<string, string> slotToKey,
            string slot,
            string key)
        {
            if (!MvpConstants.LoadoutSlotKeys.Contains(slot))
            {
                return (false, "INVALID_SLOT", null);
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return (false, "INVALID_KEY", null);
            }

            var next = slotToKey.ToDictionary(entry => entry.Key, entry => entry.Value);
            next[slot] = key.Trim().ToUpperInvariant();

            var validation = Validate(next);
            if (!validation.Ok)
            {
                return (false, validation.Errors[0], null);
            }

            return (true, null, next);
        }
    }

    public sealed class LoadoutUiModel
    {
        private readonly ISet<string> _ownedSkills;
        private readonly Dictionary<string, string?> _loadout;

        public LoadoutUiModel(ISet<string> ownedSkills, Dictionary<string, string?> loadout)
        {
            _ownedSkills = ownedSkills;
            _loadout = loadout;
        }

        public RuleResult Equip(string slot, string skillId)
        {
            return LoadoutRules.EquipSkill(_ownedSkills, _loadout, slot, skillId);
        }

        public RuleResult Unequip(string slot)
        {
            if (!MvpConstants.LoadoutSlotKeys.Contains(slot))
            {
                return RuleResult.Fail("INVALID_SLOT");
            }

            _loadout[slot] = null;
            return RuleResult.Success();
        }

        public IReadOnlyDictionary<string, string?> Snapshot()
        {
            return new Dictionary<string, string?>(_loadout);
        }
    }

    public readonly struct HostRelayEvent
    {
        public HostRelayEvent(int seq, string type, IReadOnlyDictionary<string, object> payload)
        {
            Seq = seq;
            Type = type;
            Payload = payload;
        }

        public int Seq { get; }
        public string Type { get; }
        public IReadOnlyDictionary<string, object> Payload { get; }
    }

    public sealed class HostRelaySession
    {
        private static int _roomSequence;
        private static readonly HashSet<string> KnownEvents = new(
            NetworkContract.MatchEvents
                .Concat(NetworkContract.RoundEvents)
                .Concat(NetworkContract.CombatEvents)
                .Concat(NetworkContract.EconomyEvents)
                .Concat(NetworkContract.ArenaEvents),
            StringComparer.Ordinal
        );

        private readonly List<HostRelayEvent> _events = new();
        private readonly HashSet<string> _players = new(StringComparer.Ordinal);
        private readonly HashSet<string> _disconnectedPlayers = new(StringComparer.Ordinal);
        private int _lastSeq;

        public HostRelaySession(string hostId)
        {
            HostId = string.IsNullOrWhiteSpace(hostId)
                ? throw new ArgumentException("hostId is required", nameof(hostId))
                : hostId;
        }

        public string HostId { get; }
        public string? RoomId { get; private set; }
        public string? InviteCode { get; private set; }
        public RoomSettings? Settings { get; private set; }

        public (bool ok, string? reason) CreateRoom(int? maxPlayers, int? rounds)
        {
            var validation = RoomSettingsValidator.Validate(maxPlayers, rounds);
            if (!validation.Ok)
            {
                return (false, validation.Errors.FirstOrDefault() ?? "INVALID_ROOM_SETTINGS");
            }

            _roomSequence += 1;
            RoomId = $"room-{_roomSequence:0000}";
            InviteCode = $"WLK{_roomSequence:0000}";
            Settings = validation.Settings;
            _players.Clear();
            _disconnectedPlayers.Clear();
            _players.Add(HostId);

            PushEvent("RoomCreated", new Dictionary<string, object>
            {
                { "roomId", RoomId },
                { "inviteCode", InviteCode },
                { "hostId", HostId }
            });

            return (true, null);
        }

        public (bool ok, string? reason) JoinPlayer(string playerId)
        {
            if (RoomId == null || Settings == null)
            {
                return (false, "ROOM_NOT_CREATED");
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return (false, "INVALID_PLAYER_ID");
            }

            if (_players.Contains(playerId))
            {
                return (true, null);
            }

            if (_players.Count >= Settings.Value.MaxPlayers)
            {
                return (false, "ROOM_FULL");
            }

            _players.Add(playerId);
            PushEvent("PlayerJoined", new Dictionary<string, object>
            {
                { "roomId", RoomId },
                { "playerId", playerId }
            });
            return (true, null);
        }

        public (bool ok, string? reason, HostRelayEvent? @event) SyncEvent(string eventType, IReadOnlyDictionary<string, object>? payload = null)
        {
            if (!KnownEvents.Contains(eventType))
            {
                return (false, "UNKNOWN_EVENT_TYPE", null);
            }

            var created = PushEvent(eventType, payload ?? new Dictionary<string, object>());
            return (true, null, created);
        }

        public IReadOnlyList<HostRelayEvent> ReadEventsSince(int seq)
        {
            return _events.Where(evt => evt.Seq > seq).ToList();
        }

        public (bool ok, string action) OnDisconnect(string playerId)
        {
            if (!_players.Contains(playerId))
            {
                return (false, "REJECT_UNKNOWN_PLAYER");
            }

            if (playerId == HostId)
            {
                PushEvent("MatchEnded", new Dictionary<string, object>
                {
                    { "reason", "HOST_DISCONNECTED" }
                });
                return (true, "SAFE_TERMINATE");
            }

            _disconnectedPlayers.Add(playerId);
            PushEvent("PlayerEliminated", new Dictionary<string, object>
            {
                { "playerId", playerId },
                { "reason", "PLAYER_DISCONNECTED" }
            });
            return (true, "MARK_INACTIVE");
        }

        public (bool ok, string action, int resumeFromSeq) OnReconnect(string playerId)
        {
            if (!_players.Contains(playerId))
            {
                return (false, "REJECT_UNKNOWN_PLAYER", 0);
            }

            if (!_disconnectedPlayers.Contains(playerId))
            {
                return (true, "NO_OP", _lastSeq);
            }

            _disconnectedPlayers.Remove(playerId);
            PushEvent("PlayerJoined", new Dictionary<string, object>
            {
                { "playerId", playerId },
                { "reconnected", true },
                { "resumeFromSeq", _lastSeq }
            });
            return (true, "RESUME_FROM_LAST_SEQUENCE", _lastSeq);
        }

        private HostRelayEvent PushEvent(string type, IReadOnlyDictionary<string, object> payload)
        {
            _lastSeq += 1;
            var created = new HostRelayEvent(_lastSeq, type, payload);
            _events.Add(created);
            return created;
        }
    }
}
