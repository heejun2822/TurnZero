# 로컬 인게임 실행 가이드

| 항목 | 내용 |
|---|---|
| Git 최초 확인 | 2026-10-22, eba3e9b |
| 직전 마지막 커밋 | 2026-10-23, ed5cc8a. 총 5개 커밋, 이후 기획 문답에서 작업 트리 수정 |
| 현재 점검 / 갱신 | 2026-10-29, 로컬 봇 모드·자동 배치·진단/재현 추가 |
| 역할 / 관리 조건 | 현재 로컬 씬의 실행·조작·데이터 편집 안내. 실행/입력 변경 시 갱신 |
| 게임 기준 / 현재 차이 | [DevelopmentPlan.md](DevelopmentPlan.md) / [Development.md](Development.md#2-명세와-현재-구현의-차이) |
| 구조 / 검증 | [GameArchitecture.md](GameArchitecture.md) / [Verification.md](Verification.md) |
| 이전 판본 | [재편 전 가이드](Archive/2026-10-28/LocalPlaytest.md) |

한 기기에서 양 팀을 번갈아 조작하거나 사람 P1 대 봇 P2로 플레이하는 로컬 개발 도구다. 아래 조작은 현재 구현 안내이며 최종 입력 기획의 확정과 구분한다. 확정된 로컬 전투 차이 PT-01~14를 반영하고 Unity 자동 검사를 실행했다. 전체 수용 플레이와 실기기 검증은 계속 필요하다. 실행 증거는 Verification.md 5장에서 확인한다.

봇은 1차 범위의 이동·기본 공격·스킬·대기·부활을 판단하며 제작·장비 행동은 사용하지 않는다. 사람은 기존 제작·장비를 사용할 수 있다. 규칙·점수는 [개발 명세](DevelopmentPlan.md#414-로컬-봇), 구현/검증 상태는 [개발 현황](Development.md#51-로컬-봇-1차-개발-계획)을 따른다.

## 1. 실행

1. Unity 6000.3.5f2에서 TurnZero 프로젝트를 연다.
2. 메뉴 **TurnZero > Local Playtest > Open game** 또는 [LocalBattle.unity](../TurnZero/Assets/ProjectFiles/Scenes/LocalBattle.unity)를 연다. 현재 빌드 설정의 첫 씬이다.
3. Play를 누르고 설정 화면 오른쪽 아래 **LOCAL TWO PLAYER**를 눌러 **HUMAN VS BOT**으로 바꾸거나 로컬 2인 모드를 유지한다. 봇 대전은 P1이 사람, P2가 봇이며 P2 필드·덱은 기본값으로 고정한다. 로컬 2인은 양측 필드·장비 덱을 선택한다. 장비 UI는 5칸에서 빈칸과 정의된 3종을 순환하며 중복을 건너뛴다. 양측 각각 1종 이상 선택해야 배치를 시작할 수 있다.
4. **DEPLOY TEAMS**로 P1 기지 → P1 캐릭터 5명을 배치한다. 봇 대전에서는 이후 P2가 자동 배치되고, 로컬 2인에서는 P2 기지 → P2 캐릭터 5명을 직접 배치한다. 캐릭터는 자기 필드의 기지 시야 안에 둔다. **QUICK DEPLOY**는 로컬 빠른 배치 도구다.
5. 턴 안내 후 준비 중 행동을 예약한다. 봇은 준비 진입에 한 번 판단·완료하며 사람이 READY하면 바로 판정/재생한다. 로컬 2인에서는 **TAB / VIEW P2**로 상대도 조작하고 양측 완료 또는 시간 만료에 판정/재생한다.
6. 종료 후 **SET UP A REMATCH**로 다시 시작한다. 선택한 모드·경기 시드는 유지하고 봇 계획·진단 상태는 초기화한다. 설정 화면에서 모드를 다시 선택할 수 있다.

실행 도구 근거: [LocalPlaytestBuilder.cs](../TurnZero/Assets/ProjectFiles/Editor/Playtest/LocalPlaytestBuilder.cs), 입력 근거: [BattleInput.cs](../TurnZero/Assets/ProjectFiles/Scripts/Input/BattleInput.cs)와 [BattleSession.cs](../TurnZero/Assets/ProjectFiles/Scripts/Application/BattleSession.cs).

## 2. 현재 조작

| 목적 | PC | 터치 / 화면 버튼 |
|---|---|---|
| 아군 선택 | 모델·카드 클릭, 1~5 | 모델·카드 탭 |
| 중첩 타일 선택 | 행동 미선택 상태에서 바닥 반복 클릭, 또는 개별 카드 | 바닥 반복 탭·카드 |
| 캐릭터 중심 | 카드 더블클릭, 같은 숫자 키 빠르게 2회 | 카드 더블탭 |
| 이동 | Q 후 타일 클릭, 우클릭, 선택 캐릭터 드래그 경로 | 이동 버튼 후 타일, 선택 캐릭터 드래그 |
| 공격 / 스킬 / 장비 | W / E / R 후 대상 타일 | 해당 행동 버튼 후 대상 |
| 대기 / 예약 취소 | T 또는 Esc | WAIT / CANCEL |
| 부활 | 사망 아군 카드 → REVIVE → 부활 타일 | 동일 |
| 카메라 이동 / 줌 | 빈 전장 드래그 / 휠 | 빈 전장 드래그 / 핀치 |
| 준비 완료 / 해제 | Enter | READY / UNREADY |
| 시점 전환 | Tab, 로컬 2인에서만 | VIEW P1 / P2. 봇 대전은 VS BOT 표시·전환 비활성 |
| 타이머 정지 | P | PAUSE / RESUME |
| 강제 판정 / 재생 넘기기 | Space | RESOLVE / FAST |
| 조작 안내 | F1 | GUIDE |

- 중첩 모델은 같은 타일 안에서 분산 표시한다. 논리 위치는 같은 타일이며 모델 직접 클릭과 바닥 순환 선택을 구분한다.
- 행동 지정 중에는 아군/적 모델과 바닥 클릭을 모두 대상 타일로 처리한다. 캐릭터 선택은 행동 미선택 상태의 모델/바닥 또는 카드를 이용한다.
- 경로 드래그는 이동력까지 미리보기를 제한하고 되돌아가면 경로를 줄일 수 있다. 한도 밖 드래그 종료와 유효하지 않은 최종 경로 제출의 처리는 명세 기준 재검증 대상이다.
- UI 위 드래그/휠은 전장 카메라 입력과 구분하고 시뮬레이션 중 수동 카메라·명령 편집을 잠근다.
- 준비 완료 후 상대가 아직 완료하지 않았다면 READY 버튼으로 해제할 수 있다. 예약·AP·남은 시간은 유지한다. 양측 완료나 마감 후에는 편집할 수 없다.
- 같은 페이즈 이동은 한 칸씩 함께 재생하고 공격/교환은 자기 팀 번호순 → 상대 팀 번호순으로 재생한다. 재생 후 수동 카메라 위치·줌을 복원한다.
- 처치 AP는 턴 마무리의 실제 증가량으로 강조한다. 아이템은 최초/동시 접촉 확정 시 바로 제거하며 회색의 미관측 기억은 재관측 때 갱신한다.
- **VISION / DEBUG**, 빠른 배치·강제 판정·타이머 정지는 테스트 도구다. 정상 시야와 온라인 정보 공개를 검증할 때 디버그 모드를 끈다.
- 타이머 정지는 준비 시간만 멈춘다. 준비 완료와 강제 판정은 계속 사용할 수 있다.
- 최종 단축키·제스처는 Q-18에서 결정한다. 현재 1~5·Q/W/E/R/T 구현만으로 원문 입력 공백을 확정하지 않는다.

## 3. 데이터와 씬 편집

경로는 Unity 프로젝트 TurnZero 기준이다.

| 위치 | 편집 대상 |
|---|---|
| [LocalBattleConfiguration.asset](../TurnZero/Assets/ProjectFiles/Data/LocalBattleConfiguration.asset) | 콘텐츠 참조·HP/시야·AP·부활·재료·시간·재생 속도·카메라·시드·프로필 메모 |
| [Data/Characters](../TurnZero/Assets/ProjectFiles/Data/Characters) | HP·이동/공격/스킬 수치·효과·모델·초상화 |
| [Data/Equipment](../TurnZero/Assets/ProjectFiles/Data/Equipment) | 장비 행동·설치 HP·포털 기간/거리·아이콘 |
| [Data/Fields](../TurnZero/Assets/ProjectFiles/Data/Fields) | 같은 크기를 사용하는 필드 프리셋 |
| [LocalBattleSession.prefab](../TurnZero/Assets/ProjectFiles/Prefabs/LocalBattleSession.prefab) | 세션 연결·월드/HUD 배치 |

수치 변경 후 새 Play 또는 REMATCH로 상태를 다시 초기화한다. SO 데이터는 경기별 일반 C# 상태로 복사한다. 최종 기획 수치를 바꾸는 작업이라면 DevelopmentPlan.md의 해당 콘텐츠/VAL 표도 함께 갱신한다.

경기 설정의 Initial Zoom은 시작 거리 비율, Zoom Sensitivity는 휠/핀치 감도다. 현재 기본값은 0.45와 1.2이며 Minimum/Maximum Zoom으로 제한한다. 이 값은 구현 설정이며 확정된 전체 게임 밸런스/입력 목표가 아니다.

### 3.1. 필드 저작

현재 필드는 Local Battle > World > Field / 12 x 20 > Field Grid 아래 **Allied Field / Neutral Field / Enemy Field** Tilemap으로 구성한다. 바닥 크기 2×2, 간격 2.2, XZY 좌표이며 아군 Y=0~7·중립 Y=8~11·적군 Y=12~19다.

LocalFieldMap Inspector의 **Paint … Field**로 Tile Palette/GameObject Brush를 선택한다. 바닥을 바꿀 때 기존 칸을 지우고 새 프리팹을 칠하며 Z=0·셀당 바닥 하나를 유지한다. 씬/프리팹을 저장한다. 게임 내 커스텀 필드 편집 UI나 저장 슬롯의 구현과 구분한다.

### 3.2. 재생성 도구

**TurnZero > Local Playtest > Build scene and missing defaults**는 없는 기본 에셋을 만들고 씬·세션 프리팹을 다시 저작한다. 기존 SO 수치는 유지하지만 직접 편집한 씬/프리팹 배치는 재생성 전에 보관한다. 일반 실행에는 Open game을 사용한다.

Local Profile Notes는 v2로 갱신했다. 게임 규칙의 기준은 DevelopmentPlan.md이며 메모와 구현 차이는 Development.md에서 확인한다.


### 3.3. View 컴포넌트 편집

LocalBattleSession.prefab을 열고 수정할 화면 오브젝트를 선택한다. 예를 들어 Match header는 LocalMatchHeaderView, Timer는 LocalTurnTimerView, Selection header는 LocalSelectionView, Equipment crafting은 LocalEquipmentView가 담당한다. 각 컴포넌트의 Inspector에는 해당 표시 요소에 필요한 텍스트·이미지·버튼 참조만 둔다.

- 기존 화면을 수정할 때 해당 오브젝트의 컴포넌트와 참조를 편집한다. 카드·AP·모델·타일 컴포넌트도 같은 방식이다.
- 오브젝트를 교체할 때 새 오브젝트에 해당 View를 추가하고 UI 참조를 연결한 뒤, Game HUD의 LocalBattleHud 또는 World의 LocalBattleWorld에 있는 해당 컴포넌트 슬롯을 바꾼다. 게임을 다시 시작하면 BattleBootstrap이 Bind한다.
- 도움말·결과·교체 확인처럼 처음 꺼져 있는 패널도 루트의 참조를 유지한다. 타이머를 추가할 때 전체 상태를 매 프레임 새로 생성하지 말고 SecondsLeft를 사용한다.
- World 아래 Field / 12 x 20·Characters and bases·Planned path와 Items, structures and effects의 하위 그룹에서 범위·모델·경로·필드 표식·일시 효과를 각각 편집한다.

전체 컴포넌트와 책임은 [GameArchitecture.md](GameArchitecture.md#21-오브젝트별-view-구성)에 정리했다. 씬 재생성 도구도 같은 연결을 생성하지만, 일반 표시 수정에 전체 씬 재생성을 사용할 필요는 없다.

### 3.4. 봇 판단 진단

- 준비 단계에 진입한 봇 경기에서 메뉴 **TurnZero > Local Playtest > Bot > Export last decision**을 실행한다. 마지막 판단의 정상 관측·규칙 사본·봇/규칙 식별자·시드·행동/대상/경로·점수 항목·예약 결과를 `Tools/Checks/Results/last-bot-plan.json`에 저장한다. 파일은 저장소 루트 기준이며 실행 로그와 같이 Git 제외 대상이다.
- **Replay exported decision**은 저장한 관측·시드로 판단기를 다시 실행해 규칙 식별자·명령·점수의 일치를 검사한다. 현재 경기 상태를 바꾸지 않는다.
- 정상 HUD에는 봇 예약·점수를 표시하지 않는다. VISION/DEBUG는 사람의 디버그 표시만 바꾸고 봇은 항상 P2의 정상 관측으로 판단한다.
- `BattleConfiguration.DefaultMode`로 새 실행의 기본 모드를 정하고 `RandomSeed`로 재현 시드를 정한다. 기본 모드는 로컬 2인이다. 장비 포함 승률·최적 판단을 평가하는 도구로 1차 봇을 사용하지 않는다.

## 4. 테스트와 판정 기준

Unity Test Runner에서 현재 인게임 EditMode·PlayMode 테스트를 실행한다. 에디터 자동화는 테스트 어셈블리의 LocalTestRun.Begin(mode, outputPath)로 같은 검사를 실행하고 XML을 저장할 수 있다. 씬 전환 전 사용자 편집 내용을 저장하거나 버릴지 결정한다. Unity가 패키지를 가져온 뒤 .NET SDK 8에서도 코어 검사를 실행할 수 있다.

```powershell
dotnet run --project Tools/Checks/TurnZero.Checks.csproj
```

검증 ID·기대 결과·과거 실행 범위와 새 결과는 [Verification.md](Verification.md)에 관리한다. Tools/Checks/Results는 실행 로그·XML·렌더 증거 저장 위치이며 과거 기록의 경로가 현재 체크아웃에 존재하는지는 실행 시 함께 확인한다.

이전 씬 Assets/ProjectFiles/Prototype/Scenes/Battle.unity는 회귀용이다. [보관 프로토타입 가이드](Archive/2026-10-28/Prototype.md)의 16×24 맵·4인 편성·옛 조작을 현재 LocalBattle 가이드에 섞지 않는다.
