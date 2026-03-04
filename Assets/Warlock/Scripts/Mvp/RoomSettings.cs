using System.Collections.Generic;
using System.Linq;

namespace Warlock.Mvp
{
    public readonly struct RoomSettings
    {
        public RoomSettings(int maxPlayers, int rounds)
        {
            MaxPlayers = maxPlayers;
            Rounds = rounds;
        }

        public int MaxPlayers { get; }
        public int Rounds { get; }
    }

    public readonly struct RoomSettingsValidationResult
    {
        public RoomSettingsValidationResult(bool ok, IReadOnlyList<string> errors, RoomSettings settings)
        {
            Ok = ok;
            Errors = errors;
            Settings = settings;
        }

        public bool Ok { get; }
        public IReadOnlyList<string> Errors { get; }
        public RoomSettings Settings { get; }
    }

    public static class RoomSettingsValidator
    {
        public static RoomSettings Normalize(int? maxPlayers, int? rounds)
        {
            return new RoomSettings(
                maxPlayers ?? MvpConstants.MaxPlayers,
                rounds ?? MvpConstants.DefaultRounds
            );
        }

        public static RoomSettingsValidationResult Validate(int? maxPlayers, int? rounds)
        {
            var normalized = Normalize(maxPlayers, rounds);
            var errors = new List<string>();

            if (normalized.MaxPlayers < MvpConstants.MinPlayers || normalized.MaxPlayers > MvpConstants.MaxPlayers)
            {
                errors.Add($"maxPlayers must be between {MvpConstants.MinPlayers} and {MvpConstants.MaxPlayers}");
            }

            if (!MvpConstants.RoomRoundOptions.Contains(normalized.Rounds))
            {
                errors.Add($"rounds must be one of: {string.Join(", ", MvpConstants.RoomRoundOptions)}");
            }

            return new RoomSettingsValidationResult(errors.Count == 0, errors, normalized);
        }
    }
}
