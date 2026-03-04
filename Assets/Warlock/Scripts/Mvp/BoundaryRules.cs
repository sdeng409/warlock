using System;

namespace Warlock.Mvp
{
    public sealed class BoundaryState
    {
        public BoundaryState(float maxHp)
        {
            MaxHp = maxHp;
            Hp = maxHp;
        }

        public float MaxHp { get; }
        public float Hp { get; set; }
        public float OutsideAccumulatedSec { get; set; }
        public float InsideContinuousSec { get; set; }
        public float OutsideTickAccumulatorSec { get; set; }
    }

    public static class BoundaryRules
    {
        public static BoundaryState Create(float maxHp) => new(maxHp);

        public static BoundaryState Step(BoundaryState state, bool isOutside, float deltaSec)
        {
            if (isOutside)
            {
                state.InsideContinuousSec = 0f;
                state.OutsideAccumulatedSec += deltaSec;
                state.OutsideTickAccumulatorSec += deltaSec;

                var tickCount = (int)Math.Floor(state.OutsideTickAccumulatorSec / MvpConstants.BoundaryTickSeconds);
                if (tickCount > 0)
                {
                    state.OutsideTickAccumulatorSec -= tickCount * MvpConstants.BoundaryTickSeconds;
                    var perTickDamage = state.MaxHp * MvpConstants.BoundaryMaxHpPercentPerTick;
                    state.Hp = Math.Max(0f, state.Hp - (perTickDamage * tickCount));
                }

                return state;
            }

            state.InsideContinuousSec += deltaSec;
            if (state.InsideContinuousSec >= MvpConstants.BoundaryInsideResetSeconds)
            {
                state.OutsideAccumulatedSec = 0f;
                state.OutsideTickAccumulatorSec = 0f;
            }

            return state;
        }

        public static float RadiusAtTime(float initialRadius, float elapsedSec)
        {
            if (elapsedSec <= MvpConstants.ArenaShrinkStartAfterSeconds)
            {
                return initialRadius;
            }

            var shrinkElapsed = elapsedSec - MvpConstants.ArenaShrinkStartAfterSeconds;
            var shrinkRatio = Math.Max(MvpConstants.ArenaMinRadiusRatio, 1f - (shrinkElapsed * MvpConstants.ArenaShrinkPerSecond));
            return initialRadius * shrinkRatio;
        }
    }
}
