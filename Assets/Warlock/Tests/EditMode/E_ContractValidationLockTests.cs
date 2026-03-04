using NUnit.Framework;
using Warlock.Mvp;

namespace Warlock.Mvp.Tests.EditMode
{
    public class E_ContractValidationLockTests
    {
        [Test]
        public void NetworkContractAndScopeGuard_AreLockedForMvp()
        {
            CollectionAssert.Contains(NetworkContract.CombatEvents, "SkillCastRequested");
            CollectionAssert.Contains(NetworkContract.EconomyEvents, "ShopPurchaseRejected");
            CollectionAssert.Contains(NetworkContract.ArenaEvents, "BoundaryDotTick");

            var castReject = NetworkContract.ValidateHostSkillCastRequest(RoundState.Shop, new CastValidationResult(true));
            Assert.That(castReject.Ok, Is.False);

            var shopReject = NetworkContract.ValidateHostShopPurchase(RoundState.Shop, -1, 1);
            Assert.That(shopReject.Ok, Is.False);
            Assert.That(shopReject.Reason, Is.EqualTo("NEGATIVE_GOLD_INVALID"));

            Assert.That(MvpScopeGuard.IsExplicitlyExcluded("team-mode"), Is.True);
            Assert.That(MvpScopeGuard.IsExplicitlyExcluded("random-matchmaking"), Is.True);
            Assert.That(MvpScopeGuard.IsExplicitlyExcluded("dedicated-server"), Is.True);
            Assert.That(SkillCatalog.Fixed12.Count, Is.EqualTo(12));
            Assert.That(MvpVersionLock.IsLocked("6000.0.68f1", "2.0.11 Stable", "1743", "1.17.0"), Is.True);
        }
    }
}
