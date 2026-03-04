# HANDOFF (세션 인수인계)

작성일: 2026-03-05 (KST)
브랜치: `main`

## 1) 현재 상태 요약
- MVP 로컬 플레이어블 + 문서/테스트 베이스라인까지 완료.
- 프로덕션 전환 로드맵 문서 추가 완료:
  - `docs/26-production-roadmap.md`
- `.omx/`는 `.gitignore`에 포함되어 있어 원격으로 동기화되지 않음.

## 2) 다음 세션 시작 순서 (필수)
1. `AGENTS.md`
2. `README.md`
3. `docs/26-production-roadmap.md`
4. `docs/HANDOFF.md` (이 문서)

## 3) 즉시 착수할 작업 (우선순위)
1. **Phase 0 고정**
   - Production 1.0 범위(FFA+커스텀룸) 확정
   - 성능/안정성 목표 수치 확정
   - actorId 인증 바인딩 정책 확정
2. **Phase 1 리팩터링**
   - `Assets/Warlock/Scripts/Playable/WarlockPlayableBootstrap.cs` 책임 분리
3. **검증 강화**
   - Unity PlayMode 스모크 테스트 추가

## 4) 실행/검증 명령어
```bash
# Node 테스트
npm test --silent

# Git 동기화
git pull origin main
```

## 5) 인계 시 주의사항
- Unity에서 생성되는 `Library/`, `Logs/`, `UserSettings/` 등은 커밋 대상이 아님.
- 인수인계 메모는 `.omx/`가 아니라 `docs/*.md` 같은 추적 파일에 남길 것.

