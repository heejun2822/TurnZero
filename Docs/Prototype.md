# 이전 전투 프로토타입

Unity 6000.3.5f2 기반의 3D 동시 턴 전술 게임 프로토타입.

## 실행

1. Unity Hub에서 `TurnZero` 폴더를 연다.
2. `Assets/Scenes/Battle.unity`를 열거나 메뉴 `TurnZero > Open Battle Prototype`을 선택한다.
3. Play를 누르고 P1, P2 순서로 강조된 칸에 기지를 배치한다.
4. 3D 필드에서 내 캐릭터 → MOVE / ATTACK → 대상 타일 또는 적 모델 순서로 명령을 지정한다.
5. VIEW / SWITCH 버튼으로 상대 시점으로 전환해 다른 플레이어의 명령을 입력한다.
6. 30초가 지나면 동시 판정한다. 0.75초 동안 `RESOLVING`으로 결과와 해당 턴의 공개 지점을 표시한 뒤 다음 턴의 30초 준비 시간을 시작한다. 빠른 검증에는 `ADVANCE TURN`을 사용한다.

오른쪽 패널의 되돌리기 버튼은 선택한 캐릭터의 명령과 AP 예약을 취소한다. READY는 해당 플레이어의 수정만 막는다. 종료 후 RESTART로 새 경기를 시작한다.

필드는 XZ 평면의 입체 타일이며 원근 카메라로 내려다본다. ROTATE로 시점을 회전하고, 필드 위에서 마우스 휠 또는 ZOOM + / - 버튼으로 확대·축소한다. 커서를 화면 가장자리 24px 안으로 보내면 해당 화면 방향으로 이동한다. 확대했을 때 이동할 수 있으며, 전체 필드가 보이는 축은 중앙에 고정한다. UI 위의 휠, 마우스 버튼을 누른 상태, 터치 중, 창 밖이나 포커스를 잃은 상태에서는 마우스 카메라 조작을 받지 않는다. 마우스 클릭과 터치로 월드 오브젝트를 선택한다. 캐릭터·기지는 조명과 그림자가 적용되는 임시 3D 모델이다.

상단은 기지 HP·턴·시간, 오른쪽은 선택 캐릭터·명령·장비 표시, 하단은 캐릭터 카드·명령·사용 가능한 AP와 예약량을 표시한다. UI는 영어이며 SKILL·ITEM은 비활성이다. 임시 초상화·아이콘 생성 기록은 [Tools/Art/README.md](../Tools/Art/README.md)를 참고한다.

현재 개발 기준과 진행 상태는 [DevelopmentPlan.md](../DevelopmentPlan.md)를 따른다. [Development.md](../Development.md)는 기존 프로토타입의 임시 규칙 기록이며, 원본 기획은 [Plan.md](../Plan.md)다. 현재 구현은 로컬 기본 전투이며 온라인·스킬·아이템·부활·필드 편집·재화는 이후 단계다.

새 명세의 첫 구현 범위는 AP와 확정된 승패 판정이다. AP는 배치 중 0, 첫 준비 단계 10으로 시작하고 이후 턴마다 10을 지급해 최대 25까지 이월한다. 예약·수정·취소는 보유 AP를 차감하지 않으며 비용 합계가 보유량을 넘는 수정은 기존 명령을 유지한다. 실제 비용은 이동·공격 실행 단계에서 차감한다. 양측 기지가 생존한 상호 전멸은 기지 HP를 비교하며, 한쪽 기지 파괴와 상대 캐릭터 전멸이 겹치면 기지가 파괴된 쪽이 패배한다. 나머지 미정 복합 승패는 기존 무승부 처리를 임시로 유지한다. 공격 순차 연출·새 캐릭터/필드 수치·중첩·범위·쿨다운 등 남은 범위는 DevelopmentPlan.md에서 별도로 추적한다.

## 씬과 프리팹 편집

`Battle.unity`에는 `Battle Session` 프리팹이 미리 배치되어 있다. Play 전에도 고정 맵, 양측 캐릭터와 기지, HUD를 확인하고 편집할 수 있다. Play와 RESTART는 경기 데이터와 표시 상태만 초기화하며 오브젝트를 생성하거나 교체하지 않는다. 캐릭터의 편집기 위치는 미리보기이며 실제 시작 위치는 기지 선택 후 정해진다.

- `Assets/Prefabs/Map`: GameObject Brush로 배치한 16×24 고정 맵. 바닥 크기는 2×2, 격자 간격은 2.2이며 필드 전체는 35.2×52.8 월드 단위다. 타일 높이는 0.16이며 캐릭터·조형물 모델 크기는 유지한다. `Field Grid` 아래 Allied Field 16×10, Neutral Field 16×4, Enemy Field 16×10의 세 Tilemap을 연결한다. 주변 바닥·외곽 조형물·Foundation은 없다. `BattleMapView`는 전체 필드 좌표를 사용하며 `BattleTileView`가 시야·강조 표시를 담당한다.
- `Assets/Prefabs/Characters`: Guardian, Scout, Ranger, Support, Base. `BattleActorView`가 위치·표시·라벨을 갱신한다. 모델을 교체할 때 루트의 스크립트와 연결된 라벨·선택 대상을 유지한다.
- `Assets/Prefabs/UI`: 상단 바, 명령 패널, 역할별 카드, AP 패널, 툴바와 HUD. 각 View의 Inspector에서 UI와 초상화 참조를 편집한다.
- `BattleCameraRig`: 원근 카메라와 `BattleCameraRig` 스크립트. Inspector의 Minimum / Maximum Zoom은 전체 필드를 맞춘 거리의 배율(기본 0.15–1), Wheel Sensitivity는 휠 감도, Edge Size는 가장자리 폭(px), Pan Speed는 이동 속도(타일/초, 줌 배율 적용), Field Padding은 허용할 외곽 여백(기본 0.65타일 = 1.43 월드 단위)이다. 화면에 보이는 바닥 범위를 기준으로 이동을 제한하며 회전·화면 크기 변경에도 다시 적용한다. 원거리 표시 한계도 필드 크기와 카메라 거리에 맞춰 갱신한다. `BattleHudLayout`은 화면 방향·안전 영역에 따라 HUD 배치를 조정한다.
- `LastSeenMarker`: 씬에 준비된 마커로 마지막 관측 위치·당시 HP·관측 턴을 표시한다. 시야 밖 적의 현재 위치나 HP로 갱신하지 않는다.
- `BattleSession`: 위 프리팹들의 연결과 `BattleController`. Controller의 Rules에서 HP·AP·공격·턴 시간을 변경한다. 맵 크기는 배치된 `BattleMapView`를 기준으로 한다.

전투 상태·명령 검증·동시 판정은 `Scripts/Core`에 유지한다. 런타임 스크립트는 프리팹이나 UI를 만들지 않는다. 임시 모델과 UI를 만드는 코드는 `Assets/Editor/BattlePrefabBuilder.*.cs`에 있으며 플레이어 빌드에 포함되지 않는다.

HUD·버튼·카드 텍스트는 `TextMeshProUGUI`, 캐릭터 HP·마지막 관측의 월드 텍스트는 `TextMeshPro`를 사용한다. View 스크립트는 `TMP_Text`를 참조한다. Unity 6의 uGUI 패키지에 포함된 TMP와 가져온 `TMP Essential Resources`를 사용하며, 기본 폰트는 `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`이다. 현재 UI는 영어이며 한글 폰트는 별도로 추가해야 한다.

`TurnZero > Rebuild Prototype Scene and Prefabs`는 임시 에셋과 씬을 명시적으로 다시 만드는 개발 도구다. 직접 수정한 프리팹과 씬 배치를 덮어쓰므로 일반 실행이나 UI 편집에는 사용하지 않는다.

## 타일맵으로 필드 편집

1. `Assets/Prefabs/Map/FixedBattleMap.prefab`을 Prefab Mode로 연다.
2. 맵 Inspector에서 `Paint Allied Field`, `Paint Neutral Field`, `Paint Enemy Field` 중 편집할 구역을 누른다. `TurnZero 3D` 팔레트와 패키지의 `GameObject Brush`가 선택된다. Tilemap 선택 후 `TurnZero > Open 3D Tile Palette` 메뉴도 사용할 수 있다.
3. StoneLight·StoneDark·Grass 또는 바닥과 조형물을 묶은 TreeTile·RuinedWallTile을 고른다. 브러시 Anchor는 `(0.5, 0.5, 0)`, Z는 0으로 유지한다.
4. GameObject Brush는 이미 찬 칸을 덮어쓰지 않는다. 바닥을 바꾸려면 먼저 Erase로 기존 타일을 지운 뒤 Paint한다. 칠하기·지우기는 Unity의 Undo·Redo를 지원한다.
5. 전체 구역은 같은 전역 좌표를 사용한다. X는 0–15이며 Allied Y 0–9, Neutral Y 10–13, Enemy Y 14–23에 칠한다. 각 구역의 Tilemap을 Active Target으로 지정한다.
6. Prefab을 저장한다. 전체 384칸에 바닥 프리팹이 정확히 하나씩 있어야 Play할 수 있다. 기지는 중립 구역에 배치할 수 없으며, 양쪽 자기 필드의 가장자리 한 칸도 제외한다.

`2D Tilemap Extras 6.0.3`을 사용한다. 팔레트는 바닥 3종과 조형물 포함 바닥 2종으로 구성한다. 중립 필드에는 나무 4개·벽 4개를 대칭으로 배치하고 가운데 두 열을 비워 둔다. GameObject Brush의 결과물은 각 Tilemap의 자식 프리팹으로 저장된다. 별도의 Tile 에셋 데이터나 메시 동기화 과정이 없으며, Tilemap Renderer는 평면 스프라이트를 그리지 않도록 비활성이다. 나무·벽·지형 종류는 현재 외형만 구분하며 이동이나 시야를 차단하지 않는다.

편집한 3D 타일은 프리팹에 저장된다. 플레이·재시작 때 새 타일을 생성하지 않는다. 게임 안 필드 편집 UI는 아직 제공하지 않는다.

## 전장의 안개

플레이어별로 현재 시야와 탐색 기록을 관리한다. 자기 필드 160칸은 처음부터 탐색한 상태이며 중립·상대 필드는 미탐색 상태다. 현재 시야가 우선하고, 한 번 시야를 확보한 칸은 이후에도 탐색 기록이 남는다. 재시작하면 자기 필드만 탐색한 초기 상태로 돌아간다.

- 미탐색: 지형 종류와 관계없이 동일한 검정 계열 바닥을 표시해 격자와 필드 경계를 알 수 있게 한다. 조형물·선택 테두리와 그 그림자는 숨긴다. 논리 타일과 입력용 콜라이더는 유지한다.
- 탐색했으나 현재 안 보임: 지형과 조형물을 원래 색의 30% 밝기로 표시한다. 시야 밖 적의 실제 모델은 숨기고 마지막 관측 위치 마커만 표시한다. 마커는 관측 당시의 위치·HP·턴을 유지한다.
- 현재 보임: 살아 있는 아군 기지·캐릭터의 상하좌우 거리 2칸과 해당 턴의 행동 공개 지점을 정상 표시한다. 타일의 실제 월드 크기는 시야의 칸 수에 영향을 주지 않는다.

유효한 기본 공격이 실행되면 대상 칸을 공격한 플레이어에게 해당 턴 동안 공개한다. 한 칸만 공개하며 주변에 추가 시야를 만들지 않는다. 판정 중 공격자가 죽어도 공개 지점은 유지되고, 다음 턴에는 임시 시야가 만료되어 탐색 기록만 남는다. 명령 예약·취소·실패한 공격은 임시 시야를 만들지 않는다. 스킬 실행 기능은 아직 없으며 향후 스킬 판정에서도 `BattleState.RevealCell`을 사용할 수 있다. 나무·벽의 시선 차단은 아직 적용하지 않는다.

공개 정보에는 현재 시야 안의 적 상태만 포함된다. 마지막 관측 데이터는 별도로 유지하며, 상단 적 기지 HP에 기존 관측값을 표시할 때는 `*`로 구분한다.

## 테스트와 기록

- Unity 메뉴 `Window > General > Test Runner`에서 EditMode와 PlayMode 테스트를 실행한다.
- Unity가 패키지를 가져온 뒤 .NET 8 이상에서 `dotnet run --project Tools/Checks/TurnZero.Checks.csproj`로 같은 전투 코어 테스트를 실행할 수 있다.
- SAVE REPLAY는 `Application.persistentDataPath/Replays`에 JSON을 저장하고 Console에 위치를 출력한다.
- `TurnZero > Verify Debug Recording`으로 파일을 선택하면 기록된 각 턴의 판정이 동일하게 재현되는지 검사한다.
