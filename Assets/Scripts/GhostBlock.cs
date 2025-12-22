using UnityEngine;

public class GhostBlock : MonoBehaviour
{
    [Header("👻 설정")]
    public float revealDistance = 3.0f; // 이 거리 안으로 오면 보이기 시작
    [Range(0f, 1f)] public float maxOpacity = 0.5f; // 최대 선명도 (1이면 완전 불투명, 0.5면 반투명)

    private Transform player;
    private MeshRenderer meshRenderer;
    private Material mat;
    private Color originalColor;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        // 중요: 재질의 복사본을 가져와서 이 블록만 색이 변하게 함
        mat = meshRenderer.material;
        originalColor = mat.color;

        // 시작할 때는 완전히 투명하게(Alpha 0) 설정
        Color startColor = originalColor;
        startColor.a = 0f;
        mat.color = startColor;

        // 플레이어 찾기
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        // 플레이어와의 거리 계산
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= revealDistance)
        {
            // 거리 비율 계산 (가까울수록 1, 멀수록 0)
            float ratio = 1 - (distance / revealDistance);

            // 투명도 설정 (0 ~ maxOpacity 사이)
            float alpha = Mathf.Clamp01(ratio) * maxOpacity;

            // 색상 업데이트
            Color newColor = originalColor;
            newColor.a = alpha;
            mat.color = newColor;
        }
        else
        {
            // 거리가 멀어지면 다시 완전히 투명하게
            if (mat.color.a > 0)
            {
                Color cleanColor = originalColor;
                cleanColor.a = 0f;
                mat.color = cleanColor;
            }
        }
    }
}