using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("1. 속도 설정")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float crouchSpeed = 3f;

    [Header("2. 시야 및 키 설정")]
    public float mouseSensitivity = 2f;
    public float standEyeLevel = 0.6f;
    public float crouchEyeLevel = 0.3f;
    public float standHeight = 1.0f;
    public float crouchHeight = 0.5f;

    [Header("3. 자동 앉기 설정")]
    public float obstacleCheckDistance = 1.0f;
    public LayerMask obstacleLayer;

    [Header("4. 기타 물리 설정")]
    public float gravity = -20f;
    public float transitionSpeed = 10f;

    [Header("5. 사운드 설정")]
    public AudioSource footstepSource;
    public AudioClip walkSound;
    public AudioClip runSound;
    [Range(0, 1)] public float walkVolume = 0.5f;
    [Range(0, 1)] public float runVolume = 1.0f;

    [Header("6. 스테미나 설정")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 10f;
    public Slider staminaSlider;

    // --- ★ [추가] 탈진 시스템 설정 ---
    [Header("7. 탈진(지침) 설정")]
    [Range(0f, 1f)] public float recoveryThreshold = 0.3f; // 30% 회복될 때까지 달리기 불가
    private bool isExhausted = false; // 현재 지쳐서 못 뛰는 상태인가?

    [Header("상태 확인")]
    public bool isCrouching = false;

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 moveDirection;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        controller.height = standHeight;
        controller.center = Vector3.zero;

        Vector3 camPos = cameraTransform.localPosition;
        camPos.y = standEyeLevel;
        cameraTransform.localPosition = camPos;

        if (obstacleLayer == 0) obstacleLayer = ~0;

        if (footstepSource != null)
        {
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
        }

        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Update()
    {
        // 1. 시선 처리
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // 2. 이동 입력
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 moveInputDirection = transform.right * x + transform.forward * z;

        // 앉기 판단
        bool manualCrouch = Input.GetKey(KeyCode.C);
        bool hasCeiling = CheckCeiling();
        bool needAutoCrouch = CheckFrontObstacle(moveInputDirection);
        isCrouching = manualCrouch || hasCeiling || needAutoCrouch;

        // 3. 움직임 상태 확인
        bool isMoving = moveInputDirection.magnitude > 0.1f;

        // --- ★ [핵심 로직 변경] 탈진 상태 관리 ---
        // 스테미나가 0 이하가 되면 탈진 시작
        if (currentStamina <= 0)
        {
            isExhausted = true;
        }
        // 탈진 상태인데, 스테미나가 N% (예: 30%) 이상 차오르면 탈진 해제
        else if (isExhausted && currentStamina >= maxStamina * recoveryThreshold)
        {
            isExhausted = false;
        }

        // 달리기 조건: Shift 누름 + 앉지 않음 + 움직임 + ★ 탈진 상태가 아님(!isExhausted)
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && isMoving && !isExhausted;

        // 속도 적용
        float currentSpeed = walkSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isRunning) currentSpeed = runSpeed;

        if (controller.isGrounded && moveDirection.y < 0) moveDirection.y = -2f;

        Vector3 move = moveInputDirection * currentSpeed;
        float currentY = moveDirection.y;
        moveDirection = move;
        moveDirection.y = currentY + (gravity * Time.deltaTime);

        controller.Move(moveDirection * Time.deltaTime);

        // 4. 높이 조절
        AdjustHeight(isCrouching);

        // 5. 기능 실행
        HandleFootsteps(isMoving, isRunning);
        HandleStamina(isRunning);
    }

    void HandleStamina(bool isRunning)
    {
        if (isRunning)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
            }
        }

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;

            // (선택사항) 탈진 상태일 때 슬라이더 색상을 빨간색으로 바꿔 시각적 피드백 주기
            Image fillImage = staminaSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = isExhausted ? Color.red : Color.green; // 혹은 원래 색상
            }
        }
    }

    void HandleFootsteps(bool isMoving, bool isRunning)
    {
        if (footstepSource == null) return;

        // 땅에 있고, 움직이고, 앉지 않았을 때
        if (controller.isGrounded && isMoving && !isCrouching)
        {
            // ★ 탈진 상태(isExhausted)가 되면 isRunning이 false가 되므로
            // 자동으로 walkSound가 선택되어 재생됩니다.
            AudioClip targetClip = isRunning ? runSound : walkSound;
            float targetVolume = isRunning ? runVolume : walkVolume;

            // 클립이 다르거나 재생 중이 아니면 새로 재생
            if (footstepSource.clip != targetClip || !footstepSource.isPlaying)
            {
                footstepSource.clip = targetClip;
                footstepSource.Play();
            }
            footstepSource.volume = targetVolume;
        }
        else
        {
            // 멈추거나 앉거나 공중에 뜨면 소리 끔
            if (footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }
    }

    // (아래 함수들은 기존과 동일)
    bool CheckFrontObstacle(Vector3 moveDir)
    {
        if (moveDir.magnitude < 0.1f) return false;
        Vector3 forward = moveDir.normalized;
        Vector3 headOrigin = transform.position + Vector3.up * (standHeight - 0.1f);
        bool headBlocked = Physics.Raycast(headOrigin, forward, obstacleCheckDistance, obstacleLayer);
        Vector3 kneeOrigin = transform.position + Vector3.up * (crouchHeight * 0.5f);
        bool kneeClear = !Physics.Raycast(kneeOrigin, forward, obstacleCheckDistance, obstacleLayer);
        return headBlocked && kneeClear;
    }

    bool CheckCeiling()
    {
        float checkHeight = standHeight + 0.1f;
        return Physics.Raycast(transform.position, Vector3.up, checkHeight, obstacleLayer);
    }

    void AdjustHeight(bool isCrouch)
    {
        float targetHeight = isCrouch ? crouchHeight : standHeight;
        float targetCamY = isCrouch ? crouchEyeLevel : standEyeLevel;

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * transitionSpeed);

        float heightDelta = standHeight - controller.height;
        controller.center = new Vector3(0, -heightDelta / 2f, 0);

        Vector3 camPos = cameraTransform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * transitionSpeed);
        cameraTransform.localPosition = camPos;
    }
}