using UnityEngine;

namespace CartoonZombieGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.1f;

        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        private Rigidbody rb;
        private Animator animator;
        private bool isGrounded;
        private Vector3 movement;
        private bool isJumping;

        // Events
        public delegate void HealthChangedDelegate(int currentHealth, int maxHealth);
        public event HealthChangedDelegate OnHealthChanged;

        public delegate void PlayerDeathDelegate();
        public event PlayerDeathDelegate OnPlayerDeath;

        // Properties
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float MoveSpeed => moveSpeed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            currentHealth = maxHealth;
        }

        private void Update()
        {
            // Check if grounded
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

            // Read input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // Calculate movement
            movement = new Vector3(horizontal, 0f, vertical).normalized;

            // Handle jumping
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                isJumping = true;
            }

            // Update animations
            if (animator != null)
            {
                animator.SetFloat("Speed", movement.magnitude);
                animator.SetBool("IsGrounded", isGrounded);
                animator.SetBool("IsJumping", isJumping && !isGrounded);
            }
        }

        private void FixedUpdate()
        {
            // Move player
            MovePlayer();

            // Apply jump force
            if (isJumping && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isJumping = false;
            }
        }

        private void MovePlayer()
        {
            if (movement.magnitude > 0.1f)
            {
                // Calculate rotation
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

                // Move the player
                Vector3 moveDirection = movement * moveSpeed * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + moveDirection);
            }
        }

        public void TakeDamage(int damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            
            // Trigger health changed event
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            // Trigger death event
            OnPlayerDeath?.Invoke();

            // Disable player control
            enabled = false;

            // Play death animation if available
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }
        }
    }
}