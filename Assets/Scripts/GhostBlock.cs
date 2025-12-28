using UnityEngine;

public class GhostBlock : MonoBehaviour
{
    [Header("👻 설정")]
    public float revealDistance = 3.0f;
    [Range(0f, 1f)] public float maxOpacity = 0.2f; // ★ 텍스처가 너무 잘 보이면 이 값을 0.2로 낮추세요!

    private Transform player;
    private MeshRenderer meshRenderer;
    private Material mat;
    private Color originalColor;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            // 재질 복사 (이 블록만 개별적으로 색이 변함)
            mat = meshRenderer.material;

            // 원래 텍스처 색상 저장
            originalColor = mat.color;

            // 시작 시 완전 투명하게
            Color startColor = originalColor;
            startColor.a = 0f;
            mat.color = startColor;
        }

        // ★ 플레이어 찾기 개선 (태그 없으면 카메라도 찾음)
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else if (Camera.main != null) player = Camera.main.transform;
        }
    }

    void Update()
    {
        if (player == null || mat == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= revealDistance)
        {
            // 거리 비율 (0 ~ 1)
            float ratio = 1.0f - (distance / revealDistance);

            // 투명도 조절 (부드럽게 보정)
            // Mathf.Pow를 쓰면 선형보다 더 자연스럽게(유령처럼) 나타납니다.
            // ratio * ratio -> 거리가 조금만 멀어져도 급격히 흐려짐
            float alpha = Mathf.Clamp01(ratio) * maxOpacity;

            // 색상 적용
            Color newColor = originalColor;
            newColor.a = alpha;
            mat.color = newColor;
        }
        else
        {
            // 최적화: 이미 투명하면 색상 변경 안 함
            if (mat.color.a > 0.01f)
            {
                Color cleanColor = originalColor;
                cleanColor.a = 0f;
                mat.color = cleanColor;
            }
        }
    }
}