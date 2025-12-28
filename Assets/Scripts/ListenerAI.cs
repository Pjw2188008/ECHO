using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement; // ★ [추가] 씬 이동을 위해 필수!

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MeshRenderer))]
public class ListenerAI : MonoBehaviour
{
    [Header("💀 게임오버 UI 연결")]
    public GameObject gameOverUI; // ★ [추가] 여기에 'GameOverPanel'을 연결하세요.

    [Header("📷 카메라 잠금 설정")]
    public MonoBehaviour playerCameraScript;


    [Header("🎯 타겟 설정")]
    public Transform player;
    public PlayerController playerScript;

    [Header("📏 감지 범위")]
    [Range(0, 50)] public float warningRadius = 20f;
    [Range(0, 30)] public float detectionRadius = 10f;
    [Range(0, 5)] public float catchRadius = 1.2f;

    [Header("🧱 벽 투시 방지 (장애물)")]
    public LayerMask obstacleLayer;

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

    private bool hasWhispered = false;
    private bool isHitByLightThisFrame = false;
    private float stunTimer = 0f;

    private Animator anim;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();
        anim = GetComponent<Animator>();
        startPosition = transform.position;
        globalPatrolCenter = startPosition + patrolCenterOffset;

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

        // ★ Start에는 UI를 끄는 코드가 없습니다. 
        // (유니티 에디터에서 미리 꺼두신 설정 그대로 시작됩니다)

        SetRandomDestination();
    }

    void Update()
    {
        if (isGameOver || player == null || playerScript == null) return;

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
                if (anim != null) anim.SetTrigger("Attack");
                GameOver();
            }
            return;
        }

        // 2. 경고 범위
        if (distance <= warningRadius)
        {
            if (!isPlayerInWarningZone)
            {
                isPlayerInWarningZone = true;
                if (!hasWhispered)
                {
                    hasWhispered = true;
                    // 귓속말 매니저가 있다면 실행
                     if (WhisperManager.Instance != null) WhisperManager.Instance.PlayMonsterWhisper(MonsterType.Listener);
                }
            }
        }
        else
        {
            isPlayerInWarningZone = false;
        }

        // 3. 감지 및 추격
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

    void UpdateAnimationStates()
    {
        if (anim == null) return;

        if (isStunned)
        {
            anim.SetBool("IsIdle", true);
            anim.SetBool("IsWalk", false);
            anim.SetBool("IsRun", false);
            return;
        }

        float speed = agent.velocity.magnitude;

        if (speed < 0.1f)
        {
            anim.SetBool("IsIdle", true);
            anim.SetBool("IsWalk", false);
            anim.SetBool("IsRun", false);
        }
        else if (speed <= wanderSpeed + 0.5f)
        {
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalk", true);
            anim.SetBool("IsRun", false);
        }
        else
        {
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsWalk", false);
            anim.SetBool("IsRun", true);
        }
    }

    void CheckForPlayer()
    {
        if (playerScript.isCrouching)
        {
            StopChasing();
            Patrol();
            return;
        }

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float dstToPlayer = Vector3.Distance(transform.position, player.position);
        Vector3 startEye = transform.position + Vector3.up * 1.0f;

        if (Physics.Raycast(startEye, dirToPlayer, out RaycastHit hit, dstToPlayer, obstacleLayer))
        {
            if (hit.transform != player)
            {
                StopChasing();
                Patrol();
                return;
            }
        }

        StartChasing();
    }

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

    // ▼▼▼ [수정된 게임오버 함수] ▼▼▼
    void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        agent.isStopped = true;
        Time.timeScale = 0; // [필수] 시간 정지

        // ★ 여기서 UI를 강제로 켭니다!
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        if (playerCameraScript != null)
        {
            playerCameraScript.enabled = false; // "야, 이제 작동하지 마!"
        }

        // [필수] 마우스 커서 보이게 하기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.LogError("💀 Game Over");
    }

    // ▼▼▼ [추가된 타이틀 이동 함수] ▼▼▼
    // "타이틀로 돌아가기" 버튼의 OnClick()에 연결하세요.
    public void GoToTitle()
    {
        Time.timeScale = 1f; // [필수] 시간 흐름 복구
        SceneManager.LoadScene("StartScene"); // 이름 꼭 확인!
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
        Gizmos.DrawLine(basePos, center);

        if (player != null)
        {
            Vector3 startEye = transform.position + Vector3.up * 1.0f;
            Vector3 dir = (player.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, player.position);

            if (Physics.Raycast(startEye, dir, dist, obstacleLayer))
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.white;

            Gizmos.DrawLine(startEye, player.position);
        }
    }
}
