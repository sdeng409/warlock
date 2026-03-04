# 22. Skill Cards v1 (Implementable Draft)

작성일: 2026-03-04

## 목적
Windows AI 세션이 바로 구현 가능한 수준으로 **12개 스킬 카드**를 고정한다.

## 공통 필드
- SkillId / Name / Tag / CastType / Cooldown / Effect / PushForce / Price(Gold)

## 스킬 카드 (v1 초안)
| ID | Name | Tag | Cast | CD | 핵심 효과 | PushForce | Price |
|---|---|---|---|---:|---|---:|---:|
| S01 | Arc Push | Knockback | Direction | 5s | 직선 충격파 + 넉백 | 1.0 | 2 |
| S02 | Blast Palm | Knockback | Target | 7s | 단일 대상 밀치기 | 1.3 | 3 |
| S03 | Shock Ring | Knockback | Area | 10s | 원형 파동, 주변 밀치기 | 1.1 | 4 |
| S04 | Overdrive Wave | Knockback | Direction | 14s | 강한 장거리 넉백 | 1.6 | 6 |
| S05 | Ember Bolt | Damage | Direction | 4s | 직선 피해 | 0.2 | 2 |
| S06 | Void Spear | Damage | Target | 8s | 고피해 단타 | 0.1 | 4 |
| S07 | Blink Step | Mobility | Self | 10s | 짧은 거리 이동 | 0.0 | 3 |
| S08 | Dash Burst | Mobility | Direction | 12s | 돌진 + 경미한 밀치기 | 0.4 | 4 |
| S09 | Slow Field | Control | Area | 12s | 범위 감속 | 0.0 | 3 |
| S10 | Silence Pulse | Control | Area | 15s | 짧은 침묵/시전 방해 | 0.0 | 5 |
| S11 | Guard Shell | Defense | Self | 13s | 넉백 저항/피해 감소 | 0.0 | 4 |
| S12 | Barrier Wall | Defense | Area | 16s | 짧은 벽 생성(진로 차단) | 0.0 | 5 |

## 구현 주의
- MVP는 클래스리스 자유 배치지만, 지나친 넉백 연계는 쿨다운으로 제어
- PushForce는 내부 단위(상대값)로 시작하고 플레이테스트로 재보정
