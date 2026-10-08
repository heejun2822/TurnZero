# 프로젝트 지침

## 커밋

- 커밋하기 전에 루트의 `CommitConvention.md`를 읽고 해당 규칙을 따른다.
- 실제 커밋에 포함되는 변경을 기준으로 타입과 범위를 선택한다.
- 타입과 범위는 영문 소문자, 변경 내용은 한국어로 작성한다.
- 기본 형식은 `타입(범위): 변경 내용`이며, 범위 생략 조건은 `CommitConvention.md`를 따른다.

## 코드 작업

- 구조적 코드 수정(모듈, 클래스, 책임 또는 호출 관계 변경) 시 프로젝트 로컬 `karpathy-guidelines`와 `ponytail` 스킬을 함께 적용한다.
- 코드 수정 후 저장소 루트에서 `graphify update .`를 실행해 그래프를 갱신한다. `graphify-out/graph.json`이 없다면 먼저 `graphify .`으로 초기 그래프를 생성한다.
