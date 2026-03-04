using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warlock.Mvp;

namespace Warlock.Playable
{
    public sealed class WarlockPlayableBootstrap : MonoBehaviour
    {
        private enum RuntimePhase
        {
            Waiting,
            Countdown,
            Combat,
            RoundEnd,
            Shop,
            MatchEnd
        }

        private sealed class ActorState
        {
            public ActorState(string id, bool isBot, GameObject body)
            {
                Id = id;
                IsBot = isBot;
                Body = body;
                Boundary = BoundaryRules.Create(MaxHp);
            }

            public const float MaxHp = 100f;

            public string Id { get; }
            public bool IsBot { get; }
            public GameObject Body { get; }
            public BoundaryState Boundary { get; }
            public float Hp = MaxHp;
            public float Shield;
            public bool Alive = true;
            public Vector3 MoveTarget;
            public float SlowUntil;
            public float SilenceUntil;
            public float BotThinkAt;
            public float BotCastAt;
            public int EliminationOrder = -1;

            public float MoveSpeed(float now) => now < SlowUntil ? 3f : 5f;
            public bool IsSilenced(float now) => now < SilenceUntil;
        }

        [Header("Match")]
        [SerializeField] [Range(2, 8)] private int playerCount = 4;
        [SerializeField] [Range(3, 7)] private int rounds = 3;
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private float roundEndSeconds = 4f;
        [SerializeField] private float shopSeconds = 12f;
        [SerializeField] private float maxCombatSeconds = 75f;

        [Header("Arena")]
        [SerializeField] private float arenaRadius = 18f;
        private readonly Dictionary<string, ActorState> _actors = new(StringComparer.Ordinal);
        private readonly Dictionary<string, KeyCode> _slotKeyCodes = new();
        private readonly List<string> _roundResultLines = new();

        private CastValidator _castValidator = new();

        private MvpMatchCore? _core;
        private RuntimePhase _runtimePhase;
        private string _localPlayerId = "P1";
        private string _status = string.Empty;
        private float _phaseTimer;
        private float _combatTimer;
        private int _eliminationSequence;
        private float _currentBoundaryRadius;
        private WinnerDecision? _winner;

        private GameObject? _arenaVisual;
        private Camera? _mainCamera;

        private static readonly string[] StarterSkills = { "S01", "S05", "S07", "S11" };

        private void Awake()
        {
            Application.targetFrameRate = 60;
            rounds = NormalizeRounds(rounds);
            BuildSlotKeys();
            BootstrapSceneIfNeeded();
            StartPlayableMatch();
        }

        private void Update()
        {
            if (_core == null)
            {
                return;
            }

            switch (_runtimePhase)
            {
                case RuntimePhase.Waiting:
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        BeginCountdown();
                    }
                    break;
                case RuntimePhase.Countdown:
                    _phaseTimer -= Time.deltaTime;
                    if (_phaseTimer <= 0f)
                    {
                        StartCombatRound();
                    }
                    break;
                case RuntimePhase.Combat:
                    TickLocalInput();
                    TickBots();
                    TickMovement();
                    TickBoundaryAndEliminations();
                    TickCombatEndCheck();
                    break;
                case RuntimePhase.RoundEnd:
                    _phaseTimer -= Time.deltaTime;
                    if (_phaseTimer <= 0f)
                    {
                        if (_core.Phase == RoundState.Shop)
                        {
                            EnterShop();
                        }
                        else
                        {
                            EnterMatchEnd();
                        }
                    }
                    break;
                case RuntimePhase.Shop:
                    TickBotsShop();
                    _phaseTimer -= Time.deltaTime;
                    if (_phaseTimer <= 0f || Input.GetKeyDown(KeyCode.Return))
                    {
                        BeginCountdown();
                    }
                    break;
                case RuntimePhase.MatchEnd:
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        ResetAndRestart();
                    }
                    break;
            }

            UpdateArenaVisual();
        }

        private void OnGUI()
        {
            if (_core == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 470, 760), GUI.skin.box);
            GUILayout.Label("Warlock MVP - Local Playable Slice");
            GUILayout.Label($"Runtime Phase: {_runtimePhase}");
            GUILayout.Label($"Core Phase: {_core.Phase} | Round: {_core.StateMachine.CurrentRound}/{_core.Settings.Rounds}");
            GUILayout.Label($"Arena Radius: {_currentBoundaryRadius:0.0}");
            GUILayout.Label($"Status: {_status}");

            if (_runtimePhase == RuntimePhase.Waiting)
            {
                GUILayout.Label("Press [Space] to start match");
            }

            if (_runtimePhase == RuntimePhase.Countdown)
            {
                GUILayout.Label($"Round starts in {_phaseTimer:0.0}s");
            }

            if (_runtimePhase == RuntimePhase.Combat)
            {
                GUILayout.Label($"Combat ends in {Mathf.Max(0f, maxCombatSeconds - _combatTimer):0.0}s");
                GUILayout.Label("Move: Right click on floor");
                GUILayout.Label("Cast slots: QWERASDFZXCV");
            }

            if (_runtimePhase == RuntimePhase.Shop)
            {
                GUILayout.Space(6);
                GUILayout.Label($"Shop ends in {_phaseTimer:0.0}s (or Enter)");
                DrawShopPanel();
            }

            GUILayout.Space(10);
            GUILayout.Label("Round ranking (latest)");
            foreach (var line in _roundResultLines)
            {
                GUILayout.Label($" - {line}");
            }

            GUILayout.Space(10);
            GUILayout.Label("Standings");
            foreach (var p in _core.GetStandingsSnapshot().OrderByDescending(p => p.TotalPoints).ThenBy(p => p.LastRoundRank))
            {
                GUILayout.Label($"{p.Id}: HP {Mathf.CeilToInt(GetActorHp(p.Id))}, Gold {p.Gold}, Points {p.TotalPoints}, 1st {p.FirstPlaceCount}, LastRank {p.LastRoundRank}");
                GUILayout.Label($"    Loadout: {GetLoadoutSummary(p.Id)}");
            }

            if (_runtimePhase == RuntimePhase.MatchEnd && _winner.HasValue)
            {
                GUILayout.Space(8);
                GUILayout.Label($"Winner: {_winner.Value.Winner.Id}");
                GUILayout.Label("Press [R] to restart");
            }

            GUILayout.EndArea();
        }

        private void DrawShopPanel()
        {
            if (_core == null)
            {
                return;
            }

            var local = _core.GetPlayer(_localPlayerId);
            GUILayout.Label($"Local Gold: {local.Gold}");

            foreach (var skill in SkillCatalog.Fixed12)
            {
                var owned = local.OwnedSkills.Contains(skill.Id);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{skill.Id} {skill.Name} [{skill.Price}g]{(owned ? " (Owned)" : string.Empty)}", GUILayout.Width(300));
                GUI.enabled = !owned && local.Gold >= skill.Price;
                if (GUILayout.Button("Buy", GUILayout.Width(60)))
                {
                    var purchase = _core.PurchaseSkill(_localPlayerId, skill.Id);
                    if (purchase.Ok)
                    {
                        AutoEquipToFirstFreeSlot(local, skill.Id);
                        _status = $"Bought {skill.Name}";
                    }
                    else
                    {
                        _status = $"Purchase failed: {purchase.Reason}";
                    }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        private void StartPlayableMatch()
        {
            rounds = NormalizeRounds(rounds);
            var ids = Enumerable.Range(1, playerCount).Select(i => $"P{i}").ToList();
            _localPlayerId = ids[0];
            _core = new MvpMatchCore(maxPlayers: playerCount, rounds: rounds, playerIds: ids);
            _castValidator = new CastValidator();
            _runtimePhase = RuntimePhase.Waiting;
            _phaseTimer = 0f;
            _combatTimer = 0f;
            _winner = null;
            _status = "Ready. Press Space to start";
            _roundResultLines.Clear();

            SpawnActors(ids);
            SetupStartingSkills();
            _currentBoundaryRadius = arenaRadius;
        }

        private void BeginCountdown()
        {
            _runtimePhase = RuntimePhase.Countdown;
            _phaseTimer = countdownSeconds;
            _status = "Countdown";
        }

        private void StartCombatRound()
        {
            if (_core == null)
            {
                return;
            }

            _core.StartRound();
            _runtimePhase = RuntimePhase.Combat;
            _combatTimer = 0f;
            _eliminationSequence = 0;
            _status = "Combat";
            _currentBoundaryRadius = arenaRadius;

            var activeRadius = Mathf.Max(4f, arenaRadius * 0.75f);
            var ids = _actors.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
            for (var i = 0; i < ids.Count; i++)
            {
                var actor = _actors[ids[i]];
                var angle = (Mathf.PI * 2f * i) / ids.Count;
                var pos = new Vector3(Mathf.Cos(angle), 0.5f, Mathf.Sin(angle)) * activeRadius;
                actor.Body.transform.position = pos;
                actor.MoveTarget = pos;
                actor.Hp = ActorState.MaxHp;
                actor.Boundary.Hp = ActorState.MaxHp;
                actor.Boundary.OutsideAccumulatedSec = 0f;
                actor.Boundary.InsideContinuousSec = 0f;
                actor.Boundary.OutsideTickAccumulatorSec = 0f;
                actor.SlowUntil = 0f;
                actor.SilenceUntil = 0f;
                actor.BotThinkAt = 0f;
                actor.BotCastAt = 0f;
                actor.Alive = true;
                actor.EliminationOrder = -1;
                actor.Shield = 0f;
                actor.Body.SetActive(true);
                ApplyActorMaterial(actor);
            }
        }

        private void TickLocalInput()
        {
            if (_mainCamera == null || _core == null)
            {
                return;
            }

            if (!_actors.TryGetValue(_localPlayerId, out var local) || !local.Alive)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out var hitDistance))
                {
                    var point = ray.GetPoint(hitDistance);
                    local.MoveTarget = ClampToArena(point, _currentBoundaryRadius * 1.2f);
                }
            }

            foreach (var entry in _slotKeyCodes)
            {
                if (!Input.GetKeyDown(entry.Value))
                {
                    continue;
                }

                if (_core.GetPlayer(_localPlayerId).Loadout.TryGetValue(entry.Key, out var skillId) && !string.IsNullOrWhiteSpace(skillId))
                {
                    TryCastSkill(_localPlayerId, skillId!);
                }
            }
        }

        private void TickBots()
        {
            foreach (var actor in _actors.Values)
            {
                if (!actor.IsBot || !actor.Alive)
                {
                    continue;
                }

                if (Time.time >= actor.BotThinkAt)
                {
                    var target = FindNearestEnemy(actor.Id);
                    if (target != null)
                    {
                        actor.MoveTarget = ClampToArena(target.Body.transform.position + UnityEngine.Random.insideUnitSphere * 1.5f, _currentBoundaryRadius * 1.2f);
                        actor.MoveTarget = new Vector3(actor.MoveTarget.x, 0.5f, actor.MoveTarget.z);
                    }
                    actor.BotThinkAt = Time.time + UnityEngine.Random.Range(0.5f, 1.4f);
                }

                if (Time.time >= actor.BotCastAt)
                {
                    var loadout = _core!.GetPlayer(actor.Id).Loadout;
                    var castable = loadout.Values.Where(v => !string.IsNullOrWhiteSpace(v)).Cast<string>().ToList();
                    if (castable.Count > 0)
                    {
                        var skill = castable[UnityEngine.Random.Range(0, castable.Count)];
                        TryCastSkill(actor.Id, skill);
                    }
                    actor.BotCastAt = Time.time + UnityEngine.Random.Range(1.2f, 2.4f);
                }
            }
        }

        private void TickBotsShop()
        {
            if (_core == null)
            {
                return;
            }

            foreach (var actor in _actors.Values)
            {
                if (!actor.IsBot)
                {
                    continue;
                }

                var progress = _core.GetPlayer(actor.Id);
                var purchase = SkillCatalog.Fixed12
                    .Where(s => !progress.OwnedSkills.Contains(s.Id) && s.Price <= progress.Gold)
                    .OrderBy(s => s.Price)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(purchase.Id))
                {
                    var result = _core.PurchaseSkill(actor.Id, purchase.Id);
                    if (result.Ok)
                    {
                        AutoEquipToFirstFreeSlot(progress, purchase.Id);
                    }
                }
            }
        }

        private void TickMovement()
        {
            foreach (var actor in _actors.Values)
            {
                if (!actor.Alive)
                {
                    continue;
                }

                var position = actor.Body.transform.position;
                var target = new Vector3(actor.MoveTarget.x, position.y, actor.MoveTarget.z);
                var speed = actor.MoveSpeed(Time.time);
                actor.Body.transform.position = Vector3.MoveTowards(position, target, speed * Time.deltaTime);
            }
        }

        private void TickBoundaryAndEliminations()
        {
            _combatTimer += Time.deltaTime;
            _currentBoundaryRadius = BoundaryRules.RadiusAtTime(arenaRadius, _combatTimer);

            foreach (var actor in _actors.Values)
            {
                if (!actor.Alive)
                {
                    continue;
                }

                var flatPos = new Vector2(actor.Body.transform.position.x, actor.Body.transform.position.z);
                var outside = flatPos.magnitude > _currentBoundaryRadius;
                actor.Boundary.Hp = actor.Hp;
                BoundaryRules.Step(actor.Boundary, outside, Time.deltaTime);
                actor.Hp = actor.Boundary.Hp;

                if (actor.Hp <= 0f)
                {
                    Eliminate(actor);
                }
                else
                {
                    ApplyActorMaterial(actor);
                }
            }
        }

        private void TickCombatEndCheck()
        {
            if (_core == null)
            {
                return;
            }

            var aliveCount = _actors.Values.Count(a => a.Alive);
            if (aliveCount > 1 && _combatTimer < maxCombatSeconds)
            {
                return;
            }

            var ranks = BuildRoundRanks();
            var result = _core.EndRoundWithRanks(ranks);
            _winner = result.final;
            _runtimePhase = RuntimePhase.RoundEnd;
            _phaseTimer = roundEndSeconds;
            _status = "Round ended";

            _roundResultLines.Clear();
            foreach (var rr in ranks.OrderBy(r => r.Rank))
            {
                var p = _core.GetPlayer(rr.PlayerId);
                _roundResultLines.Add($"#{rr.Rank} {rr.PlayerId} (+{EconomyRules.RankPointsFor(playerCount, rr.Rank)}pt / +{EconomyRules.GoldPayoutFor(playerCount, rr.Rank)}g => total {p.TotalPoints}pt, {p.Gold}g)");
            }
        }

        private void EnterShop()
        {
            _runtimePhase = RuntimePhase.Shop;
            _phaseTimer = shopSeconds;
            _status = "Shop";
        }

        private void EnterMatchEnd()
        {
            _runtimePhase = RuntimePhase.MatchEnd;
            _status = _winner.HasValue ? $"Winner is {_winner.Value.Winner.Id}" : "Match End";
        }

        private List<RoundRank> BuildRoundRanks()
        {
            var ordered = _actors.Values
                .OrderByDescending(a => a.Alive)
                .ThenByDescending(a => a.Hp)
                .ThenByDescending(a => a.EliminationOrder)
                .ThenBy(a => a.Id, StringComparer.Ordinal)
                .ToList();

            var ranks = new List<RoundRank>(ordered.Count);
            for (var i = 0; i < ordered.Count; i++)
            {
                ranks.Add(new RoundRank(ordered[i].Id, i + 1));
            }

            return ranks;
        }

        private void TryCastSkill(string casterId, string skillId)
        {
            if (_core == null || !_actors.TryGetValue(casterId, out var caster) || !caster.Alive)
            {
                return;
            }

            if (caster.IsSilenced(Time.time))
            {
                return;
            }

            var progress = _core.GetPlayer(casterId);
            var cast = _castValidator.ValidateAndCommit(
                _core.Phase,
                casterId,
                skillId,
                nowMs: (long)(Time.time * 1000f),
                ownedSkills: progress.OwnedSkills,
                loadout: progress.Loadout);

            if (!cast.Ok)
            {
                return;
            }

            if (!SkillCatalog.ById.TryGetValue(skillId, out var skill))
            {
                return;
            }

            _status = $"{casterId} cast {skill.Name}";
            ExecuteSkill(caster, skill);
        }

        private void ExecuteSkill(ActorState caster, SkillCard skill)
        {
            var pos = caster.Body.transform.position;
            var nearest = FindNearestEnemy(caster.Id);
            var forward = nearest != null
                ? (nearest.Body.transform.position - pos).normalized
                : caster.Body.transform.forward;

            switch (skill.Id)
            {
                case "S01":
                    ApplyKnockbackAround(pos + forward * 1.5f, 2.5f, 4f, 6f, caster.Id);
                    SpawnFxSphere(pos + forward * 1.5f, 0.8f, Color.cyan, 0.4f);
                    break;
                case "S02":
                    if (nearest != null)
                    {
                        ApplyDamage(nearest, 16f);
                        PushActor(nearest, (nearest.Body.transform.position - pos).normalized, 6f);
                        SpawnFxSphere(nearest.Body.transform.position, 0.7f, new Color(1f, 0.5f, 0f), 0.4f);
                    }
                    break;
                case "S03":
                    ApplyKnockbackAround(pos, 3.3f, 5f, 9f, caster.Id);
                    SpawnFxSphere(pos, 1.2f, Color.magenta, 0.45f);
                    break;
                case "S04":
                    ApplyKnockbackAround(pos + forward * 2f, 3.5f, 7f, 12f, caster.Id);
                    SpawnFxSphere(pos + forward * 2f, 1.5f, new Color(0.8f, 0.2f, 1f), 0.5f);
                    break;
                case "S05":
                    if (nearest != null)
                    {
                        ApplyDamage(nearest, 12f);
                        SpawnFxSphere(nearest.Body.transform.position, 0.5f, Color.red, 0.3f);
                    }
                    break;
                case "S06":
                    if (nearest != null)
                    {
                        ApplyDamage(nearest, 20f);
                        SpawnFxSphere(nearest.Body.transform.position, 0.6f, new Color(0.2f, 0.2f, 0.7f), 0.35f);
                    }
                    break;
                case "S07":
                    var blinkTarget = ClampToArena(pos + forward * 4.5f, _currentBoundaryRadius * 1.1f);
                    caster.Body.transform.position = new Vector3(blinkTarget.x, 0.5f, blinkTarget.z);
                    caster.MoveTarget = caster.Body.transform.position;
                    SpawnFxSphere(caster.Body.transform.position, 0.6f, Color.white, 0.25f);
                    break;
                case "S08":
                    var dashTarget = ClampToArena(pos + forward * 3.5f, _currentBoundaryRadius * 1.1f);
                    caster.Body.transform.position = new Vector3(dashTarget.x, 0.5f, dashTarget.z);
                    caster.MoveTarget = caster.Body.transform.position;
                    ApplyKnockbackAround(caster.Body.transform.position, 2f, 4f, 8f, caster.Id);
                    SpawnFxSphere(caster.Body.transform.position, 0.8f, Color.yellow, 0.3f);
                    break;
                case "S09":
                    ApplySlowArea(pos, 4f, 3f, caster.Id);
                    SpawnFxSphere(pos, 1.4f, new Color(0.3f, 0.8f, 1f), 0.7f);
                    break;
                case "S10":
                    ApplySilenceArea(pos, 4f, 2.5f, caster.Id);
                    SpawnFxSphere(pos, 1.4f, new Color(0.4f, 0.4f, 0.4f), 0.7f);
                    break;
                case "S11":
                    caster.Shield += 28f;
                    SpawnFxSphere(pos, 1f, Color.green, 0.5f);
                    break;
                case "S12":
                    caster.Shield += 18f;
                    ApplyKnockbackAround(pos, 3f, 3f, 4f, caster.Id);
                    SpawnFxSphere(pos, 1.2f, new Color(0.1f, 0.9f, 0.9f), 0.6f);
                    break;
                default:
                    if (nearest != null)
                    {
                        ApplyDamage(nearest, 8f);
                    }
                    break;
            }
        }

        private void ApplySlowArea(Vector3 center, float radius, float seconds, string exceptId)
        {
            foreach (var actor in _actors.Values)
            {
                if (actor.Id == exceptId || !actor.Alive)
                {
                    continue;
                }

                if (Vector3.Distance(actor.Body.transform.position, center) <= radius)
                {
                    actor.SlowUntil = Mathf.Max(actor.SlowUntil, Time.time + seconds);
                }
            }
        }

        private void ApplySilenceArea(Vector3 center, float radius, float seconds, string exceptId)
        {
            foreach (var actor in _actors.Values)
            {
                if (actor.Id == exceptId || !actor.Alive)
                {
                    continue;
                }

                if (Vector3.Distance(actor.Body.transform.position, center) <= radius)
                {
                    actor.SilenceUntil = Mathf.Max(actor.SilenceUntil, Time.time + seconds);
                }
            }
        }

        private void ApplyKnockbackAround(Vector3 center, float radius, float pushDistance, float damage, string exceptId)
        {
            foreach (var actor in _actors.Values)
            {
                if (actor.Id == exceptId || !actor.Alive)
                {
                    continue;
                }

                var direction = actor.Body.transform.position - center;
                direction.y = 0f;
                var distance = direction.magnitude;
                if (distance > radius)
                {
                    continue;
                }

                ApplyDamage(actor, damage);
                if (distance > 0.001f)
                {
                    PushActor(actor, direction.normalized, pushDistance);
                }
            }
        }

        private void PushActor(ActorState actor, Vector3 direction, float distance)
        {
            var target = actor.Body.transform.position + direction * distance;
            target = ClampToArena(target, _currentBoundaryRadius * 1.4f);
            target.y = 0.5f;
            actor.Body.transform.position = target;
            actor.MoveTarget = target;
        }

        private void ApplyDamage(ActorState actor, float damage)
        {
            if (!actor.Alive)
            {
                return;
            }

            var remainingDamage = damage;
            if (actor.Shield > 0f)
            {
                var blocked = Mathf.Min(actor.Shield, remainingDamage);
                actor.Shield -= blocked;
                remainingDamage -= blocked;
            }

            if (remainingDamage > 0f)
            {
                actor.Hp = Mathf.Max(0f, actor.Hp - remainingDamage);
                actor.Boundary.Hp = actor.Hp;
            }

            if (actor.Hp <= 0f)
            {
                Eliminate(actor);
            }
        }

        private void Eliminate(ActorState actor)
        {
            if (!actor.Alive)
            {
                return;
            }

            actor.Alive = false;
            actor.EliminationOrder = _eliminationSequence;
            _eliminationSequence += 1;
            actor.Body.SetActive(false);
        }

        private ActorState? FindNearestEnemy(string casterId)
        {
            if (!_actors.TryGetValue(casterId, out var caster))
            {
                return null;
            }

            ActorState? nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var actor in _actors.Values)
            {
                if (actor.Id == casterId || !actor.Alive)
                {
                    continue;
                }

                var d = Vector3.SqrMagnitude(actor.Body.transform.position - caster.Body.transform.position);
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    nearest = actor;
                }
            }

            return nearest;
        }

        private void SpawnActors(IReadOnlyList<string> ids)
        {
            foreach (var actor in _actors.Values)
            {
                if (actor.Body != null)
                {
                    Destroy(actor.Body);
                }
            }
            _actors.Clear();

            for (var i = 0; i < ids.Count; i++)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = ids[i];
                body.transform.localScale = new Vector3(1f, 1f, 1f);
                body.transform.position = Vector3.zero;
                body.transform.SetParent(transform);

                var actor = new ActorState(ids[i], isBot: i > 0, body);
                actor.MoveTarget = body.transform.position;
                _actors.Add(actor.Id, actor);
                ApplyActorMaterial(actor);
            }
        }

        private void SetupStartingSkills()
        {
            if (_core == null)
            {
                return;
            }

            foreach (var actor in _actors.Values)
            {
                var progress = _core.GetPlayer(actor.Id);
                progress.OwnedSkills.Clear();
                foreach (var slot in MvpConstants.LoadoutSlotKeys)
                {
                    progress.Loadout[slot] = null;
                }

                foreach (var skillId in StarterSkills)
                {
                    progress.OwnedSkills.Add(skillId);
                }

                var slots = MvpConstants.LoadoutSlotKeys;
                for (var i = 0; i < StarterSkills.Length; i++)
                {
                    _core.EquipSkill(actor.Id, slots[i], StarterSkills[i]);
                }
            }
        }

        private void AutoEquipToFirstFreeSlot(PlayerProgress progress, string skillId)
        {
            foreach (var slot in MvpConstants.LoadoutSlotKeys)
            {
                if (!progress.Loadout.TryGetValue(slot, out var existing) || string.IsNullOrWhiteSpace(existing))
                {
                    _core?.EquipSkill(progress.Id, slot, skillId);
                    return;
                }
            }
        }

        private void BuildSlotKeys()
        {
            _slotKeyCodes.Clear();
            _slotKeyCodes["Q"] = KeyCode.Q;
            _slotKeyCodes["W"] = KeyCode.W;
            _slotKeyCodes["E"] = KeyCode.E;
            _slotKeyCodes["R"] = KeyCode.R;
            _slotKeyCodes["A"] = KeyCode.A;
            _slotKeyCodes["S"] = KeyCode.S;
            _slotKeyCodes["D"] = KeyCode.D;
            _slotKeyCodes["F"] = KeyCode.F;
            _slotKeyCodes["Z"] = KeyCode.Z;
            _slotKeyCodes["X"] = KeyCode.X;
            _slotKeyCodes["C"] = KeyCode.C;
            _slotKeyCodes["V"] = KeyCode.V;
        }

        private string GetLoadoutSummary(string playerId)
        {
            if (_core == null)
            {
                return string.Empty;
            }

            var loadout = _core.GetPlayer(playerId).Loadout;
            var filled = loadout
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => $"{kv.Key}:{kv.Value}")
                .ToArray();
            return filled.Length == 0 ? "(empty)" : string.Join(", ", filled);
        }

        private float GetActorHp(string playerId)
        {
            return _actors.TryGetValue(playerId, out var actor) ? actor.Hp : 0f;
        }

        private void BootstrapSceneIfNeeded()
        {
            if (FindObjectsByType<WarlockPlayableBootstrap>(FindObjectsSortMode.None).Length > 1)
            {
                _status = "Duplicate bootstrap detected";
            }

            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                _mainCamera = cameraGo.AddComponent<Camera>();
                cameraGo.tag = "MainCamera";
                cameraGo.transform.position = new Vector3(0f, 24f, -17f);
                cameraGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            }

            if (FindObjectsByType<Light>(FindObjectsSortMode.None).Length == 0)
            {
                var lightObj = new GameObject("Directional Light");
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            var ground = GameObject.Find("WarlockGround");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "WarlockGround";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = Vector3.one * 6f;
            }

            _arenaVisual = GameObject.Find("ArenaVisual");
            if (_arenaVisual == null)
            {
                _arenaVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _arenaVisual.name = "ArenaVisual";
                _arenaVisual.transform.position = new Vector3(0f, 0.05f, 0f);
                _arenaVisual.transform.localScale = new Vector3(arenaRadius * 2f, 0.05f, arenaRadius * 2f);
                var renderer = _arenaVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }
            }
        }

        private void UpdateArenaVisual()
        {
            if (_arenaVisual != null)
            {
                _arenaVisual.transform.localScale = new Vector3(_currentBoundaryRadius * 2f, 0.05f, _currentBoundaryRadius * 2f);
            }
        }

        private Vector3 ClampToArena(Vector3 point, float radius)
        {
            var flat = new Vector2(point.x, point.z);
            if (flat.magnitude <= radius)
            {
                return point;
            }

            var n = flat.normalized * radius;
            return new Vector3(n.x, point.y, n.y);
        }

        private void ApplyActorMaterial(ActorState actor)
        {
            var renderer = actor.Body.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            if (!actor.Alive)
            {
                renderer.material.color = Color.black;
                return;
            }

            var baseColor = actor.Id == _localPlayerId ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.3f, 0.3f);
            if (actor.Shield > 0f)
            {
                baseColor = Color.Lerp(baseColor, Color.cyan, 0.5f);
            }
            if (Time.time < actor.SlowUntil)
            {
                baseColor = Color.Lerp(baseColor, Color.blue, 0.5f);
            }
            if (Time.time < actor.SilenceUntil)
            {
                baseColor = Color.Lerp(baseColor, Color.gray, 0.6f);
            }

            renderer.material.color = baseColor;
        }

        private void SpawnFxSphere(Vector3 position, float scale, Color color, float life)
        {
            var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "FxSphere";
            fx.transform.position = new Vector3(position.x, 0.5f, position.z);
            fx.transform.localScale = Vector3.one * scale;

            var col = fx.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            var renderer = fx.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            Destroy(fx, life);
        }

        private void ResetAndRestart()
        {
            foreach (var actor in _actors.Values)
            {
                if (actor.Body != null)
                {
                    Destroy(actor.Body);
                }
            }
            _actors.Clear();
            StartPlayableMatch();
        }

        private static int NormalizeRounds(int value)
        {
            if (MvpConstants.RoomRoundOptions.Contains(value))
            {
                return value;
            }

            return MvpConstants.DefaultRounds;
        }
    }
}
