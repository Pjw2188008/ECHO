
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동 필수

public class EndingTrigger : MonoBehaviour
{
    [Header("?? 엔딩 UI 연결")]
    public GameObject endingUI; // 엔딩 패널

    [Header("?? 카메라 잠금 해제")]
    public MonoBehaviour playerCameraScript; // 플레이어 시점 스크립트

    private bool isEnding = false;

    // 물체(플레이어)가 이 블럭을 통과할 때 실행됨
    void OnTriggerEnter(Collider other)
    {
        // 1. 이미 엔딩 중이면 무시
        if (isEnding) return;

        // 2. 닿은 게 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            GameClear();
        }
    }

    void GameClear()
    {
        isEnding = true;
        Debug.Log("?? 게임 클리어!");

        // 1. 시간 멈춤
        Time.timeScale = 0;

        // 2. 엔딩 UI 켜기
        if (endingUI != null)
        {
            endingUI.SetActive(true);
        }

        // 3. 카메라 움직임 끄기 (시점 고정)
        if (playerCameraScript != null)
        {
            playerCameraScript.enabled = false;
        }

        // 4. 마우스 커서 풀기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 버튼에 연결할 함수
    public void GoToTitle()
    {
        Time.timeScale = 1f; // 시간 다시 흐르게
        SceneManager.LoadScene("StartScene"); // 시작 화면으로
    }
}
