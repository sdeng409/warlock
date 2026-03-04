# 16. Skill Pool v1 (Draft)

작성일: 2026-03-04

## 확정 사항
- 초기 스킬 풀 크기: **12개 확정**
- 자원 시스템: **Cooldown only**

## 목적
- 클래스리스 구조에서 사용할 초기 스킬 풀(초안) 정의
- 캐릭터가 아닌 스킬 조합 중심 메타를 만들기 위한 기준 문서

## 키 슬롯
- `Q W E R A S D F Z X C V` (최대 12)

## 스킬 설계 템플릿
- SkillId:
- Name:
- Tags:
- CastType:
- CooldownSec:
- Damage/Effect:
- CounterPoint:

## v1 구성 제안(수량)
- Knockback/Displacement: 4
- Damage: 2
- Mobility: 2
- Control: 2
- Defense: 2
- 합계: 12

## 밸런스 체크리스트
- 폭딜/생존/기동/제어가 한 조합에 과도하게 몰리지 않는가?
- 소프트 락온 환경에서 특정 스킬이 과도하게 강해지지 않는가?
- 바운더리 DoT 메타와 충돌하는 스킬(무적/무한 이동)이 없는가?


## 가격 티어(초안)
- Tier 1: 2 Gold
- Tier 2: 4 Gold
- Tier 3: 6 Gold

## 구매 규칙(초안)
- 스킬 구매 시 소유 목록에 추가
- 소유 스킬은 지정 슬롯(QWER/ASDF/ZXCV)에 자유 배치
- 매치 중 구매 불가, 라운드 사이 상점 페이즈에서만 구매

- 각 플레이어가 최소 1개 이상의 넉백 스킬을 선택하도록 유도(가격/티어 설계)
