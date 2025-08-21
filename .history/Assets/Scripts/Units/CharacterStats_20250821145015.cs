using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP;
    [SerializeField] private int attack = 10;
    [SerializeField] private int defense = 5;
    [SerializeField] private int speed = 5;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int Attack => attack;
    public int Defense => defense;
    public int Speed => speed;

    public bool IsAlive => currentHP > 0;

    public CharacterStats()
    {
        currentHP = maxHP;
    }

    public void Initialize()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        int damage = Mathf.Max(amount - defense, 1);
        currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
    }
}