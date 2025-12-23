using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MeshRenderer))]
public class ShaderAI : MonoBehaviour
{
    [Header("🎯 타겟 설정")]
    public Transform player;

    [Header("👀 감지 범위")]
    [Range(0, 50)] public float warningRadius = 20f;   // 1차: 귓속말 & 경고 (공격 안 함)
    [Range(0, 30)] public float detectionRadius = 8f;  // 2차: 맴돌기 & 빛 비추면 추격 (핵심 구역)
    [Range(0, 5)] public float catchRadius = 1.2f;     // 3차: 게임 오버

    [Header("🛸 맴돌기(Hover) 설정")]
    public float hoverRadius = 4.0f;
    public float hoverSpeed = 3.5f;
    private float hoverTimer = 0f;

    [Header("💨 이동 속도")]
    public float patrolSpeed = 2.0f; // 배회 속도
    public float chaseSpeed = 7.0f;  // 추격 속도

    [Header("🤬 빛 반응 설정 (분노)")]
    public float requiredLightTime = 2.0f;
    public float currentLightExposure = 0f;
    public bool isAggroed = false;

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

    // 빛 감지
    private bool isHitByLightThisFrame = false;
    private float decayDelayTimer = 0f;
    private float decayCooldown = 0.5f;

    // 게임 상태
    private bool isPlayerInWarningZone = false;
    private bool isGameOver = false;
    private bool hasWhispered = false;
    private Color originalColor;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();
        startPosition = transform.position;
        globalPatrolCenter = startPosition + patrolCenterOffset;

        if (meshRenderer.material.HasProperty("_BaseColor"))
            originalColor = meshRenderer.material.GetColor("_BaseColor");
        else
            originalColor = Color.white;

        agent.baseOffset = 0.2f;
        agent.acceleration = 15f;

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

        // 💀 [최우선] 게임 오버 체크
        if (distance <= catchRadius)
        {
            GameOver();
            return;
        }

        // 1. 빛 게이지 관리
        HandleLightExposure();

        // 2. 분노(추격) 상태 판단
        // ★ 중요: 2차 범위(detectionRadius) 밖으로 나가면 무조건 진정함
        if (isAggroed)
        {
            // 2차 범위 밖으로 도망침 -> 추격 포기
            if (distance > detectionRadius)
            {
                CalmDown();
            }
            else
            {
                // 2차 범위 안 + 화남 -> 추격
                ChasePlayer();
                isHitByLightThisFrame = false;
                return; // 추격 중에는 아래 로직 무시
            }
        }

        // 3. 평소 상태 (화나지 않음)
        if (distance <= detectionRadius)
        {
            // [2차 범위 안] 빛을 안 비추면 -> 맴돌기 (Hover)
            HoverAroundPlayer();
        }
        else if (distance <= warningRadius)
        {
            // [1차 범위 안] 경고 & 귓속말만 함 (쫓아오지 않음)
            if (!isPlayerInWarningZone)
            {
                isPlayerInWarningZone = true;
                Debug.Log("👻 [Shader] 주변 공기가 차가워진다... (Warning Zone)");

                if (!hasWhispered)
                {
                    hasWhispered = true;
                    if (WhisperManager.Instance != null)
                    {
                        WhisperManager.Instance.PlayMonsterWhisper(MonsterType.Shader);
                    }
                }
            }
            Patrol(); // 그냥 자기 갈 길 감
        }
        else
        {
            // [범위 밖]
            isPlayerInWarningZone = false;
            Patrol();
        }

        isHitByLightThisFrame = false;
    }

    // --- 🛸 맴돌기 (Hover) ---
    void HoverAroundPlayer()
    {
        agent.speed = hoverSpeed;
        hoverTimer -= Time.deltaTime;

        if (agent.remainingDistance <= agent.stoppingDistance || hoverTimer <= 0)
        {
            Vector3 randomOffset = Random.insideUnitSphere * hoverRadius;
            randomOffset.y = 0;
            Vector3 targetPos = player.position + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
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

            if (meshRenderer != null)
                meshRenderer.material.color = Color.Lerp(originalColor, new Color(1, 0.5f, 0.5f), currentLightExposure / requiredLightTime);

            if (currentLightExposure >= requiredLightTime && !isAggroed)
            {
                BecomeAggressive();
            }
        }
        else
        {
            if (decayDelayTimer > 0) decayDelayTimer -= Time.deltaTime;
            else
            {
                currentLightExposure -= Time.deltaTime;
                if (!isAggroed && meshRenderer != null) meshRenderer.material.color = originalColor;
            }
            if (currentLightExposure < 0) currentLightExposure = 0;
        }
    }

    void BecomeAggressive()
    {
        isAggroed = true;
        Debug.Log("🤬 [Shader] 빛에 반응했다! 추격 시작!");
        agent.speed = chaseSpeed;
        if (meshRenderer != null) meshRenderer.material.color = Color.red;
    }

    void CalmDown()
    {
        isAggroed = false;
        currentLightExposure = 0f;
        agent.speed = patrolSpeed;
        if (meshRenderer != null) meshRenderer.material.color = originalColor;

        Debug.Log("😓 [Shader] 범위 밖으로 도망쳤다. 추격 종료.");
        SetRandomDestination();
    }

    void ChasePlayer()
    {
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
        Debug.LogError("💀 쉐이더에게 잡힘.");
    }

    void OnDrawGizmosSelected()
    {
        // 1차: 노랑 (안전, 경고만)
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, warningRadius);

        // 2차: 빨강 (위험, 맴돌기/추격 구역)
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // 3차: 검정 (사망)
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, catchRadius);

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 basePos = Application.isPlaying ? startPosition : transform.position;
        Vector3 center = basePos + patrolCenterOffset;
        Gizmos.DrawWireCube(center, new Vector3(patrolAreaSize.x, 1f, patrolAreaSize.y));
    }
}