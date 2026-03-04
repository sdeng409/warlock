# 25. Engine Lock & Package Versions

작성일: 2026-03-04
기준 시점: **2026-03-04 KST**

## 목적
Windows AI 세션에서 재현 가능한 개발 환경을 고정한다.

## Engine Lock (MVP 고정)
- Engine: **Unity**
- Unity Version: **6000.0.68f1 (Unity 6.0 LTS 라인)**
- Render Pipeline: **URP**

### 선택 이유
- 8인 탑뷰 PvP MVP 기준에서 URP가 성능/개발 속도 균형이 가장 좋다.
- Photon Fusion 2.0 공식 요구사항이 Unity **6.0.x**를 명시하고 있어, 통합 리스크를 최소화한다.

## Network Stack Lock (MVP 고정)
- Primary: **Photon Fusion 2.0**
- Version: **2.0.11 Stable (Build 1743, 2026-02-09)**
- Voice (선택): **Photon Voice 2.62** (Fusion 2.0 매칭 다운로드)

## 필수 패키지 버전(고정 규칙)
| Package | Version | Lock Rule |
|---|---|---|
| com.unity.inputsystem | **1.17.0** | Unity 6000.0 라인 고정 |
| com.unity.ugui | **Editor와 동일 코어 버전** | Editor 버전(6000.0.68f1) 고정으로 간접 고정 |
| UI Toolkit | **Unity 6 내장** | 별도 패키지 추가 설치 없이 사용 |

## 업그레이드 정책
- MVP 기간 중 Unity를 **6000.0.x 내부 패치** 외 업그레이드 금지.
- MVP 기간 중 Fusion **2.0.x 내부 패치** 외 업그레이드 금지.
- Unity 6.3 라인(예: 6000.3.x) 이동은 MVP 완료 후 별도 브랜치에서 검증 후 진행.

## 구현 체크리스트 (Windows 세션 시작 시)
1. Unity Hub에서 **6000.0.68f1** 설치
2. 프로젝트 최초 생성 후 `Packages/manifest.json` 커밋
3. `Packages/packages-lock.json` 커밋
4. Photon Fusion **2.0.11 Stable** import 후 즉시 커밋

## 근거 링크 (2026-03-04 확인)
- Unity 6000.0.68f1 릴리스: https://unity.com/releases/editor/whats-new/6000.0.68f1
- Unity 6 릴리스/지원(6.3 LTS 및 6.0 LTS 지원 기간): https://unity.com/releases/unity-6
- Photon Fusion 2.0 릴리스 노트(2.0.11 Stable): https://doc.photonengine.com/fusion/current/getting-started/release-notes/release-notes-2-0
- Photon Fusion SDK 다운로드/요구사항(Unity 6.0.x): https://doc.photonengine.com/fusion/2-shared/getting-started/sdk-download
- Unity Input System (6000.0, 1.17.0): https://docs.unity3d.com/kr/6000.0/Manual/com.unity.inputsystem.html
- Unity UI(ugui) 코어 패키지 정책: https://docs.unity3d.com/kr/6000.0/Manual/com.unity.ugui.html
- UI Toolkit Unity 6 내장 안내: https://docs.unity3d.com/jp/current/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/install-ui-toolkit-and-sample-projects.html
