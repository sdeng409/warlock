using NUnit.Framework;
using Warlock.Mvp;

namespace Warlock.Mvp.Tests.EditMode
{
    public class B_LoadoutCastTests
    {
        [Test]
        public void Loadout_Enforces12SlotsUnownedAndDuplicateRules()
        {
            var core = new MvpMatchCore(2, 3, new[] { "p1", "p2" });
            core.StartRound();
            core.EndRoundWithRanks(new[] { new RoundRank("p1", 1), new RoundRank("p2", 2) });

            var player = core.GetPlayer("p1");
            player.Gold = 10;

            Assert.That(core.PurchaseSkill("p1", "S01").Ok, Is.True);
            Assert.That(core.PurchaseSkill("p1", "S02").Ok, Is.True);

            Assert.That(player.Loadout.Count, Is.EqualTo(12));
            CollectionAssert.AreEquivalent(MvpConstants.LoadoutSlotKeys, player.Loadout.Keys);

            Assert.That(core.EquipSkill("p1", "Q", "S01").Ok, Is.True);
            Assert.That(core.EquipSkill("p1", "W", "S02").Ok, Is.True);

            var duplicate = core.EquipSkill("p1", "E", "S01");
            Assert.That(duplicate.Ok, Is.False);
            Assert.That(duplicate.Reason, Is.EqualTo("DUPLICATE_SKILL_NOT_ALLOWED"));

            var unowned = core.EquipSkill("p1", "R", "S03");
            Assert.That(unowned.Ok, Is.False);
            Assert.That(unowned.Reason, Is.EqualTo("UNOWNED_SKILL"));
        }

        [Test]
        public void CastValidation_IsCooldownOnlyAndCombatOnly()
        {
            var core = new MvpMatchCore(2, 3, new[] { "p1", "p2" });
            core.StartRound();
            core.EndRoundWithRanks(new[] { new RoundRank("p1", 1), new RoundRank("p2", 2) });

            var player = core.GetPlayer("p1");
            player.Gold = 10;
            core.PurchaseSkill("p1", "S01");
            core.EquipSkill("p1", "Q", "S01");
            core.CloseShopAndStartNextRound();

            var validator = new CastValidator();
            var castOk = validator.ValidateAndCommit(core.Phase, "p1", "S01", 0, player.OwnedSkills, player.Loadout);
            Assert.That(castOk.Ok, Is.True);

            var cooldownRejected = validator.ValidateAndCommit(core.Phase, "p1", "S01", 1000, player.OwnedSkills, player.Loadout);
            Assert.That(cooldownRejected.Ok, Is.False);
            Assert.That(cooldownRejected.Reason, Is.EqualTo("COOLDOWN_ACTIVE"));

            var nonCombat = validator.ValidateAndCommit(RoundState.Shop, "p1", "S01", 6000, player.OwnedSkills, player.Loadout);
            Assert.That(nonCombat.Ok, Is.False);
            Assert.That(nonCombat.Reason, Is.EqualTo("CAST_ONLY_ALLOWED_IN_COMBAT"));
        }
    }
}
