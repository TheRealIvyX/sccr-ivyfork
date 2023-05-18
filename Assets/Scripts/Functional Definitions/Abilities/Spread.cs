using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spread : WeaponAbility
{
    public GameObject spreadPrefab; // the prefabbed sprite for a bullet with a BulletScript
    protected float bulletSpeed; // the speed of the bullet
    protected float survivalTime; // the time the bullet takes to delete itself
    protected Vector3 prefabScale; // the scale of the bullet prefab, used to enlarge the siege turret bullet
    protected float pierceFactor = 0; // pierce factor; increase this to pierce more of the shell
    protected string bulletSound = "clip_bullet2";
    public static readonly int bulletDamage = 1300;

    protected override void Awake()
    {
        base.Awake(); // base awake
        // hardcoded values here
        bulletSpeed = 20f;
        survivalTime = 0.75F;
        range = bulletSpeed * survivalTime;
        ID = AbilityID.Spread;
        cooldownDuration = 2.5F;
        energyCost = 65;
        damage = bulletDamage/8;
        prefabScale = 0.6F * Vector3.one;
        category = Entity.EntityCategory.All;
        bonusDamageType = typeof(ShellCore);
    }

    protected override void Start()
    {
        spreadPrefab = ResourceManager.GetAsset<GameObject>("spread_prefab");
        base.Start();
    }

    /// <summary>
    /// Fires the bullet using the helper method
    /// </summary>
    /// <param name="victimPos">The position to fire the bullet to</param>
    protected override bool Execute(Vector3 victimPos)
    {
        return FireBullet(victimPos); // fire if there is
    }

    public override void ActivationCosmetic(Vector3 targetPos)
    {
        AudioManager.PlayClipByID(bulletSound, transform.position);
    }

    /// <summary>
    /// Helper method for Execute() that creates a bullet and modifies it to be shot
    /// </summary>
    /// <param name="targetPos">The position to fire the bullet to</param>
    protected virtual bool FireBullet(Vector3 targetPos)
    {
        // Create the Bullet from the Bullet Prefab
        if (spreadPrefab == null) {
          spreadPrefab = ResourceManager.GetAsset<GameObject>("spread_prefab");
        }
        ActivationCosmetic(targetPos);
        Vector3 originPos = part ? part.transform.position : Core.transform.position;

        // Calculate future target position
        Vector2 targetVelocity = targetingSystem.GetTarget() ? targetingSystem.GetTarget().GetComponentInChildren<Rigidbody2D>().velocity : Vector2.zero;

        // Closed form solution to bullet lead problem involves finding t via a quadratic solved here.
        Vector2 relativeDistance = targetPos - originPos;
        var a = (bulletSpeed * bulletSpeed - Vector2.Dot(targetVelocity, targetVelocity));
        var b = -(2 * targetVelocity.x * relativeDistance.x + 2 * targetVelocity.y * relativeDistance.y);
        var c = -Vector2.Dot(relativeDistance, relativeDistance);

        if (a == 0 || b * b - 4 * a * c < 0)
        {
            return false;
        }

        var t1 = (-b + Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
        var t2 = (-b - Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);

        float t = t1 < 0 ? (t2 < 0 ? 0 : t2) : (t2 < 0 ? t1 : Mathf.Min(t1, t2));
        if (t < 0)
        {
            return false;
        }
        for (int i=0;i<8;i++) {
          var bullet = Instantiate(spreadPrefab, originPos, Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(relativeDistance.y, relativeDistance.x) * Mathf.Rad2Deg - 90)));
          bullet.transform.localScale = prefabScale;

          // Update its damage to match main bullet
          var script = bullet.GetComponent<SpreadScript>();
          script.owner = GetComponentInParent<Entity>();
          script.SetDamage(GetDamage());
          script.SetCategory(category);
          script.SetTerrain(terrain);
          script.SetShooterFaction(Core.faction);
          script.SetPierceFactor(pierceFactor);
          script.particleColor = part && part.info.shiny ? FactionManager.GetFactionShinyColor(Core.faction) : new Color(0.8F, 1F, 1F, 0.9F);
          script.missParticles = true;

          // Add velocity to the bullet
          if (t != 0)  {
            var angle = Mathf.Atan2(relativeDistance.y, relativeDistance.x);
            var distance = Mathf.Sqrt(Mathf.Pow(-relativeDistance.x, 2) + Mathf.Pow(-relativeDistance.y, 2));
            angle += Random.Range(-0.15F,0.15F);
            float[] oldTargetPosIJFEOIK = {relativeDistance.x, relativeDistance.y}; // i stg if this variable name is taken
            relativeDistance.x = Mathf.Cos(angle)*distance;
            relativeDistance.y = Mathf.Sin(angle)*distance;
            bullet.GetComponent<Rigidbody2D>().velocity = Vector3.Normalize(relativeDistance + targetVelocity * t) * bulletSpeed * Random.Range(0.85F,1F);
            relativeDistance.x = oldTargetPosIJFEOIK[0];
            relativeDistance.y = oldTargetPosIJFEOIK[1];
          }

          // Destroy the bullet after survival time
          if (MasterNetworkAdapter.mode == MasterNetworkAdapter.NetworkMode.Off || !MasterNetworkAdapter.lettingServerDecide) script.StartSurvivalTimer(survivalTime);
        
          if (SceneManager.GetActiveScene().name != "SampleScene")
          {
            bullet.GetComponent<NetworkProjectileWrapper>().enabled = false;
            bullet.GetComponent<NetworkObject>().enabled = false;
          }

          if (MasterNetworkAdapter.mode != MasterNetworkAdapter.NetworkMode.Off && (!MasterNetworkAdapter.lettingServerDecide))
          {
            bullet.GetComponent<NetworkObject>().Spawn();
          }
        }
        return true;
    }
}
