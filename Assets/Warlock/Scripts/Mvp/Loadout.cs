using System.Collections.Generic;
using System.Linq;

namespace Warlock.Mvp
{
    public readonly struct RuleResult
    {
        public RuleResult(bool ok, string? reason = null)
        {
            Ok = ok;
            Reason = reason;
        }

        public bool Ok { get; }
        public string? Reason { get; }

        public static RuleResult Success() => new(true);
        public static RuleResult Fail(string reason) => new(false, reason);
    }

    public static class LoadoutRules
    {
        public static Dictionary<string, string?> CreateEmptyLoadout()
        {
            return MvpConstants.LoadoutSlotKeys.ToDictionary(slot => slot, _ => (string?)null);
        }

        public static RuleResult EquipSkill(ISet<string> ownedSkills, Dictionary<string, string?> loadout, string slot, string skillId)
        {
            if (!MvpConstants.LoadoutSlotKeys.Contains(slot))
            {
                return RuleResult.Fail("INVALID_SLOT");
            }

            if (!SkillCatalog.ById.ContainsKey(skillId))
            {
                return RuleResult.Fail("UNKNOWN_SKILL");
            }

            if (!ownedSkills.Contains(skillId))
            {
                return RuleResult.Fail("UNOWNED_SKILL");
            }

            foreach (var key in MvpConstants.LoadoutSlotKeys)
            {
                if (key != slot && loadout[key] == skillId)
                {
                    return RuleResult.Fail("DUPLICATE_SKILL_NOT_ALLOWED");
                }
            }

            loadout[slot] = skillId;
            return RuleResult.Success();
        }

        public static bool IsSkillEquipped(IReadOnlyDictionary<string, string?> loadout, string skillId)
        {
            return MvpConstants.LoadoutSlotKeys.Any(slot => loadout.TryGetValue(slot, out var equipped) && equipped == skillId);
        }
    }
}
