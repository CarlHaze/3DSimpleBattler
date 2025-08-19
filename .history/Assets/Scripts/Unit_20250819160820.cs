using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public UnitClass unitClass;
    public bool isPlayerUnit = true;

    private int currentHealth;

    public float attackRange = 1f;  // Distance at which this unit can attack
    public float attackCooldown = 1f; // seconds between attacks
    private float lastAttackTime;

    void Start()
    {
        currentHealth = unitClass.maxHealth;
        lastAttackTime = -attackCooldown; // so it can attack immediately
    }

    void Update()
    {
        // Check for enemies in range and auto-attack
        Unit target = FindClosestEnemyInRange();
        if (target != null && Time.time - lastAttackTime >= attackCooldown)
        {
            UseAbility(0, target); // use first ability (usually Attack)
            lastAttackTime = Time.time;
        }
    }

    Unit FindClosestEnemyInRange()
    {
        Unit[] allUnits = GameObject.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit closest = null;
        float minDistance = attackRange;

        foreach (Unit u in allUnits)
        {
            if (u == this) continue; // skip self
            if (u.isPlayerUnit == this.isPlayerUnit) continue; // skip allies

            float dist = Vector3.Distance(transform.position, u.transform.position);
            if (dist <= minDistance)
            {
                closest = u;
                minDistance = dist;
            }
        }

        return closest;
    }

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(amount - unitClass.defense, 0);
        currentHealth -= finalDamage;
        Debug.Log($"{unitName} took {finalDamage} damage! Current HP: {currentHealth}");

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, unitClass.maxHealth);
        Debug.Log($"{unitName} healed for {amount}. Current HP: {currentHealth}");
    }

    public void UseAbility(int abilityIndex, Unit target)
    {
        if (abilityIndex < 0 || abilityIndex >= unitClass.abilities.Length)
            return;

        unitClass.abilities[abilityIndex].Use(this, target);
    }

    void Die()
    {
        Debug.Log($"{unitName} has been defeated!");
        Destroy(gameObject);
    }
}
