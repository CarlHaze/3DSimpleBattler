using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Ability")]
public class Ability : ScriptableObject
{
    public string abilityName;
    public int power;               // Base damage or healing
    public bool isHealing = false;  // True if this ability heals instead of damages
    public int cost = 0;            // Optional: mana or action points
    public string description;

    public void Use(Unit user, Unit target)
    {
        if (isHealing)
        {
            target.Heal(power);
            Debug.Log($"{user.unitName} heals {target.unitName} for {power} HP!");
        }
        else
        {
            target.TakeDamage(power);
            Debug.Log($"{user.unitName} uses {abilityName} on {target.unitName} for {power} damage!");
        }
    }
}
