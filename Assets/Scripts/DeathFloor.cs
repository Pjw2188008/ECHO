using UnityEngine;

public class DeathFloor : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 플레이어가 떨어지면
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver("추락사: 보이지 않는 심연으로 떨어졌습니다.");
            }
        }
        // 만약 몬스터나 다른 물건이 떨어지면?
        else
        {
            // 필요하다면 제거 (성능 최적화)
            // Destroy(other.gameObject); 
        }
    }
}