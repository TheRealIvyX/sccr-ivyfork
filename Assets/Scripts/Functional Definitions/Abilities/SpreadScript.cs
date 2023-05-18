using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SpreadScript : MonoBehaviour, IProjectile
{
    public bool missParticles = false;
    public GameObject hitPrefab;
    public GameObject missPrefab;
    private float damage; // damage of the spawned bullet
    private float initialdamage = -1;
    private int faction;
    public Entity owner;
    private Entity.TerrainType terrain;
    private Entity.EntityCategory category;
    private float pierceFactor = 0;
    public Color particleColor;
    Vector2 vector;

    /// <summary>
    /// Sets the damage value of the spawned buller
    /// </summary>
    /// <param name="damage">The damage the bullet does</param>
    public void SetDamage(float damage)
    {
        this.damage = damage; // set damage
        if (initialdamage == -1) initialdamage = damage;
    }

    public void SetPierceFactor(float pierce)
    {
        pierceFactor = pierce;
    }

    public void SetShooterFaction(int faction)
    {
        this.faction = faction;
    }

    public void SetTerrain(Entity.TerrainType terrain)
    {
        this.terrain = terrain;
    }

    public void SetCategory(Entity.EntityCategory category)
    {
        this.category = category;
    }

    public bool CheckCategoryCompatibility(IDamageable entity)
    {
        return (category == Entity.EntityCategory.All || category == entity.GetCategory()) && (terrain == Entity.TerrainType.All || terrain == entity.GetTerrain());
    }

    public void InstantiateHitPrefab()
    {
        Instantiate(hitPrefab, transform.position, Quaternion.identity);
        if (MasterNetworkAdapter.mode != MasterNetworkAdapter.NetworkMode.Off && !MasterNetworkAdapter.lettingServerDecide)
        {
            MasterNetworkAdapter.instance.BulletEffectClientRpc("bullet_hit_prefab", transform.position, Vector2.zero);
        }
    }

    public void InstantiateMissPrefab()
    {
        if (missParticles)
        {
            Instantiate(missPrefab, transform.position, Quaternion.Euler(0, 0, Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg));
        } 
        if (MasterNetworkAdapter.mode != MasterNetworkAdapter.NetworkMode.Off && !MasterNetworkAdapter.lettingServerDecide)
        {
            MasterNetworkAdapter.instance.BulletEffectClientRpc("bullet_miss_prefab",transform.position, vector);
        }
    }

    public void OnDestroy()
    {
        if (transform.GetComponentInChildren<TrailRenderer>())
        {
            transform.GetComponentInChildren<TrailRenderer>().autodestruct = true;
            transform.DetachChildren();
        }
        AIData.collidingProjectiles.Remove(this);
    }

    void Start()
    {
        vector = GetComponent<Rigidbody2D>().velocity;
        GetComponent<SpriteRenderer>().color = particleColor;
        AIData.collidingProjectiles.Add(this);
    }

    public void StartSurvivalTimer(float time)
    {
        StartCoroutine(DestroyTimer(time));
    }

    IEnumerator DestroyTimer(float time)
    {
        yield return new WaitForSeconds(time);
        InstantiateMissPrefab();

        if (MasterNetworkAdapter.mode == MasterNetworkAdapter.NetworkMode.Off || !MasterNetworkAdapter.lettingServerDecide)
            Destroy(gameObject);
    }

    public int GetFaction()
    {
        return faction;
    }

    public Entity GetOwner()
    {
        return owner;
    }

    public Vector2 GetPosition()
    {
        return transform.position;
    }

    public float GetDamage()
    {
        return damage;
    }

    private void Update() {
      damage -= (initialdamage-(75/8))*(Time.deltaTime/0.75F);
    }

    public void HitPart(ShellPart part)
    {
        if (MasterNetworkAdapter.mode == MasterNetworkAdapter.NetworkMode.Off || !NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            var residue = part.craft.TakeShellDamage(damage, pierceFactor, owner); // deal the damage to the target, no shell penetration  
            part.TakeDamage(residue); // if the shell is low, damage the part
            damage = 0; // make sure, that other collision events with the same bullet don't do any more damage
        }

        InstantiateHitPrefab();
        if (MasterNetworkAdapter.mode != MasterNetworkAdapter.NetworkMode.Off && NetworkManager.Singleton.IsServer)
        {
            if (GetComponent<NetworkObject>().IsSpawned)
                GetComponent<NetworkObject>().Despawn();
        }
        Destroy(gameObject); // bullet has collided with a target, delete immediately
    }

    // Shards, core parts
    public void HitDamageable(IDamageable damageable)
    {
        if (MasterNetworkAdapter.mode == MasterNetworkAdapter.NetworkMode.Off || !NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            float residue = damageable.TakeShellDamage(damage, pierceFactor, owner);

            if (damageable is Entity)
            {
                (damageable as Entity).TakeCoreDamage(residue);
            }

            InstantiateHitPrefab();
            if (MasterNetworkAdapter.mode != MasterNetworkAdapter.NetworkMode.Off && NetworkManager.Singleton.IsServer)
            {
                if (GetComponent<NetworkObject>().IsSpawned)
                    GetComponent<NetworkObject>().Despawn();
            }
            Destroy(gameObject); // bullet has collided with a target, delete immediately
        }
    }
}
