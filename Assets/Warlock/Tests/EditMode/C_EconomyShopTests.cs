using NUnit.Framework;
using Warlock.Mvp;

namespace Warlock.Mvp.Tests.EditMode
{
    public class C_EconomyShopTests
    {
        [Test]
        public void Economy_RankPointsAndGoldFollowRankOrder()
        {
            Assert.That(EconomyRules.RankPointsFor(8, 1), Is.EqualTo(8));
            Assert.That(EconomyRules.RankPointsFor(8, 8), Is.EqualTo(1));
            Assert.That(EconomyRules.GoldPayoutFor(8, 1), Is.EqualTo(8));
            Assert.That(EconomyRules.GoldPayoutFor(8, 8), Is.EqualTo(1));
        }

        [Test]
        public void ShopPurchase_DeductsGoldAndAddsOwnership_NoRerollNoResale()
        {
            var core = new MvpMatchCore(2, 3, new[] { "p1", "p2" });
            core.StartRound();
            core.EndRoundWithRanks(new[] { new RoundRank("p1", 1), new RoundRank("p2", 2) });

            var buy = core.PurchaseSkill("p1", "S01");
            Assert.That(buy.Ok, Is.True);
            Assert.That(core.GetPlayer("p1").Gold, Is.EqualTo(0));
            Assert.That(core.GetPlayer("p1").OwnedSkills.Contains("S01"), Is.True);
        }
    }
}
