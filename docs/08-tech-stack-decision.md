# 08. Tech Stack Decision

작성일: 2026-03-04

## 후보
1. Unity + Photon Fusion
2. Unity + NGO + UGS Relay/Lobby
3. Godot + ENet 기반
4. Unreal + Replication

## 최종 권장(초안)
### MVP
- **Unity + Photon Fusion (Shared/Host 중심)**
- 이유: 8인 PvP MVP를 가장 빠르게 완주할 가능성이 높음

### Fallback
- **Unity + NGO + UGS Relay/Lobby**
- 이유: 공식 스택 선호 시 유지보수/통합성이 좋음

## 왜 Unreal/Godot를 지금 바로 1순위로 안 두는가
- Unreal: 네트워킹 구조 학습 비용이 높아 초보 MVP 완주 리스크 큼
- Godot: 오픈소스 장점은 크지만 실시간 PvP 운영/네트워크 설계 부담이 큼

## 재평가 조건
- 비용/라이선스 이슈 발생
- 목표 플랫폼 확장(콘솔 등)
- Dedicated 서버가 반드시 필요한 경쟁 모드로 전환

## 근거 링크 (2026-03-04 확인)
- Unity Netcode for GameObjects: https://docs.unity3d.com/kr/6000.0/Manual/com.unity.netcode.gameobjects.html
- Unity Relay: https://docs.unity.com/relay
- Unity Lobby: https://docs.unity.com/lobby
- Unity Multiplayer Services SDK: https://docs.unity.com/ugs/en-us/packages/com.unity.services.multiplayer/1.1
- Unity Matchmaker/Multiplay Hosting support note: https://docs.unity.com/ugs/en-us/manual/matchmaker/manual/support
- Photon Fusion Shared Intro: https://doc.photonengine.com/fusion/2-shared/fusion-shared-intro
- Photon Fusion Pricing: https://www.photonengine.com/ko-KR/fusion/pricing
- Mirror Getting Started: https://mirror-networking.gitbook.io/docs/manual/general/getting-started
- Mirror GitHub (MIT): https://github.com/MirrorNetworking/Mirror
- Godot MultiplayerAPI: https://docs.godotengine.org/en/4.0/classes/class_multiplayerapi.html
- Godot MultiplayerPeer: https://docs.godotengine.org/en/4.0/classes/class_multiplayerpeer.html
- Unreal Networking Overview: https://dev.epicgames.com/documentation/en-us/unreal-engine/networking-overview-for-unreal-engine


## 네트워크 운영 방침(확정)
- MVP: **Host + Relay**
- Dedicated Server: 초기 범위에서 제외, production 목표 시점에 비용/운영 관점에서 재평가

## MVP 버전 락(2026-03-04 기준)
- Unity: **6000.0.68f1 (6.0 LTS 라인)**
- Photon Fusion: **2.0.11 Stable**
- 상세 패키지 버전/업그레이드 정책: `25-engine-lock-and-package-versions.md`
