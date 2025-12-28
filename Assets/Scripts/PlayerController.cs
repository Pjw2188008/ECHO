using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // --- ★ [추가] 5. 사운드 설정 ---
    [Header("5. 사운드 설정")]
    public AudioSource footstepSource; // 발소리를 재생할 오디오 소스 (인스펙터에서 연결)
    public AudioClip walkSound;        // 걷기 소리 파일
    public AudioClip runSound;         // 뛰기 소리 파일
    [Range(0, 1)] public float walkVolume = 0.5f;
    [Range(0, 1)] public float runVolume = 1.0f;

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

        // 발소리 오디오 소스 초기 설정
        if (footstepSource != null)
        {
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
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

        // 앉기 판단 로직
        bool manualCrouch = Input.GetKey(KeyCode.C);
        bool hasCeiling = CheckCeiling();
        bool needAutoCrouch = CheckFrontObstacle(moveInputDirection);
        isCrouching = manualCrouch || hasCeiling || needAutoCrouch;

        // 3. 속도 결정 및 상태 확인
        bool isMoving = moveInputDirection.magnitude > 0.1f;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && isMoving;

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

        // --- ★ [추가] 5. 발소리 재생 실행 ---
        HandleFootsteps(isMoving, isRunning);
    }

    // --- ★ [추가] 발소리 제어 함수 ---
    void HandleFootsteps(bool isMoving, bool isRunning)
    {
        if (footstepSource == null) return;

        // [수정된 조건] 땅에 있고 + 움직이고 있고 + "앉아있지 않을 때"만 소리 재생
        if (controller.isGrounded && isMoving && !isCrouching)
        {
            AudioClip targetClip = isRunning ? runSound : walkSound;
            float targetVolume = isRunning ? runVolume : walkVolume;

            if (footstepSource.clip != targetClip || !footstepSource.isPlaying)
            {
                footstepSource.clip = targetClip;
                footstepSource.Play();
            }
            footstepSource.volume = targetVolume;
        }
        else
        {
            // 멈췄거나, 공중에 떴거나, "앉아있다면" 소리 즉시 정지
            if (footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }
    }

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