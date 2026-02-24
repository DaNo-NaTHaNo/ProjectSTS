using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using DG.Tweening;
using System;

public class PortraitController : MonoBehaviour
{
    [Header("Repositories")]
    [SerializeField] private PortraitRepository portraitRepository;
    [SerializeField] private LocationRepository locationRepository;

    private Datasets _datasets;
    private Dictionary<string, GameObject> _activePortraits = new Dictionary<string, GameObject>();
    private Sequence _currentSequence;

    // --- 1단계 추가: 리셋 메서드 ---
    public void Reset()
    {
        // 진행 중인 애니메이션 중지
        if (_currentSequence != null && _currentSequence.IsActive())
        {
            _currentSequence.Kill();
        }

        // 모든 포트릿 오브젝트 제거
        foreach (var portrait in _activePortraits.Values)
        {
            if (portrait != null) Destroy(portrait);
        }
        _activePortraits.Clear();
    }

    public void Initialize(Datasets datasets)
    {
        _datasets = datasets;
        if (_datasets?.episodePortraits == null)
        {
            if (_datasets != null) _datasets.episodePortraits = new List<EpisodePortrait>();
        }
    }

    public void SkipAnimation()
    {
        if (_currentSequence != null && _currentSequence.IsActive() && _currentSequence.IsPlaying())
        {
            _currentSequence.Complete();
        }
    }

    public IEnumerator UpdatePortraitFromDialogue(DialogueLine line)
    {
        if (_currentSequence != null && _currentSequence.IsActive()) _currentSequence.Kill();
        _currentSequence = DOTween.Sequence();
        if (string.IsNullOrEmpty(line.locationPreset)) { yield break; }

        var preset = _datasets.locationPresets.FirstOrDefault(p => p.locationPreset == line.locationPreset);
        if (preset == null)
        {
            Debug.LogWarning($"[PortraitCtrl] LocationPreset '{line.locationPreset}' not found.");
            yield break;
        }

        // --- 1단계 수정: 컴파일 오류 해결 및 FF 적용 ---
        // Player.cs의 내부 static 클래스를 참조하기 위해 전체 경로 사용
        bool isFastForwarding = VisualNovelPlayer.TestModeGlobals.IsFastForwarding;
        float duration = isFastForwarding ? 0f : preset.duration;

        var requiredPortraits = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(preset.leftMost)) requiredPortraits[preset.leftMost] = "leftMost";
        if (!string.IsNullOrEmpty(preset.left)) requiredPortraits[preset.left] = "left";
        if (!string.IsNullOrEmpty(preset.center)) requiredPortraits[preset.center] = "center";
        if (!string.IsNullOrEmpty(preset.right)) requiredPortraits[preset.right] = "right";
        if (!string.IsNullOrEmpty(preset.rightMost)) requiredPortraits[preset.rightMost] = "rightMost";

        var portraitsToExit = _activePortraits.Keys.Except(requiredPortraits.Keys).ToList();

        if (!Enum.TryParse<Ease>(preset.ease, true, out var easeEnum)) { easeEnum = Ease.OutQuad; }

        foreach (var name in portraitsToExit)
        {
            if (_activePortraits.TryGetValue(name, out var obj))
            {
                _activePortraits.Remove(name);
                foreach (var img in obj.GetComponentsInChildren<Image>())
                    _currentSequence.Join(img.DOColor(Color.clear, duration).SetEase(easeEnum));
                _currentSequence.OnComplete(() => Destroy(obj));
            }
        }
        foreach (var pair in requiredPortraits)
        {
            bool isSpeaker = (line.speakerPortrait == pair.Key);
            var animFields = new PortraitAnimNodeFields
            {
                portraitName = pair.Key,
                location = pair.Value,
                offset = new PositionData { x = 0, y = 0 },
                face = isSpeaker ? line.face : "Default",
                animKey = isSpeaker ? line.animKey : "",
                endTint = new ColorData { r = Color.white.r, g = Color.white.g, b = Color.white.b, a = Color.white.a },
                duration = duration, // 수정된 duration 사용
                ease = easeEnum,
                spotLight = isSpeaker && line.spotLight,
                skippable = line.skippable
            };

            if (_activePortraits.ContainsKey(pair.Key))
            {
                ProcessAnimNode(animFields, _currentSequence);
            }
            else
            {
                var enterFields = new PortraitEnterNodeFields
                {
                    portraitName = animFields.portraitName,
                    location = animFields.location,
                    offset = new PositionData { x = 0, y = 0 },
                    face = animFields.face,
                    animKey = animFields.animKey,
                    startTint = new ColorData { r = Color.clear.r, g = Color.clear.g, b = Color.clear.b, a = Color.clear.a },
                    endTint = animFields.endTint,
                    duration = duration, // 수정된 duration 사용
                    ease = animFields.ease,
                    spotLight = animFields.spotLight,
                    skippable = animFields.skippable
                };
                ProcessEnterNode(enterFields, _currentSequence);
            }
        }

        // FF 중이 아닐 때만 대기
        if (_currentSequence.Duration() > 0 && !isFastForwarding)
        {
            yield return _currentSequence.WaitForCompletion();
        }
    }

    // --- 1단계 수정: ProcessNode 시그니처 변경 및 FF 로직 ---
    public IEnumerator ProcessNode(NodeData node, bool isFastForwarding)
    {
        var sequence = DOTween.Sequence();
        bool isSkippable = true;

        try
        {
            switch (node.type)
            {
                case "Portrait Enter":
                    var enterFields = JsonConvert.DeserializeObject<PortraitEnterNodeFields>(node.fields);
                    isSkippable = enterFields.skippable;
                    if (isFastForwarding) enterFields.duration = 0; // FF 적용
                    ProcessEnterNode(enterFields, sequence);
                    break;
                case "Portrait Anim":
                    var animFields = JsonConvert.DeserializeObject<PortraitAnimNodeFields>(node.fields);
                    isSkippable = animFields.skippable;
                    if (isFastForwarding) animFields.duration = 0; // FF 적용
                    ProcessAnimNode(animFields, sequence);
                    break;
                case "Portrait Exit":
                    var exitFields = JsonConvert.DeserializeObject<PortraitExitNodeFields>(node.fields);
                    isSkippable = exitFields.skippable;
                    if (isFastForwarding) exitFields.duration = 0; // FF 적용
                    ProcessExitNode(exitFields, sequence);
                    break;
            }
        }
        catch (Exception e) { Debug.LogError($"[PortraitCtrl] ERROR parsing node fields for {node.type}: {e.Message}"); }

        // FF 중이 아닐 때만 재생 및 스킵 대기
        if (sequence.Duration() > 0 && !isFastForwarding)
        {
            while (sequence.IsActive() && sequence.IsPlaying())
            {
                if (isSkippable && Input.GetMouseButtonDown(0))
                {
                    sequence.Complete();
                    break;
                }
                yield return null;
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private void ProcessEnterNode(PortraitEnterNodeFields fields, Sequence sequence)
    {
        if (fields == null || string.IsNullOrEmpty(fields.portraitName)) return;
        if (_activePortraits.ContainsKey(fields.portraitName)) return;
        var episodePortraitEntry = _datasets.episodePortraits.FirstOrDefault(p => p.portraitName == fields.portraitName);
        if (episodePortraitEntry == null) return;
        GameObject portraitPrefab = portraitRepository.GetObject(episodePortraitEntry.portraitID);
        if (portraitPrefab == null) return;
        GameObject newPortrait = Instantiate(portraitPrefab, this.transform);
        newPortrait.name = fields.portraitName;
        _activePortraits.Add(fields.portraitName, newPortrait);
        var rt = newPortrait.GetComponent<RectTransform>();
        if (rt == null) return;
        var offsetVector = new Vector2(fields.offset.x, fields.offset.y);
        rt.anchoredPosition = GetLocationVector(fields.location) + offsetVector;
        foreach (var image in newPortrait.GetComponentsInChildren<Image>())
        {
            var startColor = new Color(fields.startTint.r, fields.startTint.g, fields.startTint.b, fields.startTint.a);
            var endColor = new Color(fields.endTint.r, fields.endTint.g, fields.endTint.b, fields.endTint.a);

            image.color = startColor;
            sequence.Join(image.DOColor(endColor, fields.duration).SetEase(fields.ease));
        }
        ChangeFace(newPortrait.transform, fields.face);
    }

    private void ProcessAnimNode(PortraitAnimNodeFields fields, Sequence sequence)
    {
        if (fields == null || string.IsNullOrEmpty(fields.portraitName)) return;
        if (_activePortraits.TryGetValue(fields.portraitName, out GameObject portrait))
        {
            var rt = portrait.GetComponent<RectTransform>();
            if (rt == null) return;
            var offsetVector = new Vector2(fields.offset.x, fields.offset.y);
            sequence.Join(rt.DOAnchorPos(GetLocationVector(fields.location) + offsetVector, fields.duration).SetEase(fields.ease));
            foreach (var img in portrait.GetComponentsInChildren<Image>())
            {
                var endColor = new Color(fields.endTint.r, fields.endTint.g, fields.endTint.b, fields.endTint.a);
                sequence.Join(img.DOColor(endColor, fields.duration).SetEase(fields.ease));
            }
            ChangeFace(portrait.transform, fields.face);
        }
    }

    private void ProcessExitNode(PortraitExitNodeFields fields, Sequence sequence)
    {
        if (fields == null || string.IsNullOrEmpty(fields.portraitName)) return;
        if (_activePortraits.TryGetValue(fields.portraitName, out GameObject portraitToExit))
        {
            _activePortraits.Remove(fields.portraitName);
            foreach (var img in portraitToExit.GetComponentsInChildren<Image>())
            {
                var endColor = new Color(fields.endTint.r, fields.endTint.g, fields.endTint.b, fields.endTint.a);
                sequence.Join(img.DOColor(endColor, fields.duration).SetEase(fields.ease));
            }
            sequence.OnComplete(() => Destroy(portraitToExit));
        }
    }

    private void ChangeFace(Transform portraitTransform, string faceName)
    {
        if (string.IsNullOrEmpty(faceName)) faceName = "Default";

        Transform defaultFaceTransform = portraitTransform.Find("Portrait_Default");
        Image defaultImage = defaultFaceTransform?.GetComponent<Image>();

        if (defaultImage == null)
        {
            return;
        }
        Color baseColor = defaultImage.color;

        for (int i = 0; i < portraitTransform.childCount; i++)
        {
            var child = portraitTransform.GetChild(i);
            if (child.name.StartsWith("Portrait_") && child.name != "Portrait_Default")
            {
                child.gameObject.SetActive(false);
            }
        }

        Transform faceToActivate = portraitTransform.Find($"Portrait_{faceName}");
        if (faceToActivate != null)
        {
            Image faceImage = faceToActivate.GetComponent<Image>();
            if (faceImage != null)
            {
                faceImage.color = baseColor;
            }
            faceToActivate.gameObject.SetActive(true);
        }
    }

    private Vector2 GetLocationVector(string location)
    {
        switch (location)
        {
            case "leftMost": return locationRepository.leftMost;
            case "left": return locationRepository.left;
            case "center": return locationRepository.center;
            case "right": return locationRepository.right;
            case "rightMost": return locationRepository.rightMost;
            default:
                try
                {
                    var parts = location.Split(',').Select(s => float.Parse(s.Trim())).ToArray();
                    if (parts.Length == 2) return new Vector2(parts[0], parts[1]);
                }
                catch { }
                Debug.LogWarning($"[PortraitCtrl] Location '{location}' not found or invalid. Defaulting to center.");
                return locationRepository.center;
        }
    }
}