using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void ChangeScene()
    {
      
        SceneManager.LoadScene("MAP1");
    }
    public void ExitGame()
    {
        // 1. 유니티 에디터에서 플레이 중일 때 (테스트용)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 2. 실제 게임(PC, 모바일 등)으로 빌드했을 때
        Application.Quit();
#endif
    }
}
