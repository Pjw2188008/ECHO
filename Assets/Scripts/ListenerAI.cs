using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MeshRenderer))]
public class ListenerAI : MonoBehaviour
{
    [Header("🎯 타겟 설정")]
    public Transform player;
    public PlayerController playerScript;

    [Header("📏 감지 범위")]
    [Range(0, 50)] public float warningRadius = 20f; // 1차: 귓속말 & 경고
    [Range(0, 30)] public float detectionRadius = 10f; // 2차: 추격 시작
    [Range(0, 5)] public float catchRadius = 1.2f; // 3차: 게임오버

    [Header("🧱 벽 투시 방지 (장애물)")]
    public LayerMask obstacleLayer; // 벽이나 장애물 레이어 (설정 필수)

    [Header("🏃 이동 속도")]
    public float wanderSpeed = 2.0f;
    public float chaseSpeed = 6.0f;

    [Header("🚶 배회(Patrol) 설정")]
    public Vector2 patrolAreaSize = new Vector2(10f, 10f);
    public Vector3 patrolCenterOffset = Vector3.zero;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    private float waitTimer = 0f;
    private Vector3 startPosition;
    private Vector3 globalPatrolCenter;

    [Header("🔦 빛 반응 설정 (스턴)")]
    public float requiredLightTime = 1.5f;
    public float stunDuration = 3.0f;
    public float decayCooldown = 0.5f;

    public float currentLightExposure = 0f;
    public bool isStunned = false;
    private float decayDelayTimer = 0f;

    // 내부 변수
    private NavMeshAgent agent;
    private MeshRenderer meshRenderer;
    private bool isChasing = false;
    private bool isPlayerInWarningZone = false;
    private bool isGameOver = false;

    // ★ [추가됨] 귓속말 중복 방지 변수
    private bool hasWhispered = false;

    private bool isHitByLightThisFrame = false;
    private float stunTimer = 0f;

    private Animator anim; // 추가

    void Start()
    {
        
        

        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();
        anim = GetComponent<Animator>(); // 애니메이션 추가
        startPosition = transform.position;
        globalPatrolCenter = startPosition + patrolCenterOffset;

        // 장애물 레이어가 설정 안 되어있으면, 기본적으로 모든 것을 검사하도록 설정
        if (obstacleLayer == 0) obstacleLayer = ~0;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerScript = p.GetComponent<PlayerController>();
            }
        }

        SetRandomDestination();
    }

    void Update()
    {
        // 에러 방지용 안전장치
        if (isGameOver || player == null || playerScript == null) return;

        // 1. 애니메이션 상태 업데이트 (스턴 체크 포함)
        UpdateAnimationStates();

        if (isStunned)
        {
            HandleStun();
            return;
        }

        HandleLightExposure();

        float distance = Vector3.Distance(transform.position, player.position);

        // 1. 게임 오버 체크
        if (distance <= catchRadius)
        {
            if (!isGameOver)
            {
                if (anim != null) anim.SetTrigger("Attack"); // 공격은 트리거!
                GameOver();
            }
            return;
        }

        // 2. 경고 범위 (귓속말 포인트)
        if (distance <= warningRadius)
        {
            if (!isPlayerInWarningZone)
            {
                Debug.Log("👂 [Listener] 쉿... 놈이 근처에 있어. (Warning Zone 진입)");
                isPlayerInWarningZone = true;

                // ★ 아직 귓속말을 안 했다면? -> 실행!
                if (!hasWhispered)
                {
                    hasWhispered = true; // 잠금 (다시는 실행 안 됨)

                    if (WhisperManager.Instance != null)
                    {
                        // 리스너 타입으로 귓속말 요청
                        WhisperManager.Instance.PlayMonsterWhisper(MonsterType.Listener);
                    }
                }
            }
        }
        else
        {
            isPlayerInWarningZone = false;
        }

        // 3. 감지 및 추격 로직
        if (distance <= detectionRadius)
        {
            CheckForPlayer();
        }
        else
        {
            StopChasing();
            Patrol();
        }

        isHitByLightThisFrame = false;
    }

    // 애니메이션 상태를 결정하는 전용 함수
    void UpdateAnimationStates()
    {
        if (anim == null) return;

        // 스턴 상태일 때는 무조건 Idle만 true로 설정
        if (isStunned)
        {
            anim.SetBool("IsIdle", true);
            anim.SetBool("IsWalk", false);
            anim.SetBool("IsRun", false);
            return;
        }

        // 현재 실제 이동 속도 확인
        float speed = agent.velocity.magnitude;

        if (speed < 0.1f) // 정지 상태
        {
            anim.SetBool("IsIdle", true);
            anim.SetBool("IsWalk", false);
            anim.SetBool("IsRun", false);
        }
        else if (speed <= wanderSpeed + 0.5f) // 걷기 상태
        {
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalk", true);
            anim.SetBool("IsRun", false);
        }
        else // 뛰기 상태 (chaseSpeed 등)
        {
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalk", false);
            anim.SetBool("IsRun", true);
        }
    }

    // ▼▼▼ [수정] 벽 검사 기능이 추가된 플레이어 확인 함수 ▼▼▼
    void CheckForPlayer()
    {
        // 1. 앉아있는지 확인 (앉아있으면 안전)
        if (playerScript.isCrouching)
        {
            StopChasing();
            Patrol();
            return;
        }

        // 2. 벽 검사 (Raycast)
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dstToPlayer = Vector3.Distance(transform.position, player.position);

        // 눈높이 보정 (바닥끼리 체크하면 땅에 걸릴 수 있으므로 1m 위에서 쏨)
        Vector3 startEye = transform.position + Vector3.up * 1.0f;

        // 몬스터 눈에서 플레이어 방향으로 레이저 발사
        if (Physics.Raycast(startEye, dirToPlayer, out RaycastHit hit, dstToPlayer, obstacleLayer))
        {
            // 무언가에 부딪혔는데, 그게 플레이어가 아니다? -> 벽이다!
            if (hit.transform != player)
            {
                // 벽에 가려짐 -> 추격 안 함 -> 배회 계속
                // Debug.Log("벽 때문에 안 보임");
                StopChasing();
                Patrol();
                return;
            }
        }

        // 3. 앉지도 않았고, 벽도 없다 -> 추격 시작!
        StartChasing();
    }
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    void Patrol()
    {
        if (isChasing) return;
        agent.speed = wanderSpeed;
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

    // --- 기존 함수들 ---
    public void HitByLight() { isHitByLightThisFrame = true; }

    void HandleLightExposure()
    {
        if (isHitByLightThisFrame)
        {
            currentLightExposure += Time.deltaTime;
            decayDelayTimer = decayCooldown;
            if (meshRenderer != null) meshRenderer.material.color = Color.Lerp(Color.red, Color.cyan, currentLightExposure / requiredLightTime);
            if (currentLightExposure >= requiredLightTime) Stun();
        }
        else
        {
            if (decayDelayTimer > 0) decayDelayTimer -= Time.deltaTime;
            else
            {
                currentLightExposure -= Time.deltaTime;
                if (isChasing && meshRenderer != null) meshRenderer.material.color = Color.red;
                else if (!isChasing && meshRenderer != null) meshRenderer.material.color = Color.white;
            }
            if (currentLightExposure < 0) currentLightExposure = 0;
        }
    }

    void Stun()
    {
        isStunned = true;
        isChasing = false;
        agent.isStopped = true;
        stunTimer = stunDuration;
        currentLightExposure = 0;
        decayDelayTimer = 0;
        Debug.Log("⚡ [Listener] 스턴!");
        if (meshRenderer != null) meshRenderer.material.color = Color.blue;
    }

    void HandleStun()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0)
        {
            isStunned = false;
            agent.isStopped = false;
            SetRandomDestination();
            if (meshRenderer != null) meshRenderer.material.color = Color.white;
        }
    }

    void StartChasing()
    {
        if (isStunned) return;
        if (!isChasing)
        {
            if (meshRenderer != null) meshRenderer.material.color = Color.red;
            isChasing = true;
            waitTimer = 0;
        }
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
    }

    void StopChasing()
    {
        if (isChasing)
        {
            if (meshRenderer != null) meshRenderer.material.color = Color.white;
            isChasing = false;
            agent.ResetPath();
        }
    }

    void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        agent.isStopped = true;
        Time.timeScale = 0;
        Debug.LogError("💀 Game Over");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, warningRadius);
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, catchRadius);

        Gizmos.color = new Color(0, 1, 0, 0.6f);
        Vector3 basePos = Application.isPlaying ? startPosition : transform.position;
        Vector3 center = basePos + patrolCenterOffset;
        Vector3 size = new Vector3(patrolAreaSize.x, 1f, patrolAreaSize.y);
        Gizmos.DrawWireCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(center, 0.3f);
        Gizmos.DrawLine(basePos, center);

        // ★ 벽 감지 디버그 선 (플레이어와 연결선)
        if (player != null)
        {
            // 벽에 막히면 빨간선, 뚫려있으면 하얀선
            Vector3 startEye = transform.position + Vector3.up * 1.0f;
            Vector3 dir = (player.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, player.position);

            if (Physics.Raycast(startEye, dir, dist, obstacleLayer))
                Gizmos.color = Color.red; // 벽 있음
            else
                Gizmos.color = Color.white; // 뚫림

            Gizmos.DrawLine(startEye, player.position);
        }
    }
}