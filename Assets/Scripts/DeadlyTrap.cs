using UnityEngine;
using UnityEngine.SceneManagement; // ★ 씬 이동을 위해 필수!

public class DeadlyTrap : MonoBehaviour
{
    [Header("⚙️ 함정 설정")]
    [Tooltip("함정이 회전하나요? (톱날, 환풍구 등)")]
    public bool isRotating = true;
    public float rotationSpeed = 200f; // 회전 속도
    public Vector3 rotationAxis = Vector3.up; // 회전 축 (Y축)

    [Header("💀 게임오버 설정 (필수 연결)")]
    public GameObject gameOverUI; // ★ 게임오버 패널 연결
    public MonoBehaviour playerCameraScript; // ★ 플레이어 카메라 스크립트 연결

    // 내부 변수
    private bool isGameOver = false;

    void Update()
    {
        // 톱날처럼 빙글빙글 돌리기
        if (isRotating)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }
    }

    // ★ 물체가 닿았을 때 실행되는 함수
    void OnTriggerEnter(Collider other)
    {
        if (isGameOver) return; // 이미 죽었으면 무시

        // 플레이어 태그를 가진 놈이 닿으면 사망!
        if (other.CompareTag("Player"))
        {
            Debug.Log("🩸 함정에 닿았습니다!");
            GameOver(); // 게임오버 실행
        }
    }

    // ▼▼▼ [추가된 게임오버 함수] ▼▼▼
    void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0; // 1. 시간 멈춤

        // 2. UI 켜기
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        // 3. 카메라 잠금 (시점 고정)
        if (playerCameraScript != null)
        {
            playerCameraScript.enabled = false;
        }

        // 4. 마우스 커서 보이게 풀기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ▼▼▼ [추가된 타이틀 이동 함수] ▼▼▼
    // UI 버튼에 연결할 함수입니다.
    public void GoToTitle()
    {
        Time.timeScale = 1f; // 시간 다시 흐르게 설정
        SceneManager.LoadScene("StartScene"); // 시작 화면으로 이동
    }
}