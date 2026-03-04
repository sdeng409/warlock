# 21. MVP Implementation Backlog

작성일: 2026-03-04

## Epic A. Match Loop
- [x] A1. Round state machine 구현
- [x] A2. Round transition UI (MVP 런타임 프리젠터 계약으로 구현)
- [x] A3. Match end + winner 판정

## Epic B. Skill Loadout
- [x] B1. 12슬롯 입력 바인딩 (키-슬롯 매핑/리바인딩 검증 포함)
- [x] B2. 스킬 장착/해제 UI (MVP 로직 모델 완성)
- [x] B3. 쿨다운 처리

## Epic C. Economy/Shop
- [x] C1. 순위 점수 계산
- [x] C2. 골드 지급
- [x] C3. 상점 구매(고정 카탈로그)

## Epic D. Arena Rules
- [x] D1. 경계 DoT 처리
- [x] D2. 누적 타이머/리셋 규칙
- [x] D3. 축소 로직 + 최소 반경

## Epic E. Networking (Host+Relay)
- [x] E1. 룸 생성/초대 코드 (HostRelaySession 모델)
- [x] E2. 핵심 이벤트 동기화 (순서/시퀀스 계약)
- [x] E3. 재접속/탈주 최소 처리 (호스트 종료 safe-terminate 정책)

---

## 비고 (완료 해석 기준)
- 본 백로그 완료 표기는 **MVP reference/handoff 범위** 기준입니다.
- Unity 실제 씬 UI 연결/실시간 Fusion 운영 환경 검증은 별도 통합 단계에서 수행합니다.
