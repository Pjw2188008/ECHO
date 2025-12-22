using UnityEngine;
using UnityEngine.SceneManagement; // 나중에 재시작 기능을 위해 필요

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 싱글톤 (어디서든 부를 수 있게)

    public bool isGameOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ★ 누구든(몬스터든 함정이든) 이 함수를 부르면 게임 오버!
    public void TriggerGameOver(string cause)
    {
        if (isGameOver) return; // 이미 죽었으면 무시

        isGameOver = true;

        // 1. 시간 멈추기
        Time.timeScale = 0;

        // 2. 마우스 커서 다시 보이게 하기 (UI 클릭을 위해)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. 로그 출력 (나중에 여기에 '게임오버 UI' 띄우는 코드 넣으면 됨)
        Debug.LogError($"💀 GAME OVER!! 사망 원인: {cause}");
    }

    // (참고) 재시작 기능 예시
    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}