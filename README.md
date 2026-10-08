# TurnZero

Unity 6000.3.5f2 기반의 3D 동시 턴 전술 게임 프로토타입.

## 실행

1. Unity Hub에서 `TurnZero` 폴더를 연다.
2. `Assets/Scenes/Battle.unity`를 열거나 메뉴 `TurnZero > Open Battle Prototype`을 선택한다.
3. Play를 누르고 P1, P2 순서로 강조된 칸에 기지를 배치한다.
4. 3D 필드에서 내 캐릭터 → MOVE / ATTACK → 대상 타일 또는 적 모델 순서로 명령을 지정한다.
5. VIEW / SWITCH 버튼으로 상대 시점으로 전환해 다른 플레이어의 명령을 입력한다.
6. 30초가 지나면 동시 판정한다. 빠른 검증에는 `ADVANCE TURN`을 사용한다.

오른쪽 패널의 되돌리기 버튼은 선택한 캐릭터의 명령과 AP 예약을 취소한다. READY는 해당 플레이어의 수정만 막는다. 종료 후 RESTART로 새 경기를 시작한다.

필드는 XZ 평면의 입체 타일이며 원근 카메라로 내려다본다. ROTATE로 시점을 회전하고 ZOOM + / -로 확대·축소한다. 마우스 클릭과 터치로 월드 오브젝트를 선택한다. 캐릭터·기지는 조명과 그림자가 적용되는 임시 3D 모델이다.

상단은 기지 HP·턴·시간, 오른쪽은 선택 캐릭터·명령·장비 표시, 하단은 캐릭터 카드·명령·사용 가능한 AP와 예약량을 표시한다. UI는 영어이며 SKILL·ITEM은 비활성이다. 임시 초상화·아이콘 생성 기록은 [Tools/Art/README.md](Tools/Art/README.md)를 참고한다.

룰과 검증용 수치는 [Development.md](Development.md), 원본 기획은 [Plan.md](Plan.md)를 참고한다. 현재 구현은 로컬 기본 전투이며 온라인·스킬·아이템·부활·필드 편집·재화는 이후 단계다.

## 씬과 프리팹 편집

`Battle.unity`에는 `Battle Session` 프리팹이 미리 배치되어 있다. Play 전에도 고정 맵, 양측 캐릭터와 기지, HUD를 확인하고 편집할 수 있다. Play와 RESTART는 경기 데이터와 표시 상태만 초기화하며 오브젝트를 생성하거나 교체하지 않는다. 캐릭터의 편집기 위치는 미리보기이며 실제 시작 위치는 기지 선택 후 정해진다.

- `Assets/Prefabs/Map`: 타일과 8×10 고정 맵. `BattleMapView`가 좌표·시야·강조 표시를 담당한다.
- `Assets/Prefabs/Characters`: Guardian, Scout, Ranger, Support, Base. `BattleActorView`가 위치·표시·라벨을 갱신한다. 모델을 교체할 때 루트의 스크립트와 연결된 라벨·선택 대상을 유지한다.
- `Assets/Prefabs/UI`: 상단 바, 명령 패널, 역할별 카드, AP 패널, 툴바와 HUD. 각 View의 Inspector에서 UI와 초상화 참조를 편집한다.
- `BattleCameraRig`: 원근 카메라와 `BattleCameraRig` 스크립트. `BattleHudLayout`은 화면 방향·안전 영역에 따라 HUD 배치를 조정한다.
- `LastSeenMarker`: 마지막 관측 위치와 당시 HP 표시. 씬에 준비된 마커를 켜고 끈다.
- `BattleSession`: 위 프리팹들의 연결과 `BattleController`. Controller의 Rules에서 HP·AP·공격·턴 시간을 변경한다. 맵 크기는 배치된 `BattleMapView`를 기준으로 한다.

전투 상태·명령 검증·동시 판정은 `Scripts/Core`에 유지한다. 런타임 스크립트는 프리팹이나 UI를 만들지 않는다. 임시 모델과 UI를 만드는 코드는 `Assets/Editor/BattlePrefabBuilder.*.cs`에 있으며 플레이어 빌드에 포함되지 않는다.

`TurnZero > Rebuild Prototype Scene and Prefabs`는 임시 에셋과 씬을 명시적으로 다시 만드는 개발 도구다. 직접 수정한 프리팹과 씬 배치를 덮어쓰므로 일반 실행이나 UI 편집에는 사용하지 않는다.

## 테스트와 기록

- Unity 메뉴 `Window > General > Test Runner`에서 EditMode와 PlayMode 테스트를 실행한다.
- Unity가 패키지를 가져온 뒤 .NET 8 이상에서 `dotnet run --project Tools/Checks/TurnZero.Checks.csproj`로 같은 전투 코어 테스트를 실행할 수 있다.
- SAVE REPLAY는 `Application.persistentDataPath/Replays`에 JSON을 저장하고 Console에 위치를 출력한다.
- `TurnZero > Verify Debug Recording`으로 파일을 선택하면 기록된 각 턴의 판정이 동일하게 재현되는지 검사한다.
