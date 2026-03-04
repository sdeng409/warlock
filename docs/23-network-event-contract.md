# 23. Network Event Contract (MVP)

작성일: 2026-03-04

## 1) 권위 원칙
- Host authoritative (Host + Relay)
- 클라이언트는 입력/요청 전송, Host가 판정/확정 브로드캐스트

## 2) 핵심 이벤트
1. Match/Room
- `RoomCreated`, `PlayerJoined`, `RoomSettingsUpdated`

2. Round
- `RoundStarted`, `RoundEnded`, `MatchEnded`

3. Combat
- `SkillCastRequested` (client->host)
- `SkillCastConfirmed` (host->all)
- `DamageApplied`
- `KnockbackApplied`
- `PlayerEliminated`

4. Economy/Shop
- `RankPointsGranted`
- `GoldGranted`
- `ShopPurchaseRequested` (client->host)
- `ShopPurchaseConfirmed` / `ShopPurchaseRejected`
- `LoadoutUpdated`

5. Arena
- `ShrinkStateUpdated`
- `BoundaryDotTick`

## 3) 검증 규칙
- 골드 음수 금지
- 소유하지 않은 스킬 장착 금지
- 쿨다운 중 시전 거부
- 라운드 상태 외(Shop/End) 전투 시전 거부
