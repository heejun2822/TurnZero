# TurnZero

Unity 6000.3.5f2 기반의 3D 동시 턴 전술 게임.

Unity Hub에서 `TurnZero` 폴더를 연다. 현재 씬은 `Assets/Scenes/SampleScene.unity`이며, 전투 코어와 규칙 검증을 먼저 추가했다. 플레이 가능한 Battle 씬은 다음 단계에서 연결한다.

기획은 [Plan.md](Plan.md), 임시 규칙과 개발 순서는 [Development.md](Development.md)를 참고한다.

## 전투 코어 검증

- Unity 메뉴 `Window > General > Test Runner`의 EditMode에서 전투 규칙 21개를 검사한다.
- Unity가 패키지를 가져온 뒤 .NET 8 이상에서 `dotnet run --project Tools/Checks/TurnZero.Checks.csproj`로 같은 검사를 실행할 수 있다.
- 전투 코어는 `Assets/Scripts/Core`에 있으며 Unity와 무관한 C#으로 상태·명령·동시 판정·공개 정보를 처리한다.
