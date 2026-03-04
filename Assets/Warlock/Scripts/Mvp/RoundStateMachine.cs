using System;
using System.Collections.Generic;

namespace Warlock.Mvp
{
    public sealed class RoundStateMachine
    {
        private static readonly Dictionary<RoundState, HashSet<RoundState>> AllowedTransitions = new()
        {
            { RoundState.Waiting, new HashSet<RoundState> { RoundState.RoundStart } },
            { RoundState.RoundStart, new HashSet<RoundState> { RoundState.Combat } },
            { RoundState.Combat, new HashSet<RoundState> { RoundState.RoundEnd } },
            { RoundState.RoundEnd, new HashSet<RoundState> { RoundState.Shop, RoundState.MatchEnd } },
            { RoundState.Shop, new HashSet<RoundState> { RoundState.RoundStart, RoundState.MatchEnd } },
            { RoundState.MatchEnd, new HashSet<RoundState>() }
        };

        public RoundStateMachine(int totalRounds = MvpConstants.DefaultRounds)
        {
            if (totalRounds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalRounds), "totalRounds must be positive");
            }

            TotalRounds = totalRounds;
            CurrentRound = 0;
            State = RoundState.Waiting;
            History = new List<RoundState> { State };
        }

        public int TotalRounds { get; }
        public int CurrentRound { get; private set; }
        public RoundState State { get; private set; }
        public List<RoundState> History { get; }

        public bool CanTransition(RoundState nextState) => AllowedTransitions[State].Contains(nextState);

        public void Transition(RoundState nextState)
        {
            if (!CanTransition(nextState))
            {
                throw new InvalidOperationException($"Invalid transition: {State} -> {nextState}");
            }

            State = nextState;
            History.Add(nextState);
        }

        public void StartRound()
        {
            if (State == RoundState.Waiting || State == RoundState.Shop)
            {
                CurrentRound += 1;
            }

            Transition(RoundState.RoundStart);
        }

        public void BeginCombat() => Transition(RoundState.Combat);

        public void EndCombat() => Transition(RoundState.RoundEnd);

        public void OpenShop() => Transition(RoundState.Shop);

        public void EndMatch()
        {
            if (!CanTransition(RoundState.MatchEnd))
            {
                throw new InvalidOperationException($"Cannot end match from state: {State}");
            }

            Transition(RoundState.MatchEnd);
        }

        public RoundState AdvanceAfterRoundEnd()
        {
            if (CurrentRound >= TotalRounds)
            {
                EndMatch();
                return State;
            }

            OpenShop();
            return State;
        }
    }
}
