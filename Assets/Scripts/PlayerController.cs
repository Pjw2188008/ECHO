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

    [Header("2. 시야 및 키 설정 (요청하신 값 적용됨)")]
    public float mouseSensitivity = 2f;

    // 요청하신 값: 서 있을 때 눈높이 0.6, 앉을 때 0.3
    public float standEyeLevel = 0.6f;
    public float crouchEyeLevel = 0.3f;

    // 눈높이에 맞춰서 몸통(캡슐) 크기도 비율에 맞게 조정 (눈보다 조금 더 크게)
    public float standHeight = 1.0f;       // 서 있을 때 키 (눈높이 0.6 + 머리 여유분)
    public float crouchHeight = 0.5f;      // 앉았을 때 키 (눈높이 0.3 + 머리 여유분)

    [Header("3. 자동 앉기 설정")]
    public float obstacleCheckDistance = 1.0f; // 전방 감지 거리
    public LayerMask obstacleLayer;            // 장애물 레이어

    [Header("4. 기타 물리 설정")]
    public float gravity = -20f;
    public float transitionSpeed = 10f;

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

        // 시작 시 값 초기화
        controller.height = standHeight;
        controller.center = Vector3.zero; // 중심점은 항상 0으로 시작

        // 카메라 위치 초기화
        Vector3 camPos = cameraTransform.localPosition;
        camPos.y = standEyeLevel;
        cameraTransform.localPosition = camPos;

        if (obstacleLayer == 0) obstacleLayer = ~0;
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

        // --- 앉기 판단 로직 ---
        bool manualCrouch = Input.GetKey(KeyCode.C);
        bool hasCeiling = CheckCeiling();
        bool needAutoCrouch = CheckFrontObstacle(moveInputDirection);

        bool isCrouching = manualCrouch || hasCeiling || needAutoCrouch;

        // 3. 속도 및 이동
        float currentSpeed = walkSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (Input.GetKey(KeyCode.LeftShift)) currentSpeed = runSpeed;

        if (controller.isGrounded && moveDirection.y < 0) moveDirection.y = -2f;

        Vector3 move = moveInputDirection * currentSpeed;
        float currentY = moveDirection.y;
        moveDirection = move;
        moveDirection.y = currentY + (gravity * Time.deltaTime);

        controller.Move(moveDirection * Time.deltaTime);

        // 4. 높이 조절
        AdjustHeight(isCrouching);
    }

    // 자동 앉기 체크 (낮은 구멍 통과용)
    bool CheckFrontObstacle(Vector3 moveDir)
    {
        if (moveDir.magnitude < 0.1f) return false;

        Vector3 forward = moveDir.normalized;

        // 이마 높이 체크 (서 있을 때 키 기준 조금 아래)
        Vector3 headOrigin = transform.position + Vector3.up * (standHeight - 0.1f);
        bool headBlocked = Physics.Raycast(headOrigin, forward, obstacleCheckDistance, obstacleLayer);

        // 무릎 높이 체크 (앉았을 때 키의 절반)
        Vector3 kneeOrigin = transform.position + Vector3.up * (crouchHeight * 0.5f);
        bool kneeClear = !Physics.Raycast(kneeOrigin, forward, obstacleCheckDistance, obstacleLayer);

        return headBlocked && kneeClear;
    }

    // 천장 체크 (일어날 수 있는지 확인) //코드 확인용도 //111112222
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

        // 키가 줄어든 만큼 중심점을 내려서 발 위치 고정
        float heightDelta = standHeight - controller.height;
        controller.center = new Vector3(0, -heightDelta / 2f, 0);

        Vector3 camPos = cameraTransform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * transitionSpeed);
        cameraTransform.localPosition = camPos;
    }
}