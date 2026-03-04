# warlock MVP reference core (Node + Unity)

Docs-first MVP reference implementation for Windows Unity/Fusion handoff.
This repo now contains:
- **Headless Node.js ESM reference core** (`src/`, `test/`)
- **Unity-oriented MVP core code + EditMode tests** (`Assets/Warlock/`, `Packages/`, `ProjectSettings/`)

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
- Network event constants + host-authoritative validation
- Runtime completion slice for MVP handoff:
  - Round transition UI contract presenter (A2)
  - 12-slot input binding map + rebinding validation (B1)
  - Loadout equip/unequip UI model (B2)
  - Host+Relay room/invite/event sync + disconnect/reconnect policy model (E1~E3)
- Fixed 12-skill catalog (`docs/22`) and version lock artifact (`src/versions-lock.json`)

Excluded:
- Team mode
- Random matchmaking
- Dedicated server

## Run
```bash
node --test
```

Current Node acceptance status: **18/18 passing**.

## Unity playable mode (local vertical slice)
You can now run a minimal-but-playable Unity loop in Editor.

### Quick start (empty scene bootstrap)
1. Open project in Unity `6000.0.68f1`.
2. Create an empty scene.
3. Add an empty GameObject named `WarlockPlayableBootstrap`.
4. Attach `Warlock.Playable.WarlockPlayableBootstrap` component.
5. Press **Play**.
6. Press **Space** to start.

Optional scene helper:
- Menu: **Warlock → Playable → Create Empty Playable Scene**

### Controls
- **Right click**: move local player.
- **QWERASDFZXCV**: cast equipped skills by loadout slot.
- **Shop phase**: buy skills from OnGUI panel, press **Enter** to close shop early.
- **Match end**: press **R** to restart.

### What is playable
- FFA 2~8 actors (1 local player + simple bots).
- Loop: `Waiting -> Countdown -> Combat -> RoundEnd -> Shop -> MatchEnd`.
- Boundary shrink + boundary DoT during combat.
- Round ranking, points/gold payout, between-round shopping.
- Skill casting mapped to all 12 slots; multiple skills have visible combat effects (damage/knockback/mobility/control/defense).

## Unity MVP code (docs/20 mapping)
Grounded in the same docs order above, now materialized under Unity paths:
- `Assets/Warlock/Scripts/Mvp/` - room validation, round FSM, economy, tie-breaks, loadout/cast validation, boundary/shrink, network contract + runtime presenter/binding/session models, scope guard, fixed 12-skill catalog
- `Assets/Warlock/Scripts/Playable/` - engine-referenced local playable bootstrap/runtime (`Warlock.Playable` asmdef)
- `Assets/Warlock/Tests/EditMode/` - acceptance-mapped EditMode tests including runtime flow/network session coverage (A~F)
- `ProjectSettings/ProjectVersion.txt` - Unity lock `6000.0.68f1`
- `Packages/manifest.json` + `Packages/warlock-mvp-lock.json` - MVP package/version lock intent (URP, Input System 1.17.0, Fusion 2.0.11 Stable build 1743)

## Structure
- `src/mvp-core.js` - minimal match core orchestration
- `src/round-state-machine.js` - fixed round state machine
- `src/economy.js` - rank points/gold + winner decision
- `src/loadout.js` - 12-slot equip rules
- `src/cast-validation.js` - cooldown/combat cast validation
- `src/boundary.js` - DoT + shrink formulas
- `src/network-contract.js` - network event contract + host validation stubs
- `src/mvp-runtime.js` - round UI presenter, input binding, loadout UI model, host/relay session model
- `src/skill-catalog.js` - fixed 12-skill catalog
- `src/versions-lock.json` + `src/version-lock.js` - engine/network/package lock
- `test/mvp-reference-core.test.js` - acceptance-mapped tests (A-D + E + runtime flow/network session)

## Unity/Fusion mapping intent
- `RoundStateMachine` / `MvpMatchCore` -> Fusion host state authority flow
- `validateHost*` stubs -> host-side RPC/event validation hooks
- `radiusAtTime` / `stepBoundaryState` -> deterministic server tick logic
- `SKILL_CATALOG` / loadout validation -> shared gameplay data contract for client UI + host checks
- `HostRelaySession` -> host-driven room/invite/event ordering + safe disconnect policy contract

## Known limitations
- Playable slice uses lightweight runtime primitives/OnGUI (no production UI/prefab polish yet).
- Bots are intentionally simple and deterministic enough for local validation.
- Still out of scope: team mode, random matchmaking, dedicated server.
- Security contract note: `actorId` must be bound from authenticated transport/session context (never trust raw client payload).
