# TurnZero

Unity 6000.3.5f2 기반의 동시 턴 전술 게임. 현재는 한 기기에서 양측을 바꿔 조작하며 배치부터 전투·승리·재시작까지 검증하는 로컬 인게임 단계다. 개발 기준은 [DevelopmentPlan.md](Project/DevelopmentPlan.md)다.

## 플레이

1. Unity Hub에서 `TurnZero` 폴더를 연다.
2. `TurnZero > Local Playtest > Open game` 또는 `Assets/ProjectFiles/Scenes/LocalBattle.unity`를 열고 Play를 누른다.
3. 양측 필드·장비 덱을 고른 뒤 **DEPLOY TEAMS**로 기지와 캐릭터 5명씩을 배치한다. 빠른 검증은 **QUICK DEPLOY**를 쓴다.
4. 캐릭터를 선택하고 행동을 예약한다. **TAB / VIEW P2**로 상대 팀을 조작한다. 양측 준비 완료 또는 제한시간 만료에 판정·재생하고 다음 턴으로 넘어간다.

PC는 1~5 선택, Q/W/E/R/T 행동, Enter 준비 완료, Tab 시점 전환, 우클릭 이동, 선택 캐릭터 드래그 경로 지정, 빈 전장 드래그 이동, 휠 줌을 지원한다. 모바일은 탭·드래그·핀치·카드 더블탭을 사용한다. F1 또는 GUIDE에서 조작 안내를 연다. P는 로컬 테스트용 타이머 정지, Space는 강제 판정·재생 빨리 넘기기다.

자세한 조작·검증 방법과 임시 해석은 [로컬 플레이 가이드](Project/LocalPlaytest.md)를 참고한다.

## 현재 인게임

- 12×20 필드, 양측 5종 캐릭터, 기지 HP 30, 직접 배치와 캐릭터 중첩.
- 첫 준비 AP 10, 턴당 +10, 상한 25. 예약·수정·취소와 실행 시 소비.
- 경로 이동, 범위 공격, 은신 이동·불 지대·고정·정밀 타격, 부활과 쿨다운.
- 초기 제작 재료 2개, 장비 제작·교체 확인, 포털·위치 교환·시야 구조물, 필드 재료 생성·공동 획득.
- 관측과 마지막 관측을 구분하는 안개·카드·마커, 양측 시점 변환, 자기 공격부터 순차 재생·공통 동시 피해, 자동 카메라.
- 양측 카드·상세 행동·인벤토리·25칸 AP·준비 완료·결과·도움말, 가로/세로 배치.

미정 규칙은 명시된 로컬 프로필로 시험한다. 원문이 비어 있는 강화·소비 콘텐츠와 온라인·계정 성장·필드 편집 UI는 후속 범위다.

## 데이터와 구조

Unity `Assets` 루트에는 게임 제작 파일 `ProjectFiles`, 가져온 에셋 `External`, 런타임 로드용 `Resources`를 둔다.

`Assets/ProjectFiles/Data/LocalBattleConfiguration.asset`에서 경기 설정과 콘텐츠를 연결한다. `Characters`, `Equipment`, `Fields`의 ScriptableObject에서 수치·설명·효과·모델·초상화를 편집하거나 교체한다. 경기 시작마다 별도 상태로 복사하므로 플레이한 HP와 AP가 원본 에셋에 저장되지 않는다.

`Assets/ProjectFiles/Scripts`는 Domain, Data, Application, Input, Presentation으로 나뉜다. 씬 구성은 `BattleBootstrap`, 수명은 `BattleSession`, 연결은 씬 소유 `BattleEvents`가 맡는다. 전투 코어는 Unity와 UI를 참조하지 않는다. 구체적인 책임·호출 관계는 [구조 문서](Project/GameArchitecture.md)를 참고한다.

`TurnZero > Local Playtest > Build scene and missing defaults`는 없는 기본 데이터만 생성하고 씬·세션 프리팹을 다시 만든다. 기존 SO 수치는 유지한다. 직접 편집한 씬 배치는 재생성 전에 보관한다.

## 검증

Unity Test Runner에서 EditMode와 PlayMode를 실행한다. Unity가 패키지를 가져온 뒤 .NET SDK 8에서도 코어를 확인할 수 있다.

```powershell
dotnet run --project Tools/Checks/TurnZero.Checks.csproj
```

실행 결과와 요구사항별 남은 범위는 [개발 기록](Project/DevelopmentPlan.md#93-로컬-인게임-플레이테스트-단계)에 관리한다. 작업용 테스트 로그·XML·렌더 이미지는 `Tools/Checks/Results`에 저장하며 Git에서는 제외한다.

이전 씬 `Assets/ProjectFiles/Prototype/Scenes/Battle.unity`는 회귀 검증용으로 유지한다. 이전 맵·프리팹·타일 팔레트의 편집 방법은 [프로토타입 가이드](Project/Prototype.md)에 있다.
