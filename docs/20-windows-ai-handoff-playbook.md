# 20. Windows AI Handoff Playbook

작성일: 2026-03-04

## 목적
Windows 환경의 다른 AI 세션이 즉시 구현을 시작할 수 있도록, 실행 순서와 참조 문서를 고정한다.

## 구현 시작 전 읽을 문서 순서
1. `00-one-pager.md`
2. `12-vertical-slice-scope.md`
3. `05-skill-system.md`
4. `17-round-economy-and-skill-shop.md`
5. `07-networking-overview.md`
6. `25-engine-lock-and-package-versions.md`
7. `19-arena-shrink-rules.md`
8. `01-decision-log.md`

## 구현 순서(권장)
1. 로컬 단일 플레이 라운드 루프
2. 상점/골드/점수 시스템 연결
3. 12키 스킬 슬롯/쿨다운 시스템
4. 넉백 + 장외 DoT + 축소
5. Host+Relay 멀티 연결
6. 룸 설정(3/5/7) + 동점 규칙

## AI 세션용 고정 프롬프트(초안)
- "MVP 범위를 벗어나지 말 것"
- "팀전/랜덤매칭/Dedicated는 구현 금지"
- "클래스리스 12키 매핑, 라운드 경제, 누적 점수 규칙을 우선 구현"

## Windows 환경 세팅 고정값
- Unity Editor: **6000.0.68f1**
- Network SDK: **Photon Fusion 2.0.11 Stable**
- MVP 기간 중 Unity 6.3.x / Fusion 2.1 업그레이드 금지
