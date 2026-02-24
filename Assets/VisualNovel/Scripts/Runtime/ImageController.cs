using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Collections;
using DG.Tweening;

public class ImageController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cutInImage;
    [SerializeField] private Image popUpImage;

    [Header("Repositories")]
    [SerializeField] private ImageBackgroundRepository imageBackgroundRepository;
    [SerializeField] private ImageCutInRepository imageCutInRepository;
    [SerializeField] private ImagePopUpRepository imagePopUpRepository;

    // --- 1단계 추가: 리셋 메서드 ---
    /// <summary>
    /// 현재 표시된 모든 이미지를 숨기고 상태를 초기화합니다.
    /// </summary>
    public void Reset()
    {
        // 모든 활성 트윈을 중지합니다.
        DOTween.Kill(backgroundImage);
        DOTween.Kill(cutInImage);
        DOTween.Kill(popUpImage);

        // 이미지를 즉시 숨깁니다 (스프라이트 제거 및 알파값 0)
        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
            backgroundImage.color = Color.clear;
        }
        if (cutInImage != null)
        {
            cutInImage.sprite = null;
            cutInImage.color = Color.clear;
        }
        if (popUpImage != null)
        {
            popUpImage.sprite = null;
            popUpImage.color = Color.clear;
        }
    }

    // --- 1단계 수정: ProcessNode 시그니처 변경 및 FF 로직 ---
    public IEnumerator ProcessNode(NodeData node, bool isFastForwarding)
    {
        var fields = JsonConvert.DeserializeObject<ImageNodeFields>(node.fields);
        if (fields == null) yield break;

        Image targetImage = null;
        BaseRepository<Sprite> targetRepository = null;

        switch (fields.display)
        {
            case "Background": targetImage = backgroundImage; targetRepository = imageBackgroundRepository; break;
            case "CutIn": targetImage = cutInImage; targetRepository = imageCutInRepository; break;
            case "PopUp": targetImage = popUpImage; targetRepository = imagePopUpRepository; break;
        }

        if (targetImage == null) yield break;

        // [1단계 수정] FF 중이면 duration 0
        float duration = isFastForwarding ? 0f : fields.duration;

        Sequence sequence = DOTween.Sequence();
        var startColor = new Color(fields.startTint.r, fields.startTint.g, fields.startTint.b, fields.startTint.a);
        var endColor = new Color(fields.endTint.r, fields.endTint.g, fields.endTint.b, fields.endTint.a);

        if (fields.controlType == "Enter")
        {
            Sprite spriteToShow = targetRepository.GetObject(fields.imageName);
            if (spriteToShow != null)
            {
                targetImage.sprite = spriteToShow;
                targetImage.color = startColor; // 변환된 Color 사용
                // [1단계 수정] duration 변수 사용
                sequence.Append(targetImage.DOColor(endColor, duration).SetEase(fields.ease));
            }
        }
        else if (fields.controlType == "Exit")
        {
            targetImage.color = startColor; // 변환된 Color 사용
            // [1단계 수정] duration 변수 사용
            sequence.Append(targetImage.DOColor(endColor, duration).SetEase(fields.ease));
            sequence.OnComplete(() => targetImage.sprite = null);
        }

        // [1단계 수정] FF 중이 아닐 때만 대기 및 스킵 처리
        if (sequence.Duration() > 0 && !isFastForwarding)
        {
            while (sequence.IsPlaying())
            {
                if (fields.skippable && Input.GetMouseButtonDown(0))
                {
                    sequence.Complete();
                    break;
                }
                yield return null;
            }
            yield return new WaitForEndOfFrame();
        }
    }
}