using NUnit.Framework;
using Warlock.Mvp;

namespace Warlock.Mvp.Tests.EditMode
{
    public class A_MatchLoopTests
    {
        [Test]
        public void MatchLoop_RepeatsAndEndsOnMatchEnd_Default5Rounds()
        {
            var core = new MvpMatchCore(4, null, new[] { "p1", "p2", "p3", "p4" });
            core.StartRound();

            for (var round = 1; round <= 5; round++)
            {
                Assert.That(core.Phase, Is.EqualTo(RoundState.Combat));

                var result = core.EndRoundWithRanks(new[]
                {
                    new RoundRank("p1", 1),
                    new RoundRank("p2", 2),
                    new RoundRank("p3", 3),
                    new RoundRank("p4", 4)
                });

                if (round < 5)
                {
                    Assert.That(result.nextPhase, Is.EqualTo(RoundState.Shop));
                    core.CloseShopAndStartNextRound();
                }
                else
                {
                    Assert.That(result.nextPhase, Is.EqualTo(RoundState.MatchEnd));
                    Assert.That(core.Phase, Is.EqualTo(RoundState.MatchEnd));
                }
            }
        }

        [Test]
        public void WinnerTiebreak_UsesPointsThenFirstPlaceCountThenLastRoundRank()
        {
            var core = new MvpMatchCore(4, 3, new[] { "A", "B", "C", "D" });
            core.StartRound();

            core.EndRoundWithRanks(new[] { new RoundRank("A", 1), new RoundRank("B", 2), new RoundRank("C", 3), new RoundRank("D", 4) });
            core.CloseShopAndStartNextRound();

            core.EndRoundWithRanks(new[] { new RoundRank("A", 1), new RoundRank("B", 3), new RoundRank("C", 2), new RoundRank("D", 4) });
            core.CloseShopAndStartNextRound();

            var final = core.EndRoundWithRanks(new[] { new RoundRank("B", 1), new RoundRank("C", 2), new RoundRank("D", 3), new RoundRank("A", 4) });

            Assert.That(final.final!.Value.Winner.Id, Is.EqualTo("A"));
            Assert.That(final.final!.Value.Winner.TotalPoints, Is.EqualTo(9));
            Assert.That(core.GetPlayer("B").TotalPoints, Is.EqualTo(9));
            Assert.That(final.final!.Value.Winner.FirstPlaceCount, Is.EqualTo(2));
        }
    }
}
