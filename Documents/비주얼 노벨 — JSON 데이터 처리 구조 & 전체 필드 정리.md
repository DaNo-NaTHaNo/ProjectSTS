# 비주얼 노벨 — JSON 데이터 처리 구조 & 전체 필드 정리

---

## 1\. 전체 데이터 흐름

\[CSV 파일\] ──Import──▶ \[VisualNovelSO (ScriptableObject)\]

                              │

                              │  에디터에서 편집 & 저장

                              ▼

                     ┌─────────────────────┐

                     │   VisualNovelSO     │

                     │  ─────────────────  │

                     │  dialogueLines\[\]    │ ◀── CSV Data

                     │  episodePortraits\[\] │ ◀── CSV Data

                     │  locationPresets\[\]  │ ◀── CSV Data

                     │  nodeGraphJson      │ ◀── 노드 에디터 JSON 문자열

                     └────────┬────────────┘

                              │

                   LoadEpisodeDataFromSO()

                              │

                              ▼

                     ┌─────────────────────┐

                     │   EpisodeData       │  ◀── 런타임 재생용 통합 객체

                     │  ─────────────────  │

                     │  episodeInfo        │

                     │  assetManifest      │

                     │  datasets           │

                     │  nodeGraph          │

                     └────────┬────────────┘

                              │

                     VisualNovelPlayer가

                     노드를 순회하며 재생

**핵심:** SO(ScriptableObject)는 **에디터 저장소**, EpisodeData는 **런타임 재생용 구조체**입니다.

---

## 2\. EpisodeData — JSON 최상위 구조

| 필드 | 타입 | 설명 |
| :---- | :---- | :---- |
| `episodeInfo` | `EpisodeInfo` | 에피소드 메타데이터 (id, title, version) |
| `assetManifest` | `AssetManifest` | 사용할 에셋 목록 |
| `datasets` | `Datasets` | 3종 CSV 데이터셋 |
| `nodeGraph` | `NodeGraph` | 노드 그래프 (재생 흐름) |

### AssetManifest

| 필드 | 타입 | 설명 |
| :---- | :---- | :---- |
| `portraits` | `List<string>` | 초상화 에셋 경로 |
| `backgroundImages` | `List<string>` | 배경 이미지 |
| `cutInImages` | `List<string>` | 컷인 이미지 |
| `popUpImages` | `List<string>` | 팝업 이미지 |
| `sfx` | `List<string>` | 효과음 |
| `bgm` | `List<string>` | 배경음악 |

---

## 3\. NodeGraph — 노드 그래프 구조

### NodeData (개별 노드)

{

  "id": "guid-문자열",

  "type": "Text",

  "position": { "x": 100.0, "y": 200.0 },

  "fields": "{\\"sceneName\\":\\"scene01\\",\\"display\\":\\"Bottom\\" }"

}

⚠️ **이중 직렬화:** `fields`는 JSON 문자열 안에 또 다른 JSON 문자열이 들어있습니다. 플레이어에서 `JsonConvert.DeserializeObject<XxxNodeFields>(node.fields)` 형태로 2차 역직렬화합니다.

### ConnectionData (노드 연결)

| 필드 | 타입 | 설명 |
| :---- | :---- | :---- |
| `sourceNodeId` | `string` | 출발 노드 ID |
| `sourceOutputIndex` | `int` | 출력 포트 인덱스 (Branch 선택지 순서 결정) |
| `targetNodeId` | `string` | 도착 노드 ID |

---

## 4\. 노드 타입 & Fields 클래스 전체 정리

노드 타입은 C\# enum이 아닌 **string 기반**으로 처리됩니다.

---

### 4-1. `"Start"` 노드

- **Fields:** 없음  
- **처리:** 재생 시작점. 연결된 다음 노드로 이동  
- **NodeView:** (별도 StartNodeView)

---

### 4-2. `"End"` 노드

- **Fields:** 없음 (SaveData → null 반환)  
- **처리:** 재생 종료점  
- **NodeView:** `EndNodeView`

---

### 4-3. `"Text"` 노드 → `TextNodeFields`

| 필드 | 타입 | 설명 | 기본값 |
| :---- | :---- | :---- | :---- |
| `sceneName` | `string` | DialogueLine과 매칭하는 키 | — |
| `display` | `string` | 표시 모드 | `"Bottom"` |

**display 선택지:** `"Bottom"`, `"Monologue"`

**처리 흐름 (VisualNovelPlayer.ExecuteTextNode):**

1. `sceneName`으로 `DialogueLine` 목록을 필터링  
2. 각 대사마다 `portraitController.UpdatePortraitFromDialogue()` \+ `textController.PlayDialogueLine()` 동시 실행  
3. 타이핑 완료 후 클릭 대기

---

### 4-4. `"Branch"` 노드 → `BranchNodeFields`

| 필드 | 타입 | 설명 |
| :---- | :---- | :---- |
| `sceneName` | `string` | DialogueLine과 매칭하는 키 |

**처리 흐름 (VisualNovelPlayer.ExecuteBranchNode):**

1. `sceneName`으로 `DialogueLine` 목록을 필터링  
2. 각 대사의 `speakerText`를 선택지 버튼으로 표시  
3. 선택한 인덱스 → `ConnectionData.sourceOutputIndex`로 다음 노드 결정  
4. 최대 **4개** 분기 출력 포트

---

### 4-5. `"Wait"` 노드 → `WaitNodeFields`

| 필드 | 타입 | 설명 | 기본값 |
| :---- | :---- | :---- | :---- |
| `duration` | `float` | 대기 시간(초) | — |

**처리:** `WaitForSeconds(duration)` (FF 중에는 스킵)

---

### 4-6. `"Portrait Enter"` 노드 → `PortraitEnterNodeFields`

| 필드 | 타입 | 설명 | 기본값 |
| :---- | :---- | :---- | :---- |
| `portraitName` | `string` | 캐릭터 이름 (EpisodePortrait.portraitName과 매칭) | — |
| `location` | `string` | 등장 위치 | `"center"` |
| `offset` | `PositionData` | 위치 미세 조정 (x, y) | (0, 0\) |
| `face` | `string` | 표정 | `"Default"` |
| `animKey` | `string` | 애니메이션 키 | `"None"` |
| `startTint` | `ColorData` | 시작 색상 (RGBA) | `Color.clear` |
| `endTint` | `ColorData` | 끝 색상 (RGBA) | `Color.white` |
| `duration` | `float` | 전환 시간(초) | `1.0` |
| `ease` | `Ease` (enum) | DOTween 이징 | `OutQuad` |
| `spotLight` | `bool` | 스포트라이트 ON/OFF | `false` |
| `skippable` | `bool` | 스킵 가능 여부 | `false` |

**location 선택지:** `"leftMost"`, `"left"`, `"center"`, `"right"`, `"rightMost"`

**face 선택지:** `"Default"`, `"Smile"`, `"Laugh"`, `"Angry"`, `"Fury"`, `"Sad"`, `"Cry"`, `"Think"`, `"Surprised"`, `"ClosedEyes"`

**animKey 선택지:** `"None"`, `"Shake"`, `"Jump"`

**처리:** `PortraitController.ProcessEnterNode()` → 프리팹 인스턴스화 → 위치 배치 → 색상 트윈

---

### 4-7. `"Portrait Anim"` 노드 → `PortraitAnimNodeFields`

| 필드 | 타입 | 설명 | 기본값 |
| :---- | :---- | :---- | :---- |
| `portraitName` | `string` | 대상 캐릭터 이름 | — |
| `location` | `string` | 이동 목적지 | `"center"` |
| `offset` | `PositionData` | 위치 미세 조정 | (0, 0\) |
| `face` | `string` | 변경할 표정 | `"Default"` |
| `animKey` | `string` | 애니메이션 키 | `"None"` |
| `endTint` | `ColorData` | 끝 색상 | `Color.white` |
| `duration` | `float` | 전환 시간(초) | `1.0` |
| `ease` | `Ease` (enum) | DOTween 이징 | `OutQuad` |
| `spotLight` | `bool` | 스포트라이트 ON/OFF | `false` |
| `skippable` | `bool` | 스킵 가능 여부 | `false` |

**처리:** `PortraitController.ProcessAnimNode()` → 위치 이동 트윈 \+ 색상 트윈 \+ 표정 변경

---

### 4-8. `"Portrait Exit"` 노드 → `PortraitExitNodeFields`

| 필드 | 타입 | 설명 | 기본값 |
| :---- | :---- | :---- | :---- |
| `portraitName` | `string` | 퇴장할 캐릭터 이름 | — |
| `animKey` | `string` | 퇴장 애니메이션 | `"None"` |
| `endTint` | `ColorData` | 끝 색상 (보통 투명으로) | `Color.black` |
| `duration` | `float` | 전환 시간(초) | `1.0` |
| `ease` | `Ease` (enum) | DOTween 이징 | `OutQuad` |
| `spotLight` | `bool` | 스포트라이트 | `false` |
| `skippable` | `bool` | 스킵 가능 여부 | `false` |

**처리:** `PortraitController.ProcessExitNode()` → 색상 트윈 → 완료 후 Destroy

---

### 4-9. `"Image"` 노드 → `ImageNodeFields`

| 필드 | 타입 | 설명 | 기본값 |
| :---- | :---- | :---- | :---- |
| `controlType` | `string` | 제어 방식 | `"Enter"` |
| `display` | `string` | 이미지 종류 | `"Background"` |
| `imageName` | `string` | 이미지 에셋 이름 | — |
| `startTint` | `ColorData` | 시작 색상 | `Color.clear` |
| `endTint` | `ColorData` | 끝 색상 | `Color.white` |
| `duration` | `float` | 전환 시간(초) | `1.0` |
| `ease` | `Ease` (enum) | DOTween 이징 | `OutQuad` |
| `skippable` | `bool` | 스킵 가능 여부 | `false` |

**controlType 선택지:** `"Enter"`, `"Exit"`

**display 선택지:** `"Background"`, `"CutIn"`, `"PopUp"`

**처리:** `ImageController.ProcessNode()` → display에 따라 대상 Image UI 선택 → 색상 트윈

---

### 4-10. `"Sound"` 노드 → `SoundNodeFields`

| 필드 | 타입 | 설명 | 기본값 |
| :---- | :---- | :---- | :---- |
| `controlType` | `string` | 제어 방식 | `"Play"` |
| `display` | `string` | 사운드 종류 | `"BGM"` |
| `soundName` | `string` | 사운드 에셋 이름 | — |
| `fadeDuration` | `float` | 페이드 시간(초) | `0` |
| `volume` | `float` | 볼륨 (0\~1) | `1.0` |
| `isLoop` | `bool` | 반복 재생 | `false` |
| `skippable` | `bool` | 스킵 가능 여부 | `false` |

**controlType 선택지:** `"Play"`, `"Stop"`

**display 선택지:** `"BGM"`, `"SFX"`

**처리:** `SoundController.ProcessNode()` → display에 따라 AudioSource 선택 → Play/Stop \+ 볼륨 페이드

---

### 4-11. `"Comment"` 노드

- **Fields:** Comment(String) \- 에디터에 표시할 메모를 입력, 기본값 없음  
- **처리:** 완전히 무시됨. 에디터 전용 메모

---

## 5\. 3종 데이터셋 (Datasets) 상세

### 5-1. DialogueLine

| 필드 | 타입 | 설명 |
| :---- | :---- | :---- |
| `sceneName` | `string` | **노드와 연결하는 키** (Text/Branch 노드의 sceneName과 1:N 매칭) |
| `locationPreset` | `string` | LocationPreset 이름 |
| `speakerPortrait` | `string` | 화자 초상화 ID (EpisodePortrait.portraitName과 매칭) |
| `speakerName` | `string` | 화자 이름 (화면 표시용) |
| `speakerGroup` | `string` | 화자 그룹 |
| `speakerText` | `string` | 대사 텍스트 |
| `face` | `string` | 표정 |
| `animKey` | `string` | 애니메이션 키 |
| `emote` | `string` | 이모트 |
| `sfx` | `string` | 효과음 |
| `spotLight` | `bool` | 스포트라이트 ON/OFF |
| `textSpeed` | `float` | 텍스트 출력 속도 (초당 글자 수) |
| `skippable` | `bool` | 스킵 가능 여부 |
| `autoNext` | `bool` | 자동 다음 대사 |
| `voice` | `string` | 음성 파일 |
| `wait` | `float` | 대사 후 대기 시간 |

### 5-2. EpisodePortrait

| 필드 | 타입 | 설명 |
| :---- | :---- | :---- |
| `portraitID` | `string` | 초상화 고유 ID (Repository 에셋 조회 키) |
| `portraitName` | `string` | 초상화 표시 이름 (노드 및 DialogueLine에서 참조하는 이름) |

### 5-3. LocationPreset

| 필드 | 타입 | 설명 |
| :---- | :---- | :---- |
| `locationPreset` | `string` | 프리셋 이름 (DialogueLine.locationPreset와 매칭) |
| `leftMost` | `string` | 맨 왼쪽 위치의 캐릭터 portraitName |
| `left` | `string` | 왼쪽 위치 |
| `center` | `string` | 가운데 위치 |
| `right` | `string` | 오른쪽 위치 |
| `rightMost` | `string` | 맨 오른쪽 위치 |
| `ease` | `string` | DOTween Ease 이름 (런타임에 Enum.TryParse로 변환) |
| `duration` | `float` | 전환 애니메이션 시간 |

---

## 6\. 데이터 간 연결 관계 (키 매핑)

\[Text/Branch 노드\]

    fields.sceneName ──────────▶ DialogueLine.sceneName (1:N 매칭)

\[DialogueLine\]

    .speakerPortrait ──────────▶ EpisodePortrait.portraitName

    .locationPreset  ──────────▶ LocationPreset.locationPreset

\[EpisodePortrait\]

    .portraitID ───────────────▶ PortraitRepository.GetObject(portraitID) → 프리팹

\[Portrait 노드들\]

    fields.portraitName ───────▶ EpisodePortrait.portraitName

\[Image 노드\]

    fields.imageName ──────────▶ ImageXxxRepository.GetObject(imageName) → Sprite

\[Sound 노드\]

    fields.soundName ──────────▶ SoundXxxRepository.GetObject(soundName) → AudioClip

\[NodeGraph\]

    ConnectionData.sourceNodeId ─▶ NodeData.id (출발)

    ConnectionData.targetNodeId ─▶ NodeData.id (도착)

---

## 7\. Enum & 문자열 선택지 총정리

### 실제 C\# Enum으로 사용되는 것

| Enum | 라이브러리 | 사용처 |
| :---- | :---- | :---- |
| `DG.Tweening.Ease` | DOTween | Portrait Enter/Anim/Exit, Image 노드의 `ease` 필드 |

### 문자열 기반 선택지 (Dropdown)

| 필드명 | 선택지 | 사용 노드 |
| :---- | :---- | :---- |
| `display` (텍스트) | `Bottom`, `Monologue` | Text |
| `location` | `leftMost`, `left`, `center`, `right`, `rightMost` | Portrait Enter, Portrait Anim |
| `face` | `Default`, `Smile`, `Laugh`, `Angry`, `Fury`, `Sad`, `Cry`, `Think`, `Surprised`, `ClosedEyes` | Portrait Enter, Portrait Anim |
| `animKey` | `None`, `Shake`, `Jump` | Portrait Enter, Portrait Anim, Portrait Exit |
| `controlType` (이미지) | `Enter`, `Exit` | Image |
| `display` (이미지) | `Background`, `CutIn`, `PopUp` | Image |
| `controlType` (사운드) | `Play`, `Stop` | Sound |
| `display` (사운드) | `BGM`, `SFX` | Sound |

---

## 8\. 컨트롤러 — 담당 역할 요약

| 컨트롤러 | 담당 노드 타입 | 핵심 역할 |
| :---- | :---- | :---- |
| `TextController` | Text, Branch | 타이핑 효과, 대사 박스 표시, 선택지 버튼 |
| `PortraitController` | Portrait Enter/Anim/Exit \+ DialogueLine | 캐릭터 프리팹 생성/이동/퇴장, 표정 변경, 색상 트윈 |
| `ImageController` | Image | 배경/컷인/팝업 이미지 전환 |
| `SoundController` | Sound | BGM/SFX 재생/정지, 볼륨 페이드 |

---

## 9\. 공용 데이터 구조

### PositionData

public class PositionData { public float x; public float y; }

### ColorData

public class ColorData { public float r; public float g; public float b; public float a; }

### BaseNodeFields (추상 부모 클래스)

모든 XxxNodeFields 클래스가 상속하는 기반 클래스입니다. `NodeView.SaveData()` → `BaseNodeFields` 반환 → JSON 직렬화 → `NodeData.fields`에 문자열로 저장

---

