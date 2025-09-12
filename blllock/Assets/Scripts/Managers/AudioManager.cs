using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource[] bgmSources = new AudioSource[4];

    [Header("BGM Clips")]
    public AudioClip[] bgmClips = new AudioClip[4];

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
        }
    }

    // BGM 재생
    public void PlayBGM(int number)
    {
        if (number < 0 || number >= bgmSources.Length) return;
        if (number == 0) dspStartTime = AudioSettings.dspTime + 0.1f;
        bgmSources[number].PlayScheduled(dspStartTime);
    }

    // SFX 재생
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}