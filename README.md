# TurnZero

Unity 6000.3.5f2 기반의 3D 동시 턴 전술 게임 프로토타입.

## 실행

1. Unity Hub에서 `TurnZero` 폴더를 연다.
2. `Assets/Scenes/Battle.unity`를 열거나 메뉴 `TurnZero > Open Battle Prototype`을 선택한다.
3. Play를 누르고 P1, P2 순서로 강조된 칸에 기지를 배치한다.
4. 3D 필드에서 내 캐릭터 → MOVE / ATTACK → 대상 타일 또는 적 모델 순서로 명령을 지정한다.
5. VIEW / SWITCH 버튼으로 상대 시점으로 전환해 다른 플레이어의 명령을 입력한다.
6. 30초가 지나면 동시 판정한다. 빠른 검증에는 `ADVANCE TURN`을 사용한다.

WAIT 버튼은 선택한 캐릭터의 명령과 AP 예약을 취소한다. READY는 해당 플레이어의 수정만 막는다. 종료 후 RESTART로 새 경기를 시작한다.

필드는 XZ 평면의 입체 타일이며 원근 카메라로 내려다본다. ROTATE로 시점을 회전하고 ZOOM + / -로 확대·축소한다. 마우스 클릭과 터치로 월드 오브젝트를 선택한다. 캐릭터·기지는 조명과 그림자가 적용되는 임시 3D 모델이다.

기본 UI는 캐릭터 선택·명령·HP·AP·타이머를 영어로 표시한다. 상세 HUD와 임시 초상화는 다음 단계에서 추가한다.

룰과 검증용 수치는 [Development.md](Development.md), 원본 기획은 [Plan.md](Plan.md)를 참고한다. 현재 구현은 로컬 기본 전투이며 온라인·스킬·아이템·부활·필드 편집·재화는 이후 단계다.

## 테스트와 기록

- Unity 메뉴 `Window > General > Test Runner`에서 EditMode와 PlayMode 테스트를 실행한다.
- Unity가 패키지를 가져온 뒤 .NET 8 이상에서 `dotnet run --project Tools/Checks/TurnZero.Checks.csproj`로 같은 전투 코어 테스트를 실행할 수 있다.
- SAVE REPLAY는 `Application.persistentDataPath/Replays`에 JSON을 저장하고 Console에 위치를 출력한다.
- `TurnZero > Verify Debug Recording`으로 파일을 선택하면 기록된 각 턴의 판정이 동일하게 재현되는지 검사한다.
