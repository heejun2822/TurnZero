# 임시 전투 아트

- `TurnZero/Assets/Resources/PrototypeUI/portrait-atlas.png`: 내장 `imagegen` 도구로 생성한 2×2 초상화 아틀라스. 위쪽부터 Guardian, Scout, Ranger, Support 순서로 잘라 HUD에서 사용한다.
- 같은 폴더의 액션·기지·무기·숫자 배지 PNG는 `Create-PrototypeIcons.ps1`의 벡터 도형으로 생성한다. 프로젝트 루트에서 `& 'Tools/Art/Create-PrototypeIcons.ps1'`로 다시 만들 수 있다.
- 편집기 전용 `BattlePrefabBuilder`가 돌·잔디 타일, 성벽·나무, 성과 네 가지 캐릭터 외형을 임시 메시로 만든다. 메시·재질·잘라낸 초상화는 `Assets/Art/Prototype`에 저장하고 모델과 UI는 프리팹으로 씬에 배치한다. 런타임 `BattleWorldView`는 배치된 오브젝트의 표시를 갱신한다. 주변 장식은 이동·시야·공격 규칙에 영향을 주지 않는다.
- Guardian / Scout / Ranger / Support는 현재 외형 구분용 이름이다. 캐릭터별 능력 차이·장비 보너스는 아직 없다. HP·피해·거리·AP는 기존 임시 규칙을 사용한다.
- 초상화의 원본 생성 파일은 Codex 생성 이미지 폴더에 보존하고, 게임이 사용하는 사본은 프로젝트 안에 저장했다.

## 초상화 생성 프롬프트

```text
Use case: stylized-concept. Asset type: one square 2 by 2 character portrait sprite atlas for a 3D fantasy tactics game's dark teal HUD. Create a SINGLE texture containing four equal square portrait panels, with exact panel borders at the horizontal and vertical midpoint, no gutters. Each quadrant is a separate chest-up low-poly 3D RPG character render, centered, fills its panel, clean faceted angular forms, polished but placeholder game art, soft studio light. Top left: armored guardian, closed silver helmet with narrow dark visor, teal tabard, steel shoulder armor. Top right: young scout wearing a muted olive hood with gold trim, brown leather armor, teal scarf. Bottom left: ranger with teal hood, brown leather straps and a bow over shoulder. Bottom right: support mage, white hood edged with teal and gold, small cyan crystal staff. All friendly adventurers, coherent visual style, three-quarter view facing slightly toward center of their own portrait. Each panel has the same near-black blue background (#0b151d), no decorations. No text, numbers, frames, logos, UI, watermarks, weapons crossing quadrant boundaries. Output a square atlas with four equal quadrants only.
```
