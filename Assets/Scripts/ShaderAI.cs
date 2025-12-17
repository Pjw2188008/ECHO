using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MeshRenderer))]
public class ShaderAI : MonoBehaviour
{
    [Header("🎯 타겟 설정")]
    public Transform player;

    [Header("👀 감지 범위")]
    [Range(0, 50)] public float warningRadius = 20f;   // 1차: 귓속말 & 경고 범위
    [Range(0, 30)] public float detectionRadius = 8f;  // 2차: 플레이어 주위 맴돌기 (Hover)
    [Range(0, 5)] public float catchRadius = 1.2f;     // 3차: 게임 오버 (접촉)

    [Header("🛸 맴돌기(Hover) 설정")]
    [Tooltip("플레이어 주변 몇 미터 반경에서 맴돌 것인가")]
    public float hoverRadius = 4.0f;
    public float hoverSpeed = 3.5f; // 맴돌 때 속도
    private float hoverTimer = 0f;  // 다음 위치 갱신 타이머

    [Header("💨 이동 속도")]
    public float patrolSpeed = 2.0f; // 평소 배회 속도
    public float chaseSpeed = 7.0f;  // 분노 시 추격 속도 (빠름!)

    [Header("🤬 빛 반응 설정 (분노)")]
    public float requiredLightTime = 2.0f; // 2초 비추면 화남
    public float currentLightExposure = 0f;
    public bool isAggroed = false; // 화난 상태인가?

    [Header("🚶 배회(Patrol) 설정")]
    public Vector2 patrolAreaSize = new Vector2(10f, 10f);
    public Vector3 patrolCenterOffset = Vector3.zero;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    // 내부 변수
    private NavMeshAgent agent;
    private MeshRenderer meshRenderer;
    private Vector3 startPosition;
    private Vector3 globalPatrolCenter;
    private float waitTimer = 0f;

    // 빛 감지 관련
    private bool isHitByLightThisFrame = false;
    private float decayDelayTimer = 0f;
    private float decayCooldown = 0.5f;

    // 게임 상태 및 귓속말
    private bool isPlayerInWarningZone = false;
    private bool isGameOver = false;

    // ★ [추가됨] 귓속말 중복 방지 변수
    private bool hasWhispered = false;

    // 초기 색상 저장용
    private Color originalColor;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();
        startPosition = transform.position;
        globalPatrolCenter = startPosition + patrolCenterOffset;

        // 쉐이더 시작 색상 저장 (없으면 흰색 가정)
        if (meshRenderer.material.HasProperty("_BaseColor"))
            originalColor = meshRenderer.material.GetColor("_BaseColor");
        else
            originalColor = Color.white;

        // 👻 쉐이더 특성: 둥둥 떠다니는 느낌
        agent.baseOffset = 1.2f; // 바닥에서 1.2m 띄움
        agent.acceleration = 15f; // 가속

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        SetRandomDestination();
    }

    void Update()
    {
        if (isGameOver || player == null) return;

        // 0. 거리 계산
        float distance = Vector3.Distance(transform.position, player.position);

        // 💀 [최우선] 게임 오버 체크 (닿으면 사망)
        if (distance <= catchRadius)
        {
            GameOver();
            return;
        }

        // 1. 빛 게이지 관리 (항상 작동)
        HandleLightExposure();

        // 2. 행동 결정
        if (isAggroed)
        {
            // 화난 상태여도 거리가 멀어지면 진정함
            if (distance > warningRadius)
            {
                CalmDown();
            }
            else
            {
                // 여전히 범위 내에 있으면 계속 추격
                ChasePlayer();
                isHitByLightThisFrame = false;
                return;
            }
        }

        // 3. 평소 상태 (화나지 않았거나 진정한 후)
        if (distance <= detectionRadius)
        {
            // [2차 범위] 플레이어 근처를 불규칙하게 맴돌기 (Hover)
            HoverAroundPlayer();
        }
        else if (distance <= warningRadius)
        {
            // [1차 범위] 경고만 하고, 행동은 배회(Patrol) 유지

            // ★ 여기가 귓속말 포인트입니다!
            if (!isPlayerInWarningZone)
            {
                isPlayerInWarningZone = true; // 범위 진입 체크
                Debug.Log("👻 [Shader] 주변 공기가 차가워진다... (Warning Zone 진입)");

                // ★ 아직 귓속말을 안 했다면? -> 실행!
                if (!hasWhispered)
                {
                    hasWhispered = true; // 잠금 (이제 다시는 실행 안 됨)

                    if (WhisperManager.Instance != null)
                    {
                        // 쉐이더 타입으로 귓속말 요청
                        WhisperManager.Instance.PlayMonsterWhisper(MonsterType.Shader);
                    }
                }
            }
            Patrol();
        }
        else
        {
            // [범위 밖] 평화롭게 배회
            isPlayerInWarningZone = false;
            Patrol();
        }

        // 프레임 종료 전 리셋
        isHitByLightThisFrame = false;
    }

    // --- 🛸 맴돌기 (Hover) 로직 : 불규칙한 움직임 ---
    void HoverAroundPlayer()
    {
        agent.speed = hoverSpeed;

        // 목적지에 거의 도착했거나, 일정 시간이 지나면 새로운 위치 갱신
        hoverTimer -= Time.deltaTime;

        if (agent.remainingDistance <= agent.stoppingDistance || hoverTimer <= 0)
        {
            // 플레이어 위치 기준으로 랜덤한 오프셋(반경 내)을 더함
            Vector3 randomOffset = Random.insideUnitSphere * hoverRadius;
            randomOffset.y = 0; // 높이는 NavMesh가 알아서 처리

            Vector3 targetPos = player.position + randomOffset;

            NavMeshHit hit;
            // 유효한 땅인지 확인하고 이동
            if (NavMesh.SamplePosition(targetPos, out hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            // 1~2초마다 위치를 바꿈 (예측 불가능하게)
            hoverTimer = Random.Range(1.0f, 2.5f);
        }
    }

    // --- 💡 빛 & 분노 로직 ---
    public void HitByLight()
    {
        isHitByLightThisFrame = true;
    }

    void HandleLightExposure()
    {
        if (isHitByLightThisFrame)
        {
            currentLightExposure += Time.deltaTime;
            decayDelayTimer = decayCooldown;

            // 시각 효과: 빛 받으면 점점 빨개짐 (경고)
            if (meshRenderer != null)
                meshRenderer.material.color = Color.Lerp(originalColor, new Color(1, 0.5f, 0.5f), currentLightExposure / requiredLightTime);

            // 게이지 꽉 차면 -> 분노 모드 발동!
            if (currentLightExposure >= requiredLightTime && !isAggroed)
            {
                BecomeAggressive();
            }
        }
        else
        {
            // 빛 끊김 (유예 시간 적용)
            if (decayDelayTimer > 0) decayDelayTimer -= Time.deltaTime;
            else
            {
                currentLightExposure -= Time.deltaTime;
                // 화난 상태가 아닐 때만 색 복구 (화나면 계속 빨강 유지)
                if (!isAggroed && meshRenderer != null) meshRenderer.material.color = originalColor;
            }
            if (currentLightExposure < 0) currentLightExposure = 0;
        }
    }

    void BecomeAggressive()
    {
        isAggroed = true;
        Debug.Log("🤬 [Shader] 끄아아악!! (분노: 추격 시작)");
        agent.speed = chaseSpeed;

        // 완전 빨간색
        if (meshRenderer != null) meshRenderer.material.color = Color.red;
    }

    void CalmDown()
    {
        isAggroed = false;
        currentLightExposure = 0f; // 쌓인 게이지 초기화
        agent.speed = patrolSpeed; // 속도 복구

        // 색상 복구
        if (meshRenderer != null) meshRenderer.material.color = originalColor;

        Debug.Log("😓 [Shader] 플레이어를 놓쳤다... 다시 배회한다.");

        // 멍하니 서있지 말고 바로 새로운 배회 장소로 이동
        SetRandomDestination();
    }

    void ChasePlayer()
    {
        // 맹렬히 플레이어 위치로 돌진
        agent.SetDestination(player.position);
    }

    // --- 🚶 배회 (Patrol) ---
    void Patrol()
    {
        agent.speed = patrolSpeed;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                SetRandomDestination();
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
        }
    }

    void SetRandomDestination()
    {
        float randomX = Random.Range(-patrolAreaSize.x / 2f, patrolAreaSize.x / 2f);
        float randomZ = Random.Range(-patrolAreaSize.y / 2f, patrolAreaSize.y / 2f);
        Vector3 targetPos = globalPatrolCenter + new Vector3(randomX, 0f, randomZ);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 5.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        agent.isStopped = true;
        Time.timeScale = 0;
        Debug.LogError("💀 쉐이더에게 닿아 영혼이 잠식되었습니다.");
    }

    void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = new Color(1, 1, 0, 0.2f); // 경고 (노랑)
        Gizmos.DrawWireSphere(transform.position, warningRadius);

        Gizmos.color = new Color(0, 1, 1, 0.3f); // 맴돌기 (하늘색)
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.black; // 사망 (검정)
        Gizmos.DrawWireSphere(transform.position, catchRadius);

        // 배회 구역 (초록 상자)
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 basePos = Application.isPlaying ? startPosition : transform.position;
        Vector3 center = basePos + patrolCenterOffset;
        Gizmos.DrawWireCube(center, new Vector3(patrolAreaSize.x, 1f, patrolAreaSize.y));

        // 맴돌기 범위 예상도 (플레이어 기준)
        if (player != null)
        {
            Gizmos.color = new Color(0.5f, 0, 1f, 0.5f); // 보라색 점선 느낌
            Gizmos.DrawWireSphere(player.position, hoverRadius);
        }
    }
}