using System.Collections.Generic;

namespace Warlock.Mvp
{
    public static class NetworkContract
    {
        public static readonly IReadOnlyList<string> MatchEvents = new[]
        {
            "RoomCreated", "PlayerJoined", "RoomSettingsUpdated"
        };

        public static readonly IReadOnlyList<string> RoundEvents = new[]
        {
            "RoundStarted", "RoundEnded", "MatchEnded"
        };

        public static readonly IReadOnlyList<string> CombatEvents = new[]
        {
            "SkillCastRequested", "SkillCastConfirmed", "DamageApplied", "KnockbackApplied", "PlayerEliminated"
        };

        public static readonly IReadOnlyList<string> EconomyEvents = new[]
        {
            "RankPointsGranted", "GoldGranted", "ShopPurchaseRequested", "ShopPurchaseConfirmed", "ShopPurchaseRejected", "LoadoutUpdated"
        };

        public static readonly IReadOnlyList<string> ArenaEvents = new[]
        {
            "ShrinkStateUpdated", "BoundaryDotTick"
        };

        public static RuleResult ValidateHostSkillCastRequest(RoundState phase, CastValidationResult cooldownValidation)
        {
            if (phase != RoundState.Combat)
            {
                return RuleResult.Fail("ROUND_PHASE_INVALID");
            }

            if (!cooldownValidation.Ok)
            {
                return RuleResult.Fail(cooldownValidation.Reason ?? "COOLDOWN_VALIDATION_FAILED");
            }

            return RuleResult.Success();
        }

        public static RuleResult ValidateHostShopPurchase(RoundState phase, int playerGold, int price)
        {
            if (phase != RoundState.Shop)
            {
                return RuleResult.Fail("SHOP_PHASE_REQUIRED");
            }

            if (playerGold < 0)
            {
                return RuleResult.Fail("NEGATIVE_GOLD_INVALID");
            }

            if (price < 0)
            {
                return RuleResult.Fail("NEGATIVE_PRICE_INVALID");
            }

            if (playerGold < price)
            {
                return RuleResult.Fail("INSUFFICIENT_GOLD");
            }

            return RuleResult.Success();
        }
    }
}
