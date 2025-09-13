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
    public float bgmVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private double dspStartTime;

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

        bgmSources[number].volume = 1f;
        bgmPlayed[number] = true;
    }

    // BGM 중단
    public void StopBGM(int number)
    {
        if (number < 0 || number >= bgmSources.Length) return;
        if (!bgmPlayed[number]) return;

        bgmSources[number].volume = 0f;
        bgmPlayed[number] = false;
    }

    // SFX 재생
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}