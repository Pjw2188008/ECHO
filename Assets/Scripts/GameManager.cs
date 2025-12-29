using UnityEngine;
using UnityEngine.SceneManagement; // 나중에 재시작 기능을 위해 필요

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 싱글톤

    [Header("💀 게임오버 UI 설정")]
    public GameObject gameOverUI; // ★ 게임오버 패널 연결
    public MonoBehaviour playerCameraScript; // ★ 플레이어 카메라 스크립트 연결

    public bool isGameOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ★ 누구든 이 함수를 부르면 게임 오버 UI가 뜹니다!
    public void TriggerGameOver(string cause)
    {
        if (isGameOver) return; // 이미 죽었으면 무시

        isGameOver = true;

        // 1. 시간 멈추기
        Time.timeScale = 0;

        // 2. 게임오버 UI 켜기 (★ 추가됨)
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        // 3. 카메라 잠금 (★ 추가됨 - 시점 고정)
        if (playerCameraScript != null)
        {
            playerCameraScript.enabled = false;
        }

        // 4. 마우스 커서 다시 보이게 하기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.LogError($"💀 GAME OVER!! 사망 원인: {cause}");
    }

    // ★ UI 버튼에 연결할 함수 (시작 화면으로 이동)
    public void GoToTitle()
    {
        Time.timeScale = 1f; // 시간 다시 흐르게 설정
        SceneManager.LoadScene("StartScene"); // 시작 씬 이름 확인!
    }

    // (참고) 재시작 기능이 필요하다면 사용
    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}