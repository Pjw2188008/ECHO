using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("기본 설정")]
    public Light flashlight;        // 손전등 라이트 컴포넌트
    public float maxBattery = 100f; // 최대 배터리
    public float currentBattery;    // 현재 배터리

    [Header("속도 설정")]
    public float drainRate = 5.0f;     // 소모 속도 (켜져 있을 때)
    public float rechargeRate = 2.0f;  // 회복 속도 (꺼져 있을 때)

    [Header("방전 설정")]
    public float recoveryThreshold = 30f; // 방전 후 다시 켜지기 위한 최소 배터리량
    public bool isDepleted = false;       // 방전 상태인지 확인하는 플래그

    [Header("상태 (자동 확인용)")]
    public bool isLightOn = false;

    void Start()
    {
        currentBattery = maxBattery;
        flashlight.enabled = false;
        isLightOn = false;
    }

    void Update()
    {
        // 1. F키 입력 (토글)
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isLightOn)
            {
                TurnOff();
            }
            else
            {
                // 켜려고 할 때 방전 상태인지 체크
                if (isDepleted)
                {
                    Debug.Log($"방전됨! {recoveryThreshold}%까지 충전 필요.");
                    // 여기에 "틱틱..." 하는 고장난 소리 효과음을 넣으면 좋습니다.
                }
                else
                {
                    TurnOn();
                }
            }
        }

        // 2. 배터리 로직 (소모 vs 회복)
        if (isLightOn)
        {
            // [켜짐] 배터리 소모
            if (currentBattery > 0)
            {
                currentBattery -= Time.deltaTime * drainRate;
            }
            else
            {
                // 배터리 0 도달 -> 강제 소등 및 방전 상태 돌입
                currentBattery = 0;
                isDepleted = true; // 방전됨! 이제 30 찰 때까지 못 킴
                TurnOff();
                Debug.Log("배터리 완전 방전! (페널티 시작)");
            }
        }
        else
        {
            // [꺼짐] 배터리 자동 회복
            if (currentBattery < maxBattery)
            {
                currentBattery += Time.deltaTime * rechargeRate;
            }

            // 방전 상태 해제 로직
            // 방전 상태였는데, 배터리가 30을 넘으면 다시 켤 수 있게 허용
            if (isDepleted && currentBattery >= recoveryThreshold)
            {
                isDepleted = false;
                Debug.Log("손전등 재사용 가능!");
            }
        }
    }

    public void TurnOn()
    {
        // 방전 상태면 켜지 못함
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
