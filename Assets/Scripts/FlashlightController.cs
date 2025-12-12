using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("기본 설정")]
    public Light flashlight;
    public float maxBattery = 100f;
    public float currentBattery;

    [Header("속도 설정")]
    public float drainRate = 5.0f;
    public float rechargeRate = 5.0f;

    [Header("방전 설정")]
    public float recoveryThreshold = 30f; // 다시 켜지는 기준
    public bool isDepleted = false;       // 방전 상태 (사용 불가) 플래그

    [Header("상태 (자동 확인용)")]
    public bool isLightOn = false;


    // ★ 추가된 변수: 회복 모드인지 확인
    // (0이 되면 true가 되고, 100이 되거나 불을 켜면 false가 됨)
    private bool isRecharging = false;

    [Header("🔦 빛 공격 설정")]
    public Transform cameraTransform;
    public float lightRange = 15.0f;
    public LayerMask targetLayer;

    void Start()
    {
        currentBattery = maxBattery;
        flashlight.enabled = false;
        isLightOn = false;
        isRecharging = false; // 초기화

        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        if (targetLayer == 0) targetLayer = ~0;
    }

    void Update()
    {
        // 1. F키 입력
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isLightOn)
            {
                TurnOff();
            }
            else
            {
                if (isDepleted)
                {
                    Debug.Log($"방전됨! 최소 {recoveryThreshold}%까지 기다려야 합니다.");
                }
                else
                {
                    TurnOn();
                }
            }
        }

        // 2. 배터리 로직
        if (isLightOn)
        {
            // [켜짐] 배터리 소모
            // 불을 켰으므로 회복 모드는 강제로 끕니다.
            isRecharging = false;

            if (currentBattery > 0)
            {
                currentBattery -= Time.deltaTime * drainRate;
                CheckLightHit();
            }
            else
            {
                // 배터리 0 도달 -> 방전 & 회복 모드 시작
                currentBattery = 0;
                isDepleted = true;    // 사용 불가 걸기
                isRecharging = true;  // ★ 회복 모드 ON
                TurnOff();
                Debug.Log("배터리 방전! 시스템 재부팅 시작...");
            }
        }
        else
        {
            // [꺼짐]
            // ★ 수정됨: 0을 찍어서 '회복 모드'가 켜진 상태여야만 회복합니다.
            if (isRecharging)
            {
                if (currentBattery < maxBattery)
                {
                    currentBattery += Time.deltaTime * rechargeRate;
                }
                else
                {
                    // 100% 도달하면 회복 모드 종료
                    currentBattery = maxBattery;
                    isRecharging = false;
                    Debug.Log("배터리 완충됨.");
                }

                // 30%가 넘으면 '사용 불가(방전)' 상태만 해제
                // (회복 모드 isRecharging은 끄지 않음 -> 계속 참)
                if (isDepleted && currentBattery >= recoveryThreshold)
                {
                    isDepleted = false;
                    Debug.Log("손전등 사용 가능! (계속 충전 중...)");
                }
            }
            // isRecharging이 false라면(예: 50%에서 껐을 때) 회복하지 않고 그대로 둠
        }
    }

    void CheckLightHit()
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, lightRange, targetLayer))
        {
            ListenerAI monster = hit.collider.GetComponent<ListenerAI>();
            if (monster != null)
            {
                monster.HitByLight();
            }
        }
    }

    public void TurnOn()
    {
        if (isDepleted) return;
        flashlight.enabled = true;
        isLightOn = true;
    }

    public void TurnOff()
    {
        flashlight.enabled = false;
        isLightOn = false;
    }
}