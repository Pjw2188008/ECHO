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

    [Header("상태 확인")]
    public bool isCrouching = false; // ★ 몬스터가 확인할 변수

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

        // --- 앉기 판단 로직 (수정됨) ---
        bool manualCrouch = Input.GetKey(KeyCode.C);
        bool hasCeiling = CheckCeiling();
        bool needAutoCrouch = CheckFrontObstacle(moveInputDirection);

        // ★ [수정] 앞에 'bool'을 지웠습니다! 이제 인스펙터 변수에 값이 들어갑니다.
        isCrouching = manualCrouch || hasCeiling || needAutoCrouch;

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

        // ★ [삭제] 맨 밑에 있던 중복된 C키 확인 로직은 지웠습니다.
        // 위에서 이미 manualCrouch로 계산했기 때문에 필요 없습니다.
        // 오히려 천장이 있어도 C키를 떼면 일어서버리는 버그를 유발합니다.
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