using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using DG.Tweening;

public class SoundController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Repositories")]
    [SerializeField] private SoundBackgroundRepository soundBackgroundRepository;
    [SerializeField] private SoundEffectRepository soundEffectRepository;

    // --- 1단계 추가: 리셋 메서드 ---
    /// <summary>
    /// 현재 재생 중인 모든 오디오를 중지하고 상태를 초기화합니다.
    /// </summary>
    public void Reset()
    {
        // 모든 활성 트윈을 중지합니다.
        if (bgmAudioSource != null) DOTween.Kill(bgmAudioSource);
        if (sfxAudioSource != null) DOTween.Kill(sfxAudioSource);

        // 오디오 소스를 즉시 중지합니다.
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
            bgmAudioSource.clip = null;
        }
        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
            sfxAudioSource.clip = null;
        }
    }

    // --- 1단계 수정: ProcessNode 시그니처 변경 및 FF 로직 ---
    public IEnumerator ProcessNode(NodeData node, bool isFastForwarding)
    {
        var fields = JsonConvert.DeserializeObject<SoundNodeFields>(node.fields);
        if (fields == null) yield break;

        AudioSource targetAudioSource = null;
        BaseRepository<AudioClip> targetRepository = null;

        switch (fields.display) // BGM or SFX
        {
            case "BGM": targetAudioSource = bgmAudioSource; targetRepository = soundBackgroundRepository; break;
            case "SFX": targetAudioSource = sfxAudioSource; targetRepository = soundEffectRepository; break;
        }

        if (targetAudioSource == null) yield break; // 유효한 AudioSource가 없으면 종료

        // [1단계 수정] FF 중이면 fadeDuration 0
        float fadeDuration = isFastForwarding ? 0f : fields.fadeDuration;

        Sequence sequence = DOTween.Sequence(); // DOTween 시퀀스 생성

        if (fields.controlType == "Play")
        {
            AudioClip clipToPlay = targetRepository.GetObject(fields.soundName);
            if (clipToPlay != null)
            {
                targetAudioSource.clip = clipToPlay;
                targetAudioSource.loop = fields.isLoop;
                targetAudioSource.Play();

                if (fadeDuration > 0) // [1단계 수정] duration 변수 사용
                {
                    targetAudioSource.volume = 0;
                    sequence.Append(targetAudioSource.DOFade(fields.volume, fadeDuration));
                }
                else
                {
                    targetAudioSource.volume = fields.volume;
                }
            }
        }
        else if (fields.controlType == "Stop")
        {
            if (fadeDuration > 0) // [1단계 수정] duration 변수 사용
            {
                sequence.Append(targetAudioSource.DOFade(0, fadeDuration));
                sequence.OnComplete(() => {
                    targetAudioSource.Stop();
                    targetAudioSource.clip = null;
                });
            }
            else
            {
                targetAudioSource.Stop();
                targetAudioSource.clip = null;
            }
        }

        // [1단계 수정] FF 중이 아닐 때만 대기 및 스킵 처리
        if (sequence.Duration() > 0 && !isFastForwarding)
        {
            // SoundNodeFields에 skippable이 있다면 여기서 처리 (현재 코드에는 skippable 필드가 없어 보임)
            // 만약 skippable 필드가 있다면 아래 주석 해제하여 사용
            /*
            while (sequence.IsActive() && sequence.IsPlaying())
            {
                if (fields.skippable && Input.GetMouseButtonDown(0))
                {
                    sequence.Complete();
                    break;
                }
                yield return null;
            }
            */
            yield return sequence.WaitForCompletion();
        }
    }
}