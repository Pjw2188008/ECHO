using UnityEngine;
using System.Collections.Generic;

public class IntersectionTrigger : MonoBehaviour
{
    [Header("??? 갈 수 있는 길들 (체크하세요)")]
    public bool hasLeft = true;
    public bool hasRight = true;
    public bool hasCenter = false;

    [Header("? 진짜 정답 방향")]
    public PathDirection correctDirection;

    [Header("? 설정")]
    // 기본값을 true로 설정해서, 별도 설정 없으면 무조건 한 번만 작동하게 함
    public bool oneTimeUse = true;

    private void OnTriggerEnter(Collider other)
    {
        // 태그 확인
        if (other.CompareTag("Player"))
        {
            // 플레이어의 부모 객체에서 PathWhisperer 스크립트 찾기
            PathWhisperer whisperer = other.GetComponentInParent<PathWhisperer>();

            if (whisperer != null)
            {
                // 1. 갈 수 있는 길 목록 만들기
                List<PathDirection> available = new List<PathDirection>();
                if (hasLeft) available.Add(PathDirection.Left);
                if (hasRight) available.Add(PathDirection.Right);
                if (hasCenter) available.Add(PathDirection.Center);

                // 2. 플레이어에게 정보 전달
                whisperer.OnEnterIntersection(available, correctDirection);

                // 3. ★ 핵심: 한 번 작동했으면 이 트리거 오브젝트를 꺼버림 (다시 작동 안 함)
                if (oneTimeUse)
                {
                    Debug.Log($"?? [Trigger] {gameObject.name} 사용 완료되어 비활성화됨.");
                    gameObject.SetActive(false);
                }
            }
        }
    }

    // 에디터에서 박스 색깔 보여주기
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f); // 반투명 초록색
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}