using UnityEngine;
using UnityEngine.AI;

namespace FpsHorrorKit
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    public class ZombieFollow : MonoBehaviour
    {
        public enum ZombieState { Idle, Walking, Chasing, Attacking }

        [Header("State Settings")]
        public ZombieState currentState = ZombieState.Idle;
        public float idleDuration = 5f;
        public float wanderRadius = 20f;

        [Header("Vision Settings")]
        public float detectionRange = 15f;
        public float viewAngle = 90f;
        public LayerMask obstacleMask; 

        [Header("Hearing Settings")]
        public float hearingRange = 8f; 
        public bool canHearPlayer = true;

        [Header("Speeds")]
        public float patrolSpeed = 2f;
        public float chaseSpeed = 5f;
        public float attackRange = 2f;

        [Header("Audio Sounds")]
        public AudioClip breathSound;   // Played during Idle/Walking
        public AudioClip moanSound; // Played during Chasing
        public AudioClip attackSound; // Played during Attack
        [Range(0, 1)] public float volume = 1.0f;

        [Header("References")]
        public Transform player;

        private NavMeshAgent agent;
        private Animator animator;
        private AudioSource audioSource;
        private float stateTimer = 0f;

        // Anti-Stuck Variables
        private Vector3 lastPosition;
        private float stuckTimer = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            
            // Configure AudioSource for 3D Horror
            audioSource.spatialBlend = 1.0f; // 3D Sound
            audioSource.playOnAwake = false;
            audioSource.loop = true;

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void Start()
        {
            TransitionToState(ZombieState.Idle);
            agent.isStopped = true;
            stateTimer = 0;
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            HandleStuckCheck();

            switch (currentState)
            {
                case ZombieState.Idle:
                    HandleIdle();
                    break;

                case ZombieState.Walking:
                    HandleWalking(distanceToPlayer);
                    break;

                case ZombieState.Chasing:
                    HandleChasing(distanceToPlayer);
                    break;

                case ZombieState.Attacking:
                    HandleAttacking(distanceToPlayer);
                    break;
            }

            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        private void TransitionToState(ZombieState newState)
        {
            if (currentState == newState && audioSource.isPlaying) return;

            currentState = newState;

            // Handle Audio Changes based on State
            switch (currentState)
            {
                case ZombieState.Idle:
                case ZombieState.Walking:
                    PlayLoopingSound(breathSound);
                    break;
                case ZombieState.Chasing:
                    PlayLoopingSound(moanSound);
                    break;
                case ZombieState.Attacking:
                    // Attack sound is usually a one-shot so it doesn't loop awkwardly
                    if (attackSound != null) audioSource.PlayOneShot(attackSound, volume);
                    break;
            }
        }

        private void PlayLoopingSound(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource.clip == clip && audioSource.isPlaying) return;

            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.Play();
        }

        private void HandleIdle()
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            stateTimer += Time.deltaTime;

            if (CanSeeOrHearPlayer())
            {
                TransitionToState(ZombieState.Chasing);
                return;
            }

            if (stateTimer >= idleDuration)
            {
                stateTimer = 0;
                TransitionToState(ZombieState.Walking);
                agent.isStopped = false;
                agent.SetDestination(GetRandomPoint(transform.position, wanderRadius));
            }
        }

        private void HandleWalking(float dist)
        {
            agent.speed = patrolSpeed;

            if (CanSeeOrHearPlayer())
            {
                TransitionToState(ZombieState.Chasing);
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                TransitionToState(ZombieState.Idle);
                stateTimer = 0;
            }
        }

        private void HandleChasing(float dist)
        {
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (dist <= attackRange)
            {
                TransitionToState(ZombieState.Attacking);
            }
            else if (dist > detectionRange * 1.5f) 
            {
                TransitionToState(ZombieState.Idle);
                stateTimer = 0;
            }
        }

        private void HandleAttacking(float dist)
        {
            agent.isStopped = true;
            
            Vector3 lookDir = (player.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);

            animator.SetTrigger("Attack");

            if (dist > attackRange + 0.5f)
            {
                TransitionToState(ZombieState.Chasing);
            }
        }

        private void HandleStuckCheck()
        {
            if (currentState == ZombieState.Chasing || currentState == ZombieState.Walking)
            {
                if (Vector3.Distance(transform.position, lastPosition) < 0.01f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 2f) 
                    {
                        agent.SetDestination(GetRandomPoint(transform.position, 5f));
                        stuckTimer = 0;
                    }
                }
                else
                {
                    stuckTimer = 0;
                }
                lastPosition = transform.position;
            }
        }

        private bool CanSeeOrHearPlayer()
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (canHearPlayer && dist < hearingRange) return true; 

            if (dist < detectionRange)
            {
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
                {
                    if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, dist, obstacleMask))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Vector3 GetRandomPoint(Vector3 center, float range)
        {
            Vector3 randomDirection = Random.insideUnitSphere * range;
            randomDirection += center;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, range, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return center;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hearingRange);
            Gizmos.color = Color.red;
            Vector3 leftRay = Quaternion.AngleAxis(-viewAngle / 2, Vector3.up) * transform.forward;
            Vector3 rightRay = Quaternion.AngleAxis(viewAngle / 2, Vector3.up) * transform.forward;
            Gizmos.DrawRay(transform.position + Vector3.up, leftRay * detectionRange);
            Gizmos.DrawRay(transform.position + Vector3.up, rightRay * detectionRange);
        }
    }
}