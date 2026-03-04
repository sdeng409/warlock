# warlock pre-production workspace

작성일: 2026-03-04

이 폴더는 코드가 아닌 **사전기획/설계 문서 전용**입니다.
현재(macOS)에서 문서 정리 → 이후 Windows에서 구현 AI 세션이 즉시 개발 가능한 상태를 목표로 구성했습니다.

## 문서 우선순위 (Windows AI 세션 진입 순서)
1. `docs/00-one-pager.md`
2. `docs/12-vertical-slice-scope.md`
3. `docs/05-skill-system.md`
4. `docs/17-round-economy-and-skill-shop.md`
5. `docs/07-networking-overview.md`
6. `docs/19-arena-shrink-rules.md`
7. `docs/01-decision-log.md`

## 핵심 기획 문서
- `docs/02-core-loop.md`
- `docs/04-combat-rules.md`
- `docs/06-arena-boundary-dot.md`
- `docs/16-skill-pool-v1.md`
- `docs/18-legacy-warlock-reference-analysis.md`

## 제작/운영 문서
- `docs/08-tech-stack-decision.md`
- `docs/22-skill-cards-v1.md`
- `docs/23-network-event-contract.md`
- `docs/24-acceptance-test-spec.md`
- `docs/25-engine-lock-and-package-versions.md`
- `docs/13-milestones.md`
- `docs/14-risk-register.md`
- `docs/15-playtest-template.md`
- `docs/20-windows-ai-handoff-playbook.md`
- `docs/21-mvp-implementation-backlog.md`

## UX/콘텐츠 가이드
- `docs/03-controls-camera.md`
- `docs/09-ux-hud.md`
- `docs/10-art-style-guide.md`
- `docs/11-audio-direction.md`

## 상태 관리
- `docs/open-questions.md`: 현재 MVP 핵심 결정 완료 상태
- `.omx/plans/open-questions.md`: OMX 체크리스트 동기화

## Windows AI 세션 시작 체크리스트
- [ ] Unity 프로젝트 생성 + 네트워크 스택 선택 반영
- [ ] `20-windows-ai-handoff-playbook.md` 순서로 구현 시작
- [ ] MVP 범위 외 기능(팀전/랜덤매칭/Dedicated) 구현 금지
