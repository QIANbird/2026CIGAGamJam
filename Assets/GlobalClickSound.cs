using UnityEngine;

public class GlobalClickSound : MonoBehaviour
{
    [Header("点击音效")]
    public AudioClip clickAudio;
    private AudioSource audioSource;

    void Start()
    {
        // 自动挂载AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // 检测鼠标左键 / 手机触摸点击
        if (Input.GetMouseButtonDown(0))
        {
            PlaySound();
        }
    }

    void PlaySound()
    {
        if (clickAudio != null)
        {
            // PlayOneShot 支持音效叠加，多次点击不会打断
            audioSource.PlayOneShot(clickAudio);
        }
        else
        {
            Debug.LogWarning("请给脚本赋值音效Clip！");
        }
    }
}