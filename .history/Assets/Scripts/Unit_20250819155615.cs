using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public UnitClass unitClass;
    public bool isPlayerUnit = true;

    private int currentHealth;

    void Start()
    {
        currentHealth = unitClass.maxHealth;
    }

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(amount - unitClass.defense, 0);
        currentHealth -= finalDamage;
        Debug.Log($"{unitName} took {finalDamage} damage! HP left: {currentHealth}");

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, unitClass.maxHealth);
        Debug.Log($"{unitName} healed for {amount}. HP: {currentHealth}");
    }

    public void UseAbility(int abilityIndex, Unit target)
    {
        if (abilityIndex < 0 || abilityIndex >= unitClass.abilities.Length)
        {
            Debug.LogWarning($"{unitName} tried to use invalid ability index!");
            return;
        }

        unitClass.abilities[abilityIndex].Use(this, target);
    }

    void Die()
    {
        Debug.Log($"{unitName} has been defeated!");
        Destroy(gameObject);
    }
}
