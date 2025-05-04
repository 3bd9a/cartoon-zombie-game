using UnityEngine;
using System.Collections;

namespace CartoonZombieGame.Weapons
{
    public enum WeaponType
    {
        Pistol,
        Shotgun,
        AssaultRifle,
        RocketLauncher
    }

    public class WeaponController : MonoBehaviour
    {
        [Header("Weapon Properties")]
        [SerializeField] private WeaponType weaponType = WeaponType.Pistol;
        [SerializeField] private string weaponName = "Pistol";
        [SerializeField] private int damage = 25;
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private int maxAmmo = 12;
        [SerializeField] private int currentAmmo;
        [SerializeField] private float reloadTime = 1.5f;
        [SerializeField] private int totalAmmo = 100;
        [SerializeField] private float range = 100f;
        [SerializeField] private float spreadAngle = 2f;
        [SerializeField] private int bulletsPerShot = 1; // For shotgun
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private GameObject impactEffect;
        [SerializeField] private GameObject bulletTrail;
        [SerializeField] private Transform bulletSpawnPoint;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip emptySound;
        
        [Header("Animation")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private string fireAnimTrigger = "Fire";
        [SerializeField] private string reloadAnimTrigger = "Reload";
        
        [Header("Recoil")]
        [SerializeField] private float recoilForce = 0.1f;
        [SerializeField] private float recoilRecoverySpeed = 10f;
        
        // References
        private AudioSource audioSource;
        private Camera playerCamera;
        
        // State tracking
        private bool isReloading = false;
        private float nextFireTime = 0f;
        private Vector3 originalPosition;
        private CartoonZombieGame.Enemies.ZombieController targetZombie;
        
        // Events
        public delegate void AmmoChangedDelegate(int currentAmmo, int totalAmmo);
        public event AmmoChangedDelegate OnAmmoChanged;
        
        public delegate void WeaponFiredDelegate();
        public event WeaponFiredDelegate OnWeaponFired;
        
        // Properties
        public WeaponType Type => weaponType;
        public string Name => weaponName;
        public int CurrentAmmo => currentAmmo;
        public int TotalAmmo => totalAmmo;
        public bool IsReloading => isReloading;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            playerCamera = Camera.main;
            
            // Set initial ammo
            currentAmmo = maxAmmo;
            
            // Store original position for recoil
            originalPosition = transform.localPosition;
        }
        
        private void Start()
        {
            // Announce initial ammo
            OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
        }
        
        private void Update()
        {
            // Handle weapon inputs
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime && !isReloading)
            {
                Fire();
            }
            
            if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < maxAmmo && totalAmmo > 0)
            {
                StartCoroutine(Reload());
            }
            
            // Return weapon to original position (recoil recovery)
            if (transform.localPosition != originalPosition)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * recoilRecoverySpeed);
            }
        }
        
        private void Fire()
        {
            if (currentAmmo <= 0)
            {
                // Click sound for empty gun
                if (emptySound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(emptySound);
                }
                
                // Auto-reload if out of ammo
                if (totalAmmo > 0 && !isReloading)
                {
                    StartCoroutine(Reload());
                }
                
                return;
            }
            
            // Set next fire time based on fire rate
            nextFireTime = Time.time + fireRate;
            
            // Reduce ammo
            currentAmmo--;
            OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
            
            // Fire animation
            if (weaponAnimator != null)
            {
                weaponAnimator.SetTrigger(fireAnimTrigger);
            }
            
            // Play sound
            if (fireSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fireSound);
            }
            
            // Play muzzle flash
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            // Apply recoil
            ApplyRecoil();
            
            // Fire projectile(s)
            for (int i = 0; i < bulletsPerShot; i++)
            {
                FireProjectile();
            }
            
            // Trigger event
            OnWeaponFired?.Invoke();
        }
        
        private void FireProjectile()
        {
            // Calculate spread
            Vector3 spread = CalculateSpread();
            
            // Create ray
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            ray.direction += spread;
            
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, range))
            {
                // Create bullet trail
                if (bulletTrail != null && bulletSpawnPoint != null)
                {
                    GameObject trail = Instantiate(bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                    StartCoroutine(MoveTrail(trail, hit.point));
                }
                
                // Impact effect
                if (impactEffect != null)
                {
                    GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, 2f);
                }
                
                // Deal damage if hit a zombie
                CartoonZombieGame.Enemies.ZombieController zombie = hit.transform.GetComponent<CartoonZombieGame.Enemies.ZombieController>();
                if (zombie != null)
                {
                    zombie.TakeDamage(CalculateDamage(hit.distance));
                }
            }
            else
            {
                // Bullet going into the distance
                if (bulletTrail != null && bulletSpawnPoint != null)
                {
                    GameObject trail = Instantiate(bulletTrail, bulletSpawnPoint.position, Quaternion.identity);
                    StartCoroutine(MoveTrail(trail, ray.GetPoint(range)));
                }
            }
        }
        
        private Vector3 CalculateSpread()
        {
            // Calculate random spread angle within spreadAngle range
            float x = Random.Range(-spreadAngle, spreadAngle);
            float y = Random.Range(-spreadAngle, spreadAngle);
            
            return new Vector3(x, y, 0) * 0.01f;
        }
        
        private int CalculateDamage(float distance)
        {
            // Damage falloff with distance
            float falloff = Mathf.Clamp01(1 - (distance / range));
            int finalDamage = Mathf.RoundToInt(damage * falloff);
            
            // Special weapon bonuses
            switch (weaponType)
            {
                case WeaponType.Shotgun:
                    // Shotgun does more damage at close range
                    if (distance < range * 0.3f)
                    {
                        finalDamage = Mathf.RoundToInt(finalDamage * 1.5f);
                    }
                    break;
                case WeaponType.RocketLauncher:
                    // Rocket launcher does AOE damage handled elsewhere
                    break;
            }
            
            return finalDamage;
        }
        
        private IEnumerator Reload()
        {
            isReloading = true;
            
            // Reload animation
            if (weaponAnimator != null)
            {
                weaponAnimator.SetTrigger(reloadAnimTrigger);
            }
            
            // Play reload sound
            if (reloadSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
            
            // Wait for reload time
            yield return new WaitForSeconds(reloadTime);
            
            // Calculate ammo to add
            int ammoToAdd = Mathf.Min(maxAmmo - currentAmmo, totalAmmo);
            currentAmmo += ammoToAdd;
            totalAmmo -= ammoToAdd;
            
            OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
            
            isReloading = false;
        }
        
        private void ApplyRecoil()
        {
            // Apply recoil
            transform.localPosition -= Vector3.forward * recoilForce;
        }
        
        private IEnumerator MoveTrail(GameObject trail, Vector3 targetPosition)
        {
            float time = 0;
            Vector3 startPosition = trail.transform.position;
            
            // Set bullet trail speed based on weapon type
            float duration = 0.1f;
            switch (weaponType)
            {
                case WeaponType.AssaultRifle:
                    duration = 0.05f;
                    break;
                case WeaponType.RocketLauncher:
                    duration = 0.3f;
                    break;
            }
            
            while (time < 1)
            {
                trail.transform.position = Vector3.Lerp(startPosition, targetPosition, time);
                time += Time.deltaTime / duration;
                yield return null;
            }
            
            trail.transform.position = targetPosition;
            Destroy(trail, 0.1f);
        }
        
        public void AddAmmo(int amount)
        {
            totalAmmo += amount;
            OnAmmoChanged?.Invoke(currentAmmo, totalAmmo);
        }
        
        public void SwitchWeapon(WeaponType newType)
        {
            // This would be implemented to switch weapons
            // In a real implementation, this might involve disabling the current weapon
            // and enabling another one from a weapons collection
        }
    }
}