using UnityEngine;
using System.Collections.Generic;

public class Leech : WeaponAbility
{
    public LineRenderer line; // line renderer of the beam
    private Material material; // material used by the line renderer
    protected bool firing; // check for line renderer drawing
    protected float timer; // float timer for line renderer drawing
    public GameObject beamHitPrefab;
    public static readonly int beamDamage = 65;
    protected List<Transform> targetArray;


    protected override void Awake()
    {
        // set instance fields 
        base.Awake();
        line = GetComponent<LineRenderer>() ? GetComponent<LineRenderer>() : gameObject.AddComponent<LineRenderer>();
        line.sortingLayerName = "Projectiles";
        line.material = material;
        line.startWidth = line.endWidth = 0.1F;
        damage = beamDamage;
        energyCost = 0;
        ID = AbilityID.Leech;
        range = 12;
        category = Entity.EntityCategory.All;
        bonusDamageType = typeof(AirConstruct);
        cooldownDuration = 0.5f;
        terrain = Entity.TerrainType.Air;
        targetArray = new List<Transform>();
        line.positionCount = 0;
    }

    protected void SetUpCosmetics()
    {
        SetMaterial(ResourceManager.GetAsset<Material>("white_material"));
        line.endColor = part && part.info.shiny ? FactionManager.GetFactionShinyColor(Core.faction) : new Color(0.8F, 1F, 1F, 0.9F);
        line.startColor = part && part.info.shiny ? FactionManager.GetFactionShinyColor(Core.faction) : new Color(0.8F, 1F, 1F, 0.9F);
        if (!beamHitPrefab)
        {
            beamHitPrefab = ResourceManager.GetAsset<GameObject>("weapon_hit_particle");
        }
        if (!particlePrefab)
        {
            particlePrefab = ResourceManager.GetAsset<GameObject>("beamParticle_prefab");
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    public void SetMaterial(Material material)
    {
        this.material = material;
        line.material = material;
    }

    protected void RenderBeam(int currentVertex)
    {
        if (firing && timer < 0.1F*(currentVertex+1)) // timer for drawing the beam, past the set timer float value and it stops being drawn
        {
            line.startWidth = line.endWidth = 0.1F;
            line.SetPosition(0, transform.position); // draw and increment timer
            if (nextTargetPart && !MasterNetworkAdapter.lettingServerDecide)
            {
                line.SetPosition(currentVertex+1, partPos);
            }
            else if (targetArray.Count > currentVertex && targetArray[currentVertex])
            {
                line.SetPosition(currentVertex+1, targetArray[currentVertex].position);
            }
            else if (!MasterNetworkAdapter.lettingServerDecide)
            {
                line.SetPosition(currentVertex+1, line.transform.position); // TODO: Fix
            }

            if (currentVertex == line.positionCount - 2)
                timer += Time.deltaTime;
        }
        else if (firing && timer >= 0.1F*(currentVertex+1) && currentVertex == line.positionCount - 2)
        {
            if (line.startWidth > 0)
            {
                line.startWidth -= 0.01F;
                line.endWidth -= 0.01F;
            }

            if (line.startWidth < 0)
            {
                line.startWidth = line.endWidth = 0;
                firing = false;
                line.positionCount = 0;
            }
        }
        else if (currentVertex == line.positionCount - 2)
        {
            line.positionCount = 0;
            firing = false;
        }
    }

    protected virtual void Update()
    {
        RenderBeam(0);
    }

    protected override bool Execute(Vector3 victimPos)
    {
        if (!beamHitPrefab)
        {
            SetUpCosmetics();
        }
        targetArray.Clear();
        targetArray.Add(targetingSystem.GetTarget());
        FireBeam(victimPos);
        return true;
    }

    public override void ActivationCosmetic(Vector3 targetPos)
    {
        if (!beamHitPrefab)
        {
            SetUpCosmetics();
        }
        AudioManager.PlayClipByID("clip_laser", transform.position);
        if (MasterNetworkAdapter.lettingServerDecide && targetingSystem.GetTarget() && targetingSystem.GetTarget().GetComponentInParent<Entity>())
        {
            GetClosestPart(targetingSystem.GetTarget().GetComponentInParent<Entity>().NetworkGetParts().ToArray());
            targetPos = nextTargetPart.transform.position;
        }


        if (line.positionCount == 0) 
        {
            timer = 0; // start the timer
            line.positionCount = 2; // render the beam line
            if (MasterNetworkAdapter.lettingServerDecide)
            {
                line.SetPosition(1, targetPos);
            }
        }
        else 
        {
            line.positionCount++;
        }
        firing = true;

        Instantiate(beamHitPrefab, targetPos, Quaternion.identity); // instantiate hit effect
    }


    protected void FireBeam(Vector3 victimPos)
    {
        Transform targetToAttack = targetArray[targetArray.Count - 1];
        var residue = targetToAttack.GetComponent<IDamageable>().TakeShellDamage(GetDamage(), 0, GetComponentInParent<Entity>());
        // deal instant damage
        if (nextTargetPart)
        {
            nextTargetPart.TakeDamage(residue);
            targetToAttack.GetComponent<Entity>().TakeEnergy(50*abilityTier);
            Core.TakeEnergy(-50*abilityTier);
            if (targetToAttack.GetComponent<Entity>().GetHealth()[2] < 0) {
              targetToAttack.GetComponent<Entity>().TakeEnergy(targetToAttack.GetComponent<Entity>().GetHealth()[2]);
            }
            if (Core.GetHealth()[2] > Core.GetMaxHealth()[2]) {
              Core.TakeEnergy(Mathf.Max(0,Core.GetMaxHealth()[2]-Core.GetHealth()[2]));
            }
            victimPos = partPos = nextTargetPart.transform.position;
        }

        ActivationCosmetic(victimPos);
    }


    public GameObject particlePrefab;

    private void InstantiateParticles(Vector3 origPos, Vector3 victimPos)
    {
        Vector3 distance = victimPos - origPos;
        Vector3 distanceNormalized = distance.normalized;
        Vector3 currentPos = origPos;

        while ((currentPos - origPos).sqrMagnitude < distance.sqrMagnitude)
        {
            Instantiate(particlePrefab, (Vector2)currentPos, Quaternion.identity);
            currentPos += distanceNormalized * 0.8F;
        }
    }

    ShellPart nextTargetPart;
    Vector2 partPos;

    protected void GetClosestPart(ShellPart[] parts)
    {
        GetClosestPart(transform.position, parts);
    }

    protected void GetClosestPart(Vector3 pos, ShellPart[] parts)
    {
        float closestD = range;
        nextTargetPart = null;
        foreach (var part in parts)
        {
            var distance = Vector2.Distance(part.transform.position, pos);
            if (distance < closestD)
            {
                closestD = distance;
                nextTargetPart = part;
            }
        }
    }

    protected override bool DistanceCheck(Transform targetEntity)
    {
        var parts = targetEntity.GetComponentsInChildren<ShellPart>();
        if (parts.Length == 0)
        {
            return base.DistanceCheck(targetEntity);
        }
        else
        {
            GetClosestPart(parts);
            return nextTargetPart;
        }
    }
}
