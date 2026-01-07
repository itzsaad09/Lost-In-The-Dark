using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Required for Coroutines

namespace FpsHorrorKit
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    public class ZombieFollow : MonoBehaviour
    {
        public enum EnemyState { Idle, Walking, Chasing, Attacking }

        [Header("State Settings")]
        public EnemyState currentState = EnemyState.Idle;
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
        public AudioClip moanSound;   
        public AudioClip breathSound; 
        public AudioClip attackSound; 
        [Range(0, 1)] public float volume = 1.0f;

        [Header("References")]
        public Transform player;
        public GameObject gameOverUI; 
        [Tooltip("How long to wait after attack starts before showing UI")]
        public float gameOverDelay = 1.0f; 

        private FpsController playerController;
        private NavMeshAgent agent;
        private Animator animator;
        private AudioSource audioSource;
        private float stateTimer = 0f;
        private bool isGameOver = false;

        private Vector3 lastPosition;
        private float stuckTimer = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            
            audioSource.spatialBlend = 1.0f; 
            audioSource.playOnAwake = false;
            audioSource.loop = true;

            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (player != null)
                playerController = player.GetComponent<FpsController>();

            if (gameOverUI != null) gameOverUI.SetActive(false);
        }

        private void Update()
        {
            if (Time.timeScale == 0 || isGameOver || player == null) 
            {
                if (audioSource.isPlaying && !isGameOver) audioSource.Pause(); 
                if (agent.enabled) agent.isStopped = true; 
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            HandleStuckCheck();

            switch (currentState)
            {
                case EnemyState.Idle: HandleIdle(); break;
                case EnemyState.Walking: HandleWalking(distanceToPlayer); break;
                case EnemyState.Chasing: HandleChasing(distanceToPlayer); break;
                case EnemyState.Attacking: HandleAttacking(distanceToPlayer); break;
            }

            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        private void TransitionToState(EnemyState newState)
        {
            if (currentState == newState && audioSource.isPlaying) return;
            currentState = newState;

            switch (currentState)
            {
                case EnemyState.Idle:
                case EnemyState.Walking: PlayLoopingSound(moanSound); break;
                case EnemyState.Chasing: PlayLoopingSound(breathSound); break;
                case EnemyState.Attacking:
                    StartCoroutine(HandleAttackSequence()); // Trigger the sequenced attack
                    break;
            }
        }

        private IEnumerator HandleAttackSequence()
        {
            // 1. Play Attack Animation and Sound
            animator.SetTrigger("Attack");
            if (attackSound != null) audioSource.PlayOneShot(attackSound, volume);

            // 2. Wait for the biting/attack animation to finish or reach a certain point
            // Use yield return new WaitForSecondsRealtime so it works even if we adjust time
            yield return new WaitForSeconds(gameOverDelay);

            // 3. Trigger Game Over UI
            TriggerGameOver();
        }

        private void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;
            
            if (gameOverUI != null) gameOverUI.SetActive(true);
            
            Time.timeScale = 0f; 
            AudioListener.pause = true; 
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HandleAttacking(float dist)
        {
            agent.isStopped = true;
            Vector3 lookDir = (player.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
        }

        // --- Standard Methods remain the same ---
        private void HandleIdle()
        {
            agent.isStopped = true;
            stateTimer += Time.deltaTime;
            if (CanSeeOrHearPlayer()) { TransitionToState(EnemyState.Chasing); return; }
            if (stateTimer >= idleDuration)
            {
                stateTimer = 0;
                TransitionToState(EnemyState.Walking);
                agent.isStopped = false;
                agent.SetDestination(GetRandomPoint(transform.position, wanderRadius));
            }
        }

        private void HandleWalking(float dist)
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;
            if (CanSeeOrHearPlayer()) { TransitionToState(EnemyState.Chasing); return; }
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                TransitionToState(EnemyState.Idle);
                stateTimer = 0;
            }
        }

        private void HandleChasing(float dist)
        {
            if (playerController != null && playerController.isInteracting)
            {
                TransitionToState(EnemyState.Idle);
                stateTimer = 0;
                return;
            }

            agent.speed = chaseSpeed;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (dist <= attackRange) TransitionToState(EnemyState.Attacking);
            else if (dist > detectionRange * 1.5f) { TransitionToState(EnemyState.Idle); stateTimer = 0; }
        }

        private bool CanSeeOrHearPlayer()
        {
            if (playerController != null && playerController.isInteracting) return false;
            float dist = Vector3.Distance(transform.position, player.position);
            if (canHearPlayer && dist < hearingRange) return true; 
            if (dist < detectionRange)
            {
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
                {
                    if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, dist, obstacleMask)) return true;
                }
            }
            return false;
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

        private void HandleStuckCheck()
        {
            if (currentState == EnemyState.Chasing || currentState == EnemyState.Walking)
            {
                if (Vector3.Distance(transform.position, lastPosition) < 0.01f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 2f) { agent.SetDestination(GetRandomPoint(transform.position, 5f)); stuckTimer = 0; }
                }
                else stuckTimer = 0;
                lastPosition = transform.position;
            }
        }

        private Vector3 GetRandomPoint(Vector3 center, float range)
        {
            Vector3 randomDirection = Random.insideUnitSphere * range;
            randomDirection += center;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, range, NavMesh.AllAreas)) return hit.position;
            return center;
        }
    }
}