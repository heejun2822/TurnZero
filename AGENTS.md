# 프로젝트 지침

## 커밋

- 커밋하기 전에 루트의 `CommitConvention.md`를 읽고 해당 규칙을 따른다.
- 실제 커밋에 포함되는 변경을 기준으로 타입과 범위를 선택한다.
- 타입과 범위는 영문 소문자, 변경 내용은 한국어로 작성한다.
- 기본 형식은 `타입(범위): 변경 내용`이며, 범위 생략 조건은 `CommitConvention.md`를 따른다.

## 코드 작업

- 현재 게임의 기본 네임스페이스는 TurnZero, 에디터는 TurnZero.Editor, 테스트는 TurnZero.Tests를 사용한다.
- SOLID 원칙을 따르며 각 클래스의 책임을 한 문장으로 설명할 수 있게 구분한다.
- 입력, 게임 규칙·상태, 표현, 저장·데이터처럼 책임·수명주기·변경 이유가 다른 영역은 명확한 클래스로 분리한다.
- SOLID를 이유로 구현 하나뿐인 인터페이스, 불필요한 팩토리나 계층을 만들지 않는다.
- 스크립트 파일명과 주요 타입명을 일치시키고 한 파일에는 하나의 주요 타입을 둔다. 공통 상수·enum 모음인 GameDefinitions.cs는 여러 enum을 함께 두는 예외로 한다.
- 공유 상수·enum은 Domain/Definitions/GameDefinitions.cs에서 관리하고, 함수·테스트 전용 상수와 에셋에서 편집하는 밸런스 설정은 해당 책임에 둔다.
- 전투 판정과 화면 재생, 공유 설정 데이터와 경기별 변경 상태를 분리한다. 화면/프레임 순서가 경기 결과를 바꾸지 않게 한다.
- 이벤트 구독·비동기 작업·경기 상태의 수명을 명시하고 재시작/씬 종료 시 정리한다.

- 구조적 코드 수정(모듈, 클래스, 책임 또는 호출 관계 변경) 시 프로젝트 로컬 `karpathy-guidelines`와 `ponytail` 스킬을 함께 적용한다.

## Unity 프로젝트 탐색

- 코드·에셋 조사는 `TurnZero/Assets/ProjectFiles`부터 시작한다. `TurnZero/Assets/Resources`, `TurnZero/Packages`, `TurnZero/ProjectSettings`는 작업에 필요할 때만 확인하고, 외부 에셋인 `TurnZero/Assets/External`은 해당 에셋 작업에만 확인한다.
- Unity 생성 폴더인 `TurnZero/Library`, `Temp`, `obj`, `Logs`, `UserSettings`, `Build`, `Builds`, `MemoryCaptures`, `Recordings`, `ExportedObj`, `ServerData`, `UIElementsSchema`와 IDE 캐시 `.vs`, `.idea`, `.gradle`은 검색·열람하지 않는다. 저장소 전체를 검색할 때도 이 경로를 제외한다. 사용자가 해당 폴더의 문제를 직접 요청한 경우에만 확인한다.

## Graphify

- 코드베이스를 조사하거나 수정하기 전 저장소 루트에 `graphify-out/graph.json`이 없으면 `graphify .`으로 생성한다. `graphify query "<변경 대상에 관한 질문>"`로 관련 구조와 호출 관계를 조회한 뒤 실제 코드를 확인한다.
- 코드 수정과 검증 후 저장소 루트에서 `graphify update .`를 실행해 변경 사항을 그래프에 반영한다.
