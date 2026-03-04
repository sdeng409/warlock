using System.Collections.Generic;
using System.Linq;

namespace Warlock.Mvp
{
    public readonly struct SkillCard
    {
        public SkillCard(string id, string name, string tag, string castType, int cooldownSec, float pushForce, int price)
        {
            Id = id;
            Name = name;
            Tag = tag;
            CastType = castType;
            CooldownSec = cooldownSec;
            PushForce = pushForce;
            Price = price;
        }

        public string Id { get; }
        public string Name { get; }
        public string Tag { get; }
        public string CastType { get; }
        public int CooldownSec { get; }
        public float PushForce { get; }
        public int Price { get; }
    }

    public static class SkillCatalog
    {
        public static readonly IReadOnlyList<SkillCard> Fixed12 = new[]
        {
            new SkillCard("S01", "Arc Push", "Knockback", "Direction", 5, 1.0f, 2),
            new SkillCard("S02", "Blast Palm", "Knockback", "Target", 7, 1.3f, 3),
            new SkillCard("S03", "Shock Ring", "Knockback", "Area", 10, 1.1f, 4),
            new SkillCard("S04", "Overdrive Wave", "Knockback", "Direction", 14, 1.6f, 6),
            new SkillCard("S05", "Ember Bolt", "Damage", "Direction", 4, 0.2f, 2),
            new SkillCard("S06", "Void Spear", "Damage", "Target", 8, 0.1f, 4),
            new SkillCard("S07", "Blink Step", "Mobility", "Self", 10, 0.0f, 3),
            new SkillCard("S08", "Dash Burst", "Mobility", "Direction", 12, 0.4f, 4),
            new SkillCard("S09", "Slow Field", "Control", "Area", 12, 0.0f, 3),
            new SkillCard("S10", "Silence Pulse", "Control", "Area", 15, 0.0f, 5),
            new SkillCard("S11", "Guard Shell", "Defense", "Self", 13, 0.0f, 4),
            new SkillCard("S12", "Barrier Wall", "Defense", "Area", 16, 0.0f, 5)
        };

        public static readonly IReadOnlyDictionary<string, SkillCard> ById = Fixed12.ToDictionary(s => s.Id, s => s);
    }
}
