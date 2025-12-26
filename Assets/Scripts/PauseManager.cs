using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI 설정")]
    public GameObject pauseMenuPanel; // 일시정지 패널

    [Header("카메라 설정 (중요!)")]
    // 1인칭 시점을 담당하는 스크립트나 오브젝트를 여기에 넣어야 합니다.
    // 보통 'FirstPersonController' 또는 'Camera'에 붙은 스크립트입니다.
    public MonoBehaviour cameraControllerScript;

    bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // [중요] 게임으로 돌아가면 마우스를 다시 가두고 숨깁니다.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 카메라 움직임 스크립트를 다시 켭니다.
        if (cameraControllerScript != null)
            cameraControllerScript.enabled = true;
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // [중요] 일시정지 때는 마우스를 풀어서 보이게 합니다.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 카메라가 마우스 따라다니지 않게 스크립트를 껍니다.
        if (cameraControllerScript != null)
            cameraControllerScript.enabled = false;
    }
   
    public void GoToMainMenu()
    {
        // 1. 중요! 멈춰있던 시간을 다시 흐르게 만듭니다.
        // 이걸 안 하면 메인 화면 가서도 게임이 멈춰있을 수 있습니다.
        Time.timeScale = 1f;

        // 2. 씬을 이동합니다.
        // "StartScene" 부분에 아까 만드신 시작 화면 씬의 이름을 정확히 적으세요.
        SceneManager.LoadScene("StartScene");
    }

}
