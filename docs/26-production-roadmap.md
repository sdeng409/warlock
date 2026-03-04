# Warlock Production Roadmap (from current codebase)

작성일: 2026-03-04 (KST)  
기준 브랜치: `main`  
목표: 현재 MVP 플레이어블을 **실제 서비스 가능한 프로덕션 레벨 게임(1.0)** 으로 확장

---

## 1) Requirements Summary

### 현재 베이스라인(확정 사실)
- 현재 구현은 **MVP 범위(FFA, Host+Relay, 2~8인, 라운드+상점 루프)** 를 충족하도록 구성됨 (`README.md:19-41`, `docs/00-one-pager.md:14-37`, `docs/07-networking-overview.md:6-23`).
- Unity 플레이어블은 동작하지만, 런타임이 단일 부트스트랩에 밀집됨 (`Assets/Warlock/Scripts/Playable/WarlockPlayableBootstrap.cs:91-151`, `153-214`, `557-637`, `877-925`).
- 도메인/엔진 분리의 출발점은 이미 존재:
  - 도메인 asmdef no-engine (`Assets/Warlock/Scripts/Mvp/Warlock.Mvp.asmdef`)
  - 플레이어블 asmdef가 도메인 참조 (`Assets/Warlock/Scripts/Playable/Warlock.Playable.asmdef`).
- 네트워크/세션은 현재 **모델 레벨 계약**까지 구현 (`Assets/Warlock/Scripts/Mvp/MvpRuntime.cs:172-395`, `docs/23-network-event-contract.md:5-38`).
- README도 현재 플레이어블이 production UI가 아니고 OnGUI/primitive 기반임을 명시 (`README.md:105-107`).

### Production 1.0 목표(이번 계획의 가정)
- 범위: **개인전(FFA) + 커스텀룸(2~8인) + Host authoritative 멀티플레이 안정화**
- 제외: 팀전/랜덤매칭/Dedicated(후속)
- 기준 문서: `docs/00`, `07`, `09`, `13`, `14`, `24`

---

## 2) Acceptance Criteria (testable)

1. **멀티 E2E 완주율**
   - 2/4/8인 시나리오에서 20회 연속 매치 완주율 95% 이상
   - 포함: 룸 생성/입장/라운드 반복/매치 종료/재시작
2. **권한 일관성**
   - 전투/경제/상점 확정 이벤트는 Host만 승인 가능
   - 비정상 actorId/권한 없는 요청은 100% 거부 (`MvpRuntime.cs:239-248`, `279-287`, `321-324`)
3. **재접속/종료 정책 검증**
   - 플레이어 재접속 시 시퀀스 복구 동작 확인 (`MvpRuntime.cs:349-386`)
   - Host 이탈 시 safe terminate/종료 이벤트 누락 0 (`MvpRuntime.cs:331-338`)
4. **UI/HUD 프로덕션 전환**
   - OnGUI 제거 (`WarlockPlayableBootstrap.cs:153-214` 대체)
   - HUD 필수 요소(체력/라운드/반경/골드/점수/12슬롯 쿨다운) 100% 노출 (`docs/09-ux-hud.md:5-12`)
5. **성능/안정성**
   - 타깃 PC에서 8인 전투 p95 프레임타임 16.7ms 이하
   - 세션 크래시율 0.5% 미만
6. **릴리즈 운영 준비**
   - CI에서 Node + Unity 테스트 자동화
   - 크래시/로그 수집 + 롤백 절차 문서화

---

## 3) Implementation Steps (priority order)

## Phase 0 — Scope/Contract Freeze (1주, 가장 먼저)

### 작업
- Production 1.0 범위 명시 문서 추가 (`docs/`): FFA 고정, QoS 수치, 출시 제외 항목
- 이벤트 계약을 "MVP 문자열 이벤트"에서 "버저닝 가능한 명세"로 고정 (`docs/23-network-event-contract.md`, `Assets/Warlock/Scripts/Mvp/NetworkContract.cs`)
- 인증/actorId 바인딩 정책 명문화 (`README.md:109`, `MvpRuntime.cs:239-248`)

### 완료 기준
- 팀이 참조하는 단일 scope 문서 1개 + ADR 1개 생성
- 네트워크/권한/성능 기준이 수치로 명시

## Phase 1 — Playable Monolith 분해 (2~3주)

### 작업
- `WarlockPlayableBootstrap` 책임 분리:
  - `Input` / `CombatPresenter` / `HudPresenter` / `SceneBootstrap`
- 스킬 실행 `switch` 분리 + 데이터/액션 매핑화 (`WarlockPlayableBootstrap.cs:565-637`)
- OnGUI 제거 준비: HUD 상태 DTO/API 정리 (`WarlockPlayableBootstrap.cs:160-210` 대체)

### 완료 기준
- 단일 1,000+ 라인 부트스트랩 제거
- 동일 기능 PlayMode 스모크 테스트 통과

## Phase 2 — 결정론 세션 커널 정립 (2~4주)

### 작업
- `Time.time`/`deltaTime` 의존 전투 판정을 서버 tick 기반으로 전환 (`WarlockPlayableBootstrap.cs:107`, `436`, `539`)
- 도메인 규칙(경계/캐스팅/경제)을 command->state transition 흐름으로 통일
- 재현 가능한 이벤트 로그 포맷 추가

### 완료 기준
- 같은 입력 시드에서 결과 재현율 100%
- 경계/랭크/보상 계산 불일치 0

## Phase 3 — 실제 온라인 런타임 통합(Fusion) (3~5주)

### 작업
- Fusion 패키지 실 설치/고정 (현재 manifest에는 직접 항목 부재: `Packages/manifest.json:2-9`, lock intent는 `Packages/warlock-mvp-lock.json:9-13`)
- `HostRelaySession` 모델을 실제 네트워크 어댑터로 치환 (`MvpRuntime.cs:172-395`)
- 룸/초대/이벤트 시퀀스/재접속 E2E 구현

### 완료 기준
- 2~8인 실 네트워크 룸에서 라운드 루프 완주
- RTT/손실 조건에서 판정/종료 동작 안정

## Phase 4 — Production UI/UX + 전투 피드백 (3~4주)

### 작업
- `docs/09` 필수 HUD를 UGUI/UIToolkit로 구현
- 경계 위험 경고/넉백 방향 피드백/라운드 결과 UI 구현 (`docs/09-ux-hud.md:13-21`)
- 입력 리바인딩 UX(현재 룰: `MvpRuntime.cs:56-123`)를 실제 메뉴 UI와 연결

### 완료 기준
- OnGUI 완전 제거
- 조작/피드백 항목 usability 테스트 통과

## Phase 5 — 운영/보안/데이터 계층 (3~4주)

### 작업
- 계정 인증과 `actorId` 바인딩(클라 임의 입력 신뢰 금지)
- 결과/경제 원장 저장 + idempotency
- 로깅/관측성/장애 대응 기본 체계 구축

### 완료 기준
- 악성 요청 재현 테스트에서 위변조 성공률 0
- 매치 결과 유실 0

## Phase 6 — QA/성능/릴리즈 게이트 (2~3주)

### 작업
- soak test(장시간), 지연/패킷손실/재접속 시나리오 자동화
- 회귀 테스트 파이프라인: Node(`npm test`) + Unity(EditMode/PlayMode)
- Release Candidate 체크리스트 운영

### 완료 기준
- P0/P1 버그 0
- 성능/완주율/안정성 지표 모두 목표 충족

---

## 4) Risks & Mitigations

1. **Fusion 실환경 동기화 리스크**
   - 대응: 1~2주 네트워크 스파이크(지연/손실 조건)로 조기 검증
2. **Host+Relay 프로덕션 한계(치팅/안정성/비용)**
   - 대응: Dedicated 전환 트리거를 KPI 기반으로 미리 정의
3. **모놀리스 유지로 인한 기능 추가 정체**
   - 대응: Phase 1을 네트워크 통합보다 먼저 수행
4. **경제/밸런스 스노우볼**
   - 대응: 라운드 보상/구매/승률 텔레메트리 수집 후 수치 조정
5. **재접속 시 이벤트 순서 무결성 깨짐**
   - 대응: seq 기반 재전송/중복 제거 테스트를 필수화

---

## 5) Verification Steps

1. **로직 회귀**: `npm test --silent` 18/18 유지 (`README.md:48`)
2. **Unity EditMode 회귀**: `Assets/Warlock/Tests/EditMode/*` 전체 통과
3. **Unity PlayMode 스모크**: 룸 생성->전투->라운드 종료->상점->재시작 자동 시나리오
4. **네트워크 통합 테스트**: 2/4/8인 + RTT 120ms/손실 1% 조건 반복
5. **릴리즈 전 게이트**: 성능/버그/완주율/크래시율 체크리스트 승인

---

## 6) Start Here First (이번 주 착수 순서)

1. **Phase 0 고정부터 시작**
   - "Production 1.0 scope + QoS + 보안 바인딩" 문서를 먼저 확정
2. **바로 다음으로 Phase 1 착수**
   - `WarlockPlayableBootstrap` 분해 (입력/전투/HUD/씬)
3. **동시에 최소 PlayMode 자동검증 추가**
   - 리팩터링 중 기능 퇴행 방지
4. **그 다음 Fusion 통합 스파이크(Phase 3 일부 선행 검증)**
   - 실제 네트워크 위험을 초기에 계측

> 즉, **정답은: 아트보다 먼저 “스코프/권한/런타임 분해/검증 기반”을 잡는 것**입니다.

---

## 7) Open Questions (결정 필요)

1. Production 1.0을 FFA+커스텀룸으로 고정할지?
2. Host+Relay를 1.0까지 유지할지, 특정 KPI에서 Dedicated 전환할지?
3. 성능/안정성 목표 수치(FPS/RTT/완주율/크래시율) 최종값?
4. 인증/actorId 바인딩 책임을 어디까지 백엔드가 보장할지?
5. 출시 최소 운영요소(CI, 관측성, 롤백) 범위를 어디까지로 할지?

