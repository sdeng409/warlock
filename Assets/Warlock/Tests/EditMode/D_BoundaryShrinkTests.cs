using NUnit.Framework;
using Warlock.Mvp;

namespace Warlock.Mvp.Tests.EditMode
{
    public class D_BoundaryShrinkTests
    {
        [Test]
        public void Boundary_DoTAppliesAndResetsOnlyAfter3SecondsInside()
        {
            var state = BoundaryRules.Create(100f);

            BoundaryRules.Step(state, true, 2.2f);
            Assert.That(state.Hp, Is.EqualTo(90f));
            Assert.That(state.OutsideAccumulatedSec, Is.EqualTo(2.2f).Within(0.001f));

            BoundaryRules.Step(state, false, 2.0f);
            Assert.That(state.Hp, Is.EqualTo(90f));
            Assert.That(state.OutsideAccumulatedSec, Is.EqualTo(2.2f).Within(0.001f));

            BoundaryRules.Step(state, true, 1.0f);
            Assert.That(state.Hp, Is.EqualTo(85f));
            Assert.That(state.OutsideAccumulatedSec, Is.EqualTo(3.2f).Within(0.001f));

            BoundaryRules.Step(state, false, 3.0f);
            Assert.That(state.OutsideAccumulatedSec, Is.EqualTo(0f));
            Assert.That(state.OutsideTickAccumulatorSec, Is.EqualTo(0f));
        }

        [Test]
        public void ArenaShrink_StartsAt30SecondsShrinks15PerSecondClampsAt35Percent()
        {
            Assert.That(BoundaryRules.RadiusAtTime(100f, 29f), Is.EqualTo(100f));
            Assert.That(BoundaryRules.RadiusAtTime(100f, 30f), Is.EqualTo(100f));
            Assert.That(BoundaryRules.RadiusAtTime(100f, 40f), Is.EqualTo(85f));
            Assert.That(BoundaryRules.RadiusAtTime(100f, 1000f), Is.EqualTo(35f));
        }
    }
}
