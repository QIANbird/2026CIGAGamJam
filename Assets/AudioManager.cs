using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("背景音乐")]
    public AudioSource bgmSource;
    public List<AudioClip> bgmList;
    private int currentBgmIndex;

    [Header("音量")]
    [Range(0, 1)] public float bgmVolume = 0.6f;
    [Range(0, 1)] public float clickVolume = 1f;

    [Header("全局统一点击音效")]
    public AudioClip globalClick;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
        }
        bgmSource.volume = bgmVolume;
    }

    #region 点击音效接口
    // 这里就是 ObjectClickAudio 需要的 PlayClick 方法
    public void PlayClick()
    {
        if (globalClick == null || Camera.main == null) return;
        AudioSource.PlayClipAtPoint(globalClick, Camera.main.transform.position, clickVolume);
    }

    public void PlayCustomClick(AudioClip clip)
    {
        if (clip == null || Camera.main == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, clickVolume);
    }
    #endregion

    #region BGM控制
    public void PlayBGM()
    {
        if (bgmList.Count == 0) return;
        bgmSource.clip = bgmList[currentBgmIndex];
        bgmSource.Play();
    }

    public void PauseBGM() => bgmSource.Pause();
    public void ResumeBGM() => bgmSource.UnPause();
    public void StopBGM() => bgmSource.Stop();

    public void NextBGM()
    {
        if (bgmList.Count == 0) return;
        currentBgmIndex = (currentBgmIndex + 1) % bgmList.Count;
        PlayBGM();
    }

    public void SetBGMVolume(float val)
    {
        bgmVolume = Mathf.Clamp01(val);
        bgmSource.volume = bgmVolume;
    }
    #endregion
}