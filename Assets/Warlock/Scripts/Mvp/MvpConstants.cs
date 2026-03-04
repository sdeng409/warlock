using System;
using System.Collections.Generic;
using System.Linq;

namespace Warlock.Mvp
{
    public static class MvpConstants
    {
        public const string ModeFfa = "FFA";

        public static readonly IReadOnlyList<string> ExplicitMvpExclusions = new[]
        {
            "team-mode",
            "random-matchmaking",
            "dedicated-server"
        };

        public static readonly IReadOnlyList<string> LoadoutSlotKeys = "QWERASDFZXCV".ToCharArray().Select(c => c.ToString()).ToArray();

        public static readonly IReadOnlyList<int> RoomRoundOptions = new[] { 3, 5, 7 };

        public const int DefaultRounds = 5;
        public const int MinPlayers = 2;
        public const int MaxPlayers = 8;

        public const float BoundaryTickSeconds = 1f;
        public const float BoundaryMaxHpPercentPerTick = 0.05f;
        public const float BoundaryInsideResetSeconds = 3f;

        public const float ArenaShrinkStartAfterSeconds = 30f;
        public const float ArenaShrinkPerSecond = 0.015f;
        public const float ArenaMinRadiusRatio = 0.35f;
    }

    public enum RoundState
    {
        Waiting,
        RoundStart,
        Combat,
        RoundEnd,
        Shop,
        MatchEnd
    }
}
