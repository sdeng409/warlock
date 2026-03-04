using System.Collections.Generic;

namespace Warlock.Mvp
{
    public readonly struct CastValidationResult
    {
        public CastValidationResult(bool ok, string? reason = null, long retryAfterMs = 0, long committedCooldownUntilMs = 0)
        {
            Ok = ok;
            Reason = reason;
            RetryAfterMs = retryAfterMs;
            CommittedCooldownUntilMs = committedCooldownUntilMs;
        }

        public bool Ok { get; }
        public string? Reason { get; }
        public long RetryAfterMs { get; }
        public long CommittedCooldownUntilMs { get; }
    }

    public sealed class CastValidator
    {
        private readonly Dictionary<string, long> _nextCastReadyAtMs = new();

        public CastValidationResult ValidateAndCommit(
            RoundState phase,
            string playerId,
            string skillId,
            long nowMs,
            ISet<string> ownedSkills,
            IReadOnlyDictionary<string, string?> loadout)
        {
            if (phase != RoundState.Combat)
            {
                return new CastValidationResult(false, "CAST_ONLY_ALLOWED_IN_COMBAT");
            }

            if (!SkillCatalog.ById.TryGetValue(skillId, out var skill))
            {
                return new CastValidationResult(false, "UNKNOWN_SKILL");
            }

            if (!ownedSkills.Contains(skillId))
            {
                return new CastValidationResult(false, "SKILL_NOT_OWNED");
            }

            if (!LoadoutRules.IsSkillEquipped(loadout, skillId))
            {
                return new CastValidationResult(false, "SKILL_NOT_EQUIPPED");
            }

            var key = $"{playerId}:{skillId}";
            var readyAtMs = _nextCastReadyAtMs.TryGetValue(key, out var existing) ? existing : 0;

            if (nowMs < readyAtMs)
            {
                return new CastValidationResult(false, "COOLDOWN_ACTIVE", retryAfterMs: readyAtMs - nowMs);
            }

            var cooldownUntil = nowMs + (skill.CooldownSec * 1000L);
            _nextCastReadyAtMs[key] = cooldownUntil;
            return new CastValidationResult(true, committedCooldownUntilMs: cooldownUntil);
        }
    }
}
