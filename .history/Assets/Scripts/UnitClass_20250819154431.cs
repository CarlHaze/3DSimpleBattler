using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitClass", menuName = "Unit Class")]
public class UnitClass : ScriptableObject
{
    public string className;
    public int maxHealth;
    public int attackPower;       // Default basic attack
    public int defense;
    public Sprite classSprite;    // Optional: for UI
    public Ability[] abilities;   // Array of abilities this class can use
}
