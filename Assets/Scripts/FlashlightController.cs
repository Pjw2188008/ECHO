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

    // 회복 모드인지 확인
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
        isRecharging = false;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        // 레이어 마스크가 설정 안 되어 있으면 모든 레이어 충돌
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
            isRecharging = false;

            if (currentBattery > 0)
            {
                currentBattery -= Time.deltaTime * drainRate;
                CheckLightHit(); // ★ 여기서 몬스터 체크
            }
            else
            {
                // 배터리 0 도달 -> 방전
                currentBattery = 0;
                isDepleted = true;
                isRecharging = true;
                TurnOff();
                Debug.Log("배터리 방전! 시스템 재부팅 시작...");
            }
        }
        else
        {
            // [꺼짐] 충전 로직
            if (isRecharging)
            {
                if (currentBattery < maxBattery)
                {
                    currentBattery += Time.deltaTime * rechargeRate;
                }
                else
                {
                    currentBattery = maxBattery;
                    isRecharging = false;
                    Debug.Log("배터리 완충됨.");
                }

                if (isDepleted && currentBattery >= recoveryThreshold)
                {
                    isDepleted = false;
                    Debug.Log("손전등 사용 가능! (계속 충전 중...)");
                }
            }
        }
    }

    // ★★★ 여기가 수정된 핵심 부분입니다 ★★★
    void CheckLightHit()
    {
        RaycastHit hit;

        // 디버그: 씬 뷰에서 초록색 선 확인 가능
        Debug.DrawRay(cameraTransform.position, cameraTransform.forward * lightRange, Color.green);

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, lightRange, targetLayer))
        {
            // 1. ShaderAI인지 확인
            ShaderAI shaderMonster = hit.collider.GetComponent<ShaderAI>();
            if (shaderMonster != null)
            {
                shaderMonster.HitByLight();
                // Debug.Log("🔦 ShaderAI 몬스터가 빛을 받았습니다!");
            }

            // 2. ListenerAI인지 확인 (기존 코드)
            ListenerAI listenerMonster = hit.collider.GetComponent<ListenerAI>();
            if (listenerMonster != null)
            {
                // 주의: ListenerAI 스크립트에도 public void HitByLight() 함수가 있어야 합니다.
                listenerMonster.HitByLight();
                // Debug.Log("🔦 ListenerAI 몬스터가 빛을 받았습니다!");
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