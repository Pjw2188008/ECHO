using UnityEngine;

public class DeadlyTrap : MonoBehaviour
{
    [Header("⚙️ 함정 설정")]
    [Tooltip("함정이 회전하나요? (톱날, 환풍구 등)")]
    public bool isRotating = true;
    public float rotationSpeed = 200f; // 회전 속도
    public Vector3 rotationAxis = Vector3.up; // 회전 축 (Y축)

    [Header("💀 사망 메시지")]
    public string deathMessage = "갈려나간 시체가 되었습니다.";

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
        // 플레이어 태그를 가진 놈이 닿으면 사망!
        if (other.CompareTag("Player"))
        {
            Debug.Log("🩸 함정에 닿았습니다!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver(deathMessage);
            }
        }
    }
}