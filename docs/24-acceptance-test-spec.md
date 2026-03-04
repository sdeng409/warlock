# 24. Acceptance Test Spec (MVP)

작성일: 2026-03-04

## A. 매치 루프
- [x] 5라운드 기본 설정에서 `RoundStart->Combat->RoundEnd->Shop`가 끊김 없이 반복
- [x] 최종 우승이 누적 점수 기준으로 정확히 계산
- [x] 동점 시 1위 횟수 -> 마지막 라운드 순위 규칙 적용

## B. 스킬/로드아웃
- [x] 12키 슬롯 자유 배치 동작
- [x] 미소유 스킬 장착 불가
- [x] 쿨다운 중 재시전 거부

## C. 경제/상점
- [x] 라운드 종료 후 순위 골드 지급 정확
- [x] 상점 구매 시 골드 차감/소유 반영 정확
- [x] 리롤/재판매 버튼/경로 없음

## D. 경계/축소
- [x] 장외 DoT 1초 5% 적용
- [x] 복귀 즉시 DoT 중단 + 누적 타이머 3초 규칙 적용
- [x] 30초 후 축소 시작, 1.5%/s, 35% 최소 반경 유지

## E. 네트워크
- [x] 2~8인에서 이벤트 동기화 일관성 유지 (MVP reference session model 기준)
- [x] Host 재시작/연결 끊김 시 안전 종료 또는 복구 동작 명확

---

## 검증 근거
- Node 테스트: `npm test --silent` (18/18 통과)
  - A/B/C/D/E + 런타임 흐름(A2/B1/B2/E1~E3) 시나리오 포함
- Unity EditMode 테스트:
  - `A_MatchLoopTests.cs`
  - `B_LoadoutCastTests.cs`
  - `C_EconomyShopTests.cs`
  - `D_BoundaryShrinkTests.cs`
  - `E_ContractValidationLockTests.cs`
  - `F_RuntimeFlowAndNetworkSessionTests.cs`

## 범위 주의
- 본 문서의 완료 표기는 **MVP reference/handoff 환경** 기준입니다.
- 실시간 Fusion 릴레이 실운영/씬 UI 와이어링은 통합 단계에서 추가 검증합니다.
