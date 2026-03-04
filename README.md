# warlock MVP headless reference core

Docs-first MVP reference implementation for Windows Unity/Fusion handoff.
This is a **headless Node.js ESM core** that encodes gameplay/network contracts from docs so Unity 6000.0.68f1 + Fusion 2.0.11 implementation can follow the same rules.

## Docs baseline (from `docs/20`)
Implementation was aligned in this order:
1. `docs/00-one-pager.md`
2. `docs/12-vertical-slice-scope.md`
3. `docs/05-skill-system.md`
4. `docs/17-round-economy-and-skill-shop.md`
5. `docs/07-networking-overview.md`
6. `docs/25-engine-lock-and-package-versions.md`
7. `docs/19-arena-shrink-rules.md`
8. `docs/01-decision-log.md`

## Scope (MVP only)
Included:
- FFA round loop: `Waiting -> RoundStart -> Combat -> RoundEnd -> Shop -> MatchEnd`
- Host room setting validation (2~8 players, rounds 3/5/7, default 5)
- Rank points + gold payout by round rank
- Final winner tie-breakers: total points > first-place count > last-round rank
- Classless 12-slot loadout (`QWERASDFZXCV`), no duplicate skill equip, no unowned equip
- Cooldown-only cast validation + cast rejection outside Combat
- Boundary DoT: 1s tick, 5% max HP
- Outside accumulation reset rule: inside for continuous 3s to reset
- Arena shrink: starts at 30s, 1.5%/s, min 35%
- Network event constants + host-authoritative validation stubs
- Fixed 12-skill catalog (`docs/22`) and version lock artifact (`src/versions-lock.json`)

Excluded:
- Team mode
- Random matchmaking
- Dedicated server

## Run
```bash
node --test
```

## Structure
- `src/mvp-core.js` - minimal match core orchestration
- `src/round-state-machine.js` - fixed round state machine
- `src/economy.js` - rank points/gold + winner decision
- `src/loadout.js` - 12-slot equip rules
- `src/cast-validation.js` - cooldown/combat cast validation
- `src/boundary.js` - DoT + shrink formulas
- `src/network-contract.js` - network event contract + host validation stubs
- `src/skill-catalog.js` - fixed 12-skill catalog
- `src/versions-lock.json` + `src/version-lock.js` - engine/network/package lock
- `test/mvp-reference-core.test.js` - acceptance-mapped tests (A-D required + E minimal)

## Unity/Fusion mapping intent
- `RoundStateMachine` / `MvpMatchCore` -> Fusion host state authority flow
- `validateHost*` stubs -> host-side RPC/event validation hooks
- `radiusAtTime` / `stepBoundaryState` -> deterministic server tick logic
- `SKILL_CATALOG` / loadout validation -> shared gameplay data contract for client UI + host checks
