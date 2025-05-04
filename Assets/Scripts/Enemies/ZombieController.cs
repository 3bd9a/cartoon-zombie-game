using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace CartoonZombieGame.Enemies
{
    public enum ZombieType
    {
        Normal,
        Runner,
        Tank,
        Exploder
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieController : MonoBehaviour
    {
        [Header("Zombie Properties")]
        [SerializeField] private ZombieType zombieType = ZombieType.Normal;
        [SerializeField] private int health = 100;
        [SerializeField] private int damage = 10;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float detectionRange = 15f;

        [Header("Movement")]
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float wanderTimer = 5f;
        [SerializeField] private float runAwayHealthPercentage = 0.3f; // Tank zombies run away at low health
        [SerializeField] private float explosionRadius = 5f; // For exploder zombies
        [SerializeField] private float explosionDamage = 50f; // For exploder zombies
        [SerializeField] private GameObject explosionEffectPrefab;

        // References
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private Transform playerTransform;
        private float timer;
        private bool canAttack = true;
        private int maxHealth;

        // State tracking
        private bool isDead = false;
        private bool isAttacking = false;
        private bool isExploding = false;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            maxHealth = health;

            // Find player
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

            // Configure based on zombie type
            ConfigureZombieType();
        }

        private void Start()
        {
            timer = wanderTimer;
        }

        private void Update()
        {
            if (isDead) return;

            float distanceToPlayer = playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : float.MaxValue;

            // Player detection
            if (playerTransform != null && distanceToPlayer <= detectionRange)
            {
                // Special behavior for different zombie types
                switch (zombieType)
                {
                    case ZombieType.Normal:
                    case ZombieType.Runner:
                        ChasePlayer();
                        break;
                    case ZombieType.Tank:
                        // Tanks run away at low health
                        if ((float)health / maxHealth <= runAwayHealthPercentage)
                        {
                            RunAwayFromPlayer();
                        }
                        else
                        {
                            ChasePlayer();
                        }
                        break;
                    case ZombieType.Exploder:
                        if (distanceToPlayer <= attackRange * 1.5f && !isExploding)
                        {
                            StartCoroutine(ExplodeSequence());
                        }
                        else
                        {
                            ChasePlayer();
                        }
                        break;
                }

                // Attack if in range and not already attacking
                if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
                {
                    StartCoroutine(AttackPlayer());
                }
            }
            else
            {
                // Wander when player not detected
                timer -= Time.deltaTime;
                if (timer <= 0.0f)
                {
                    Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                    navMeshAgent.SetDestination(newPos);
                    timer = wanderTimer;
                }
            }

            // Update animations
            if (animator != null)
            {
                animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            }
        }

        private void ConfigureZombieType()
        {
            switch (zombieType)
            {
                case ZombieType.Runner:
                    navMeshAgent.speed *= 1.5f;
                    health = Mathf.RoundToInt(health * 0.8f);
                    attackCooldown *= 0.7f;
                    break;
                case ZombieType.Tank:
                    navMeshAgent.speed *= 0.7f;
                    health = Mathf.RoundToInt(health * 2.5f);
                    damage = Mathf.RoundToInt(damage * 1.5f);
                    attackCooldown *= 1.5f;
                    break;
                case ZombieType.Exploder:
                    health = Mathf.RoundToInt(health * 0.6f);
                    break;
            }

            maxHealth = health;
        }

        private void ChasePlayer()
        {
            navMeshAgent.SetDestination(playerTransform.position);
        }

        private void RunAwayFromPlayer()
        {
            Vector3 dirToPlayer = transform.position - playerTransform.position;
            Vector3 newPos = transform.position + dirToPlayer.normalized * 10f;
            NavMeshHit hit;
            NavMesh.SamplePosition(newPos, out hit, 10f, 1);
            navMeshAgent.SetDestination(hit.position);
        }

        private IEnumerator AttackPlayer()
        {
            isAttacking = true;
            canAttack = false;

            // Face the player
            Vector3 lookDirection = playerTransform.position - transform.position;
            lookDirection.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDirection);

            // Play attack animation
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Wait for the attack animation timing
            yield return new WaitForSeconds(0.5f); // Timing for when damage happens in animation

            // Apply damage if still in range
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange)
            {
                playerTransform.GetComponent<CartoonZombieGame.Player.PlayerController>()?.TakeDamage(damage);
            }

            yield return new WaitForSeconds(attackCooldown - 0.5f); // Remaining cooldown

            isAttacking = false;
            canAttack = true;
        }

        private IEnumerator ExplodeSequence()
        {
            isExploding = true;
            navMeshAgent.isStopped = true;

            // Play pre-explosion animation
            if (animator != null)
            {
                animator.SetTrigger("Explode");
            }

            yield return new WaitForSeconds(1.5f); // Time before explosion

            // Create explosion effect
            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Deal damage to nearby objects
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider hit in colliders)
            {
                if (hit.CompareTag("Player"))
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float damagePercent = 1f - (distance / explosionRadius);
                    int finalDamage = Mathf.RoundToInt(explosionDamage * damagePercent);
                    hit.GetComponent<CartoonZombieGame.Player.PlayerController>()?.TakeDamage(finalDamage);
                }
            }

            // Self-destruct
            TakeDamage(health);
        }

        public void TakeDamage(int damageAmount)
        {
            if (isDead) return;

            health -= damageAmount;

            if (health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            navMeshAgent.isStopped = true;

            // Play death animation
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            // Remove colliders
            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Destroy after some time
            Destroy(gameObject, 5f);
        }

        // Utility function to find a random position on NavMesh
        private Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
        {
            Vector3 randDirection = Random.insideUnitSphere * dist;
            randDirection += origin;
            NavMeshHit navHit;
            NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
            return navHit.position;
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize detection and attack ranges
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            if (zombieType == ZombieType.Exploder)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawSphere(transform.position, explosionRadius);
            }
        }
    }
}