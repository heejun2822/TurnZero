# 보관 문서와 문서 수명

보관일: 2026-10-28. 현재 게임 내용은 [DevelopmentPlan.md](../DevelopmentPlan.md), 작업/질문은 [Development.md](../Development.md)에서 관리한다.

Git의 최초 확인 날짜는 저장소에서 추적되는 최초 시점이다. 실제 파일을 처음 작성한 시각과 동일하다고 단정하지 않는다. 마지막 커밋 날짜와 작업 트리의 후속 갱신을 구분한다.

## 1. 문서별 판단

| 문서 | Git 최초 확인 | 재편 전 마지막 커밋 | 판단 / 앞으로 관리 |
|---|---|---|---|
| DevelopmentPlan.md | 2026-10-22 / eba3e9b | 2026-10-23 / ed5cc8a, 이후 10/28까지 기획 문답 수정 | 유지. 현재 규칙/수치의 단일 기준으로 통합. 상태·이력·검증은 분리 |
| Development.md | 2026-10-08 / cb036f7 | 2026-10-23 / ed5cc8a | 옛 내용은 프로토타입 기록으로 보관. 같은 경로의 새 문서는 현재 개발 현황/미결정 담당 |
| GameArchitecture.md | 2026-10-22 / eba3e9b | 2026-10-23 / ed5cc8a, 총 3개 커밋 | 유지. 현재 Scripts/Domain·Application 등의 구조와 연결되어 있음. 구조 변경 때 갱신 |
| LocalPlaytest.md | 2026-10-22 / eba3e9b | 2026-10-23 / ed5cc8a, 총 5개 커밋, 이후 문답 수정 | 유지. 현재 LocalBattle 씬 실행 가이드. PT 차이 추적은 Development로 이관 |
| Prototype.md | 2026-10-07 / 2005bf5 | 2026-10-23 / ed5cc8a, 총 8개 커밋 | 보관. 16×24/4인 편성·옛 씬/코어 안내로 현재 12×20/5인 작업과 충돌. 현재 루트 안내에서 제외 |
| Decisions.md / Verification.md | 2026-10-28 이번 재편 | 새 문서 | 유지. 기존 결정/변경과 TC/실행 증거를 ID 보존해 이관 |

10월 23일 마지막 커밋에는 디렉터리 재배치가 포함된다. 날짜만으로 폐기를 판단하지 않고 대상 씬·코드·규칙과 현재 사용처를 함께 확인했다. 구조/로컬 가이드는 이번에 소스와 대조해 갱신했으며 앞으로의 갱신 조건을 각 문서 머리에 적었다.

## 2. 2026-10-28 재편 전 판본

이 판본들은 당시 내용과 문답 맥락을 보존한다. 현재 기획 기준으로 수정하지 않는다. 문장·표·ID는 보존하고 상단 보관 표시를 추가하고 이동으로 끊기는 Markdown 상대 링크와 줄바꿈 형식을 조정했다. 같은 보관 묶음의 문서는 서로 보관 판본을 가리킨다. 코드 경로를 적은 일반 텍스트는 작성 당시 표현이다.

| 보관 파일 | 용도 |
|---|---|
| [DevelopmentPlan.md](2026-10-28/DevelopmentPlan.md) | 1,594행의 재편 전 전체 명세. 완료 문답 묶음·추천안·날짜별 초기 개발 기록 포함 |
| [Development.md](2026-10-28/Development.md) | 이전 프로토타입의 구현·임시 규칙·2026-10-08~15 검증 |
| [GameArchitecture.md](2026-10-28/GameArchitecture.md) | 재편 전 구조 설명 |
| [LocalPlaytest.md](2026-10-28/LocalPlaytest.md) | PT-01~14와 기획 후속 설명이 누적된 기존 가이드 |
| [Prototype.md](2026-10-28/Prototype.md) | 이전 Battle.unity 실행·맵/프리팹 편집·회귀 검증 방법 |

Prototype 코드·씬·테스트는 Assets/ProjectFiles/Prototype에 유지한다. 보관은 회귀 도구 삭제를 뜻하지 않는다.

## 3. 보존 확인 기준

원문 Requirements.md(300행)의 SHA-256은 A301E94689AC624F06928D16A130B97087D8661184EEF4D92FE128763AA45B1C다. 이번 재편에서 사용자 원문과 루트의 미추적 기획 파일은 변경하지 않는다.

재편 전 판본은 아래 원문 체크섬을 기준으로 보관 표시·상대 링크 외의 본문을 보존했는지 검증한다.

| 원래 경로 | 재편 직전 SHA-256 |
|---|---|
| Project/DevelopmentPlan.md | 5EA1AAF01584438B8F3ADFBF33F006BA076B6FC1893AC447BDC386B29C55F68B |
| Project/Development.md | A24206408C325224DAD5FD81F0E868A3EFAB5472DEB300166C45158E8F4331AB |
| Project/GameArchitecture.md | F667181C88E106EB2AD5C83E6F614971C9824EF7A91C9A378CB58A7951E86D85 |
| Project/LocalPlaytest.md | EEB04E89BD9DFDFE833E598F0CC3CB2845B1BB5CC2486534A21B777C15C89898 |
| Project/Prototype.md | ED251B8D7A304208F4EBF0F98DBE2CB6F6FFEBD56887FF9BAAB10B52F83EC443 |
