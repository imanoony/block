using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource[] bgmSources = new AudioSource[4];

    [Header("BGM Clips")]
    public AudioClip[] bgmClips = new AudioClip[4];
    private bool[] bgmPlayed = new bool[4] { false, false, false, false };

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float[] bgmVolumes = new float[4] { 1f, 1f, 1f, 1f };
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    public void Initialize()
    {
        for (int i = 0; i < bgmSources.Length; i++)
        {
            bgmSources[i].clip = bgmClips[i];
            bgmSources[i].loop = true;
            bgmSources[i].volume = 0f;
            bgmSources[i].Play();
        }

        PlayBGM(0);
    }

    // BGM 재생
    public void PlayBGM(int number)
    {
        if (number < 0 || number >= bgmSources.Length) return;
        if (bgmPlayed[number]) return;

        if (volumnCoroutines[number] != null) StopCoroutine(volumnCoroutines[number]);
        volumnCoroutines[number] = StartCoroutine(VolumnFadeBGM(number, bgmVolumes[number]));
        bgmPlayed[number] = true;
    }

    // BGM 중단
    public void StopBGM(int number)
    {
        if (number < 0 || number >= bgmSources.Length) return;
        if (!bgmPlayed[number]) return;

        if (volumnCoroutines[number] != null) StopCoroutine(volumnCoroutines[number]);
        volumnCoroutines[number] = StartCoroutine(VolumnFadeBGM(number, 0f));
        bgmPlayed[number] = false;
    }

    private float volumnTime = 0.5f;
    private Coroutine[] volumnCoroutines = new Coroutine[4] { null, null, null, null };
    private IEnumerator VolumnFadeBGM(int number, float target)
    {
        float start = bgmSources[number].volume;

        float elapsed = 0f;
        while (elapsed < volumnTime)
        {
            float t = elapsed / volumnTime;
            bgmSources[number].volume = Mathf.Lerp(start, target, t);

            elapsed += Time.smoothDeltaTime;
            yield return null;
        }

        bgmSources[number].volume = target;
        volumnCoroutines[number] = null;
        yield break;
    }

    // SFX 재생
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}