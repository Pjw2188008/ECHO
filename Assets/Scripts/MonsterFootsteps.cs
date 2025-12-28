using UnityEngine;
using UnityEngine.AI;

public class MonsterFootsteps : MonoBehaviour
{
    [Header("?? 오디오 파일")]
    public AudioClip[] walkClips;
    public AudioClip[] runClips;

    [Header("?? 속도 및 박자")]
    public float runThreshold = 4.0f;
    public float walkInterval = 0.6f;

    // ★ 팁: 소리가 너무 겹치면 이 숫자를 0.3 -> 0.35로 살짝 늘려보세요.
    public float runInterval = 0.3f;

    [Header("?? 멈춤 판정")]
    public float stopThreshold = 0.5f;

    [Header("?? 최적화")]
    public float hearDistance = 15.0f;

    private NavMeshAgent agent;
    private AudioSource audioSource;
    private Animator anim;
    private float stepTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();

        if (audioSource != null)
        {
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = hearDistance + 2.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
        stepTimer = 0f;
    }

    void Update()
    {
        if (agent == null || !agent.enabled || Camera.main == null) return;

        // 거리 최적화
        float dist = Vector3.Distance(transform.position, Camera.main.transform.position);
        if (dist > hearDistance) return;

        // 목적지 도착 체크 (아주 잠깐 멈출 때 소리 끔)
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            stepTimer = 0;
            return;
        }

        // 애니메이션 상태 체크
        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Idle") || stateInfo.IsName("Stand") || stateInfo.IsName("Wait"))
            {
                stepTimer = 0;
                return;
            }
        }

        float currentSpeed = agent.velocity.magnitude;

        // 움직임 감지
        if (currentSpeed > stopThreshold)
        {
            bool isRunning = currentSpeed > runThreshold;
            float currentInterval = isRunning ? runInterval : walkInterval;

            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0)
            {
                PlaySound(isRunning);
                stepTimer = currentInterval;
            }
        }
        else
        {
            stepTimer = 0;
        }
    }

    void PlaySound(bool isRunning)
    {
        if (audioSource == null) return;

        AudioClip[] clips = isRunning ? runClips : walkClips;

        if (clips.Length > 0)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];

            // ★ [수정됨] 뛸 때는 겹침 방지를 위해 Stop() 후 Play()
            if (isRunning)
            {
                if (audioSource.isPlaying) audioSource.Stop();

                audioSource.clip = clip;
                audioSource.pitch = Random.Range(1.0f, 1.15f); // 뛸 땐 피치를 높여 더 급박하게
                audioSource.volume = 1.0f;
                audioSource.Play();
            }
            else
            {
                // 걸을 때는 PlayOneShot으로 자연스럽게 잔향 유지
                audioSource.pitch = Random.Range(0.9f, 1.0f);
                audioSource.PlayOneShot(clip, 0.6f);
            }
        }
    }
}