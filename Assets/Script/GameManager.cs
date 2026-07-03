using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("按下ESC执行的事件")]
    public UnityEvent OnPressEsc;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 检测ESC按键按下瞬间
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPressEsc?.Invoke();
        }
    }
    // 切换到 MainScene
    public void LoadMainScene()
    {
        // 参数填场景文件名，必须和Build里的名字完全一致
        SceneManager.LoadScene("MainScene");
    }
    

    // 切换到 MainScene
    public void LoadStartScene()
    {
        // 参数填场景文件名，必须和Build里的名字完全一致
        SceneManager.LoadScene("StartScene");
    }

    
}
