# Hybrid ScriptableObject Class System Documentation

## Overview

The Hybrid ScriptableObject Class System is a flexible, data-driven character class architecture that combines the best of both worlds: **ScriptableObjects for designer-friendly data** and **optional logic plug-ins for complex behaviors**. This system allows you to create new character classes without writing code while still supporting unique mechanics like Warrior rage mode or Ranger critical hits.

## Architecture Principles

### Core Philosophy
- **Composition over Inheritance** - Classes are defined by data + optional behavior components
- **Data-Driven Design** - Stats, skills, and properties are editable in Unity Inspector
- **Optional Complexity** - Only add logic when special behaviors are needed
- **Designer-Friendly** - Non-programmers can create and balance classes
- **Runtime Flexibility** - Characters can change classes, learn skills dynamically

### System Components

```
CharacterClassSO (ScriptableObject) - Data definition
├── Base Stats (HP, Attack, Defense, etc.)
├── Stat Bonuses (applied to characters)
├── Skills Lists (available and starting)
├── ClassLogic Reference (optional behaviors)
└── Visual/Meta Data (icon, color, description)

ClassLogic (ScriptableObject) - Behavior definition
├── Virtual Event Hooks (OnAttackCalculated, OnTakeDamage, etc.)
├── Utility Methods (GetCriticalChance, CanUseSkill, etc.)
└── Class-Specific Logic (Warrior rage, Ranger crits, etc.)

Character (MonoBehaviour) - Runtime container
├── CharacterClassSO Reference
├── CharacterStats (populated from class)
├── Skills List (learned from class)
└── Runtime State Management
```

## Core Components

### 1. CharacterClassSO.cs - Class Data Definition

**Purpose**: Defines all data-driven aspects of a character class.

**Key Features**:
- Base stats and bonuses
- Skill availability and starting skills
- Reference to optional ClassLogic
- Designer validation and auto-population
- Visual customization (icon, color, prefab)

**Example Usage**:
```csharp
[CreateAssetMenu(fileName = "New Character Class", menuName = "SimpleBattler/Character Class")]
public class CharacterClassSO : ScriptableObject
{
    [Header("Basic Information")]
    public string className = "Warrior";
    public string description = "A fierce melee fighter";
    
    [Header("Base Stats")]
    public int baseMaxHP = 100;
    public int baseAttack = 10;
    // ... other stats
    
    [Header("Stat Bonuses")]
    public int healthBonus = 20;  // Warrior gets +20 HP
    public int attackBonus = 5;   // +5 Attack
    
    [Header("Class Logic")]
    [SerializeField] private ClassLogic classLogic; // Optional WarriorLogic
}
```

### 2. ClassLogic.cs - Behavior System

**Purpose**: Defines optional class-specific behaviors through event hooks.

**Key Features**:
- Virtual methods (implement only what you need)
- Combat event hooks (damage calculation, health changes)
- Turn-based hooks (turn start/end, movement)
- Skill and stat modification hooks
- Utility methods for class-specific calculations

**Event Hook Categories**:

#### Combat Events
```csharp
// Modify outgoing damage (Warrior rage bonus)
public virtual void OnAttackCalculated(Character attacker, Character target, ref int damage)

// Modify incoming damage (Warrior damage reduction)
public virtual void OnTakeDamage(Character character, Character attacker, ref int damage)

// Post-damage effects (Warrior lifesteal)
public virtual void OnDealDamage(Character attacker, Character target, int finalDamage)

// Death effects
public virtual void OnUnitDefeated(Character character, Character killer)
```

#### Status Events
```csharp
// Health change reactions (Warrior rage activation)
public virtual void OnHealthChanged(Character character, int oldHealth, int newHealth)

// Dynamic stat modifications
public virtual void OnStatsCalculated(Character character, CharacterStats stats)
```

#### Utility Methods
```csharp
// Class-specific critical hit calculations
public virtual float GetCriticalChance(Character character)
public virtual float GetCriticalMultiplier(Character character)

// Skill restrictions
public virtual bool CanUseSkill(Character character, SkillSO skill)
```

### 3. SkillSO.cs - Reusable Skills

**Purpose**: Data-driven skill system supporting various skill types and requirements.

**Key Features**:
- Skill types (Active, Passive, Toggle)
- Target types (Self, Ally, Enemy, Ground, Area)
- Costs and cooldowns
- Effects and status applications
- Class requirements and prerequisites

**Example Skill Definition**:
```csharp
[CreateAssetMenu(fileName = "New Skill", menuName = "SimpleBattler/Skill")]
public class SkillSO : ScriptableObject
{
    public string skillName = "Rage Strike";
    public SkillType skillType = SkillType.Active;
    public int baseDamage = 15;
    public float damageMultiplier = 1.5f;
    public List<string> requiredClasses = new List<string> { "Warrior" };
}
```

### 4. Character.cs - Runtime Container

**Purpose**: Manages character state and integrates all systems.

**Key Features**:
- References CharacterClassSO for data
- Auto-populates stats and skills from class
- Provides skill management methods
- Handles class changes dynamically
- Designer validation in editor

## Implementation Examples

### Example 1: Creating a Warrior Class

#### Step 1: Create WarriorLogic.cs
```csharp
[CreateAssetMenu(fileName = "Warrior Logic", menuName = "SimpleBattler/Class Logic/Warrior Logic")]
public class WarriorLogic : ClassLogic
{
    [SerializeField] private float rageThreshold = 0.5f; // 50% health
    [SerializeField] private float rageDamageMultiplier = 1.5f;
    
    public override void OnAttackCalculated(Character attacker, Character target, ref int damage)
    {
        if (IsInRageMode(attacker))
        {
            damage = Mathf.RoundToInt(damage * rageDamageMultiplier);
            SimpleMessageLog.Log($"{attacker.CharacterName} attacks with RAGE!");
        }
    }
    
    public override void OnHealthChanged(Character character, int oldHealth, int newHealth)
    {
        bool enteredRage = !IsInRageModeForHealth(oldHealth, character.Stats.MaxHP) && 
                          IsInRageModeForHealth(newHealth, character.Stats.MaxHP);
                          
        if (enteredRage)
        {
            SimpleMessageLog.Log($"{character.CharacterName} enters RAGE MODE!");
        }
    }
    
    private bool IsInRageMode(Character character)
    {
        float healthPercent = (float)character.Stats.CurrentHP / character.Stats.MaxHP;
        return healthPercent <= rageThreshold;
    }
}
```

#### Step 2: Create Warrior CharacterClassSO in Unity
1. Right-click in Project → Create → SimpleBattler → Character Class
2. Name it "WarriorClass"
3. Set properties:
   - Class Name: "Warrior"
   - Base Max HP: 100
   - Health Bonus: 20 (total 120 HP)
   - Attack Bonus: 5
   - Assign WarriorLogic to Class Logic field

#### Step 3: Create Warrior Skills
```csharp
// Create "Charge" skill ScriptableObject
skillName = "Charge"
skillType = Active
baseDamage = 12
range = 2
requiredClasses = ["Warrior"]
```

#### Step 4: Assign to Character
1. Select character prefab
2. Drag WarriorClass SO to Character Class field
3. Stats auto-populate in inspector
4. Starting skills are automatically added

### Example 2: Creating a Ranger Class

#### Step 1: Create RangerLogic.cs
```csharp
[CreateAssetMenu(fileName = "Ranger Logic", menuName = "SimpleBattler/Class Logic/Ranger Logic")]
public class RangerLogic : ClassLogic
{
    [SerializeField] private float baseCriticalChance = 0.15f;
    [SerializeField] private float criticalMultiplier = 2.0f;
    [SerializeField] private float longRangeCritBonus = 0.1f;
    
    public override void OnAttackCalculated(Character attacker, Character target, ref int damage)
    {
        float critChance = GetCriticalChance(attacker);
        
        if (Random.value <= critChance)
        {
            damage = Mathf.RoundToInt(damage * GetCriticalMultiplier(attacker));
            SimpleMessageLog.Log($"{attacker.CharacterName} scores a CRITICAL HIT!");
        }
    }
    
    public override float GetCriticalChance(Character character)
    {
        float totalCrit = baseCriticalChance;
        if (character.Stats.AttackRange >= 3)
        {
            totalCrit += longRangeCritBonus; // Bonus at long range
        }
        return totalCrit;
    }
}
```

#### Step 2: Configure in Unity
- Base Attack Range: 3 (long range)
- Speed Bonus: 2 (mobile)
- Attack Range Bonus: 1 (total range 4)

## How to Expand the System

### Adding New Classes

#### Method 1: Data-Only Class (No Special Logic)
1. Create new CharacterClassSO in Unity
2. Set stats, bonuses, and skills
3. Leave Class Logic field empty
4. Assign to characters

**Example: Tank Class**
- High HP Bonus (+50)
- High Defense Bonus (+8)
- Low Speed (-2)
- No special logic needed

#### Method 2: Class with Custom Logic
1. Create new ClassLogic script inheriting from ClassLogic
2. Override needed event methods
3. Create ClassLogic ScriptableObject asset
4. Create CharacterClassSO and assign the logic
5. Configure stats and skills

### Adding New Skills

#### Step 1: Create Skill ScriptableObject
```csharp
// Example: Heal skill
skillName = "Heal"
skillType = Active
targetType = Ally
healing = 25
manaCost = 10
cooldown = 3
```

#### Step 2: Add to Class Available Skills
- Drag skill to class's availableSkills list
- Add to startingSkills if learned immediately

#### Step 3: Implement Skill Effects (if complex)
```csharp
// In ClassLogic if skill needs special behavior
public override void OnSkillUsed(Character character, SkillSO skill, GameObject target)
{
    if (skill.skillName == "Heal")
    {
        // Custom healing logic beyond basic healing value
        int bonusHealing = character.Stats.Attack / 4; // Scale with attack
        target.GetComponent<Character>().Stats.Heal(skill.healing + bonusHealing);
    }
}
```

### Advanced Skill System Extensions

#### Skill Prerequisites
```csharp
// In SkillSO
public List<SkillSO> prerequisiteSkills = new List<SkillSO>();
public int minLevel = 1;

// Usage: Fireball requires Magic Missile first
```

#### Status Effects
```csharp
// In SkillSO
public List<StatusEffectSO> statusEffects = new List<StatusEffectSO>();

// Create StatusEffectSO system for buffs/debuffs
```

#### Skill Cooldowns
```csharp
// Add to Character class
private Dictionary<SkillSO, float> skillCooldowns = new Dictionary<SkillSO, float>();

public bool IsSkillOnCooldown(SkillSO skill)
{
    return skillCooldowns.ContainsKey(skill) && 
           skillCooldowns[skill] > Time.time;
}
```

## Integration with Existing Systems

### Combat System Integration

The class logic system integrates seamlessly with your existing AttackManager:

```csharp
// In AttackManager.PerformAttack()
int attackPower = attackerCharacter.Stats.Attack;

// Class logic modifies damage
if (attackerCharacter.CharacterClass?.ClassLogic != null)
{
    attackerCharacter.CharacterClass.ClassLogic.OnAttackCalculated(
        attackerCharacter, targetCharacter, ref attackPower);
}

// Apply damage with class logic callbacks
int actualDamage = targetCharacter.Stats.TakeDamage(attackPower, attackerCharacter);
```

### Movement System Integration

```csharp
// In UnitMovementManager
public void OnMoveComplete(Vector3 from, Vector3 to)
{
    if (selectedUnit?.CharacterClass?.ClassLogic != null)
    {
        selectedUnit.CharacterClass.ClassLogic.OnMoveComplete(selectedUnit, from, to);
    }
}
```

### Turn-Based System Integration

```csharp
// In TurnManager (if you add one)
public void StartPlayerTurn(Character character)
{
    if (character.CharacterClass?.ClassLogic != null)
    {
        character.CharacterClass.ClassLogic.OnTurnStart(character);
    }
}
```

## Best Practices

### Design Guidelines

1. **Keep ClassLogic Optional**: Most classes shouldn't need custom logic
2. **Data-Driven First**: Try to solve problems with stats/skills before adding logic
3. **Single Responsibility**: Each ClassLogic should focus on one core theme
4. **Performance Conscious**: Avoid heavy calculations in event hooks
5. **Designer Friendly**: Use [SerializeField] for tweakable values

### Code Organization

```
Assets/
├── ScriptableObjects/
│   ├── Classes/
│   │   ├── WarriorClass.asset
│   │   ├── RangerClass.asset
│   │   └── MageClass.asset
│   ├── ClassLogic/
│   │   ├── WarriorLogic.asset
│   │   ├── RangerLogic.asset
│   │   └── MageLogic.asset
│   └── Skills/
│       ├── Basic/
│       │   ├── Strike.asset
│       │   └── Block.asset
│       ├── Warrior/
│       │   ├── Charge.asset
│       │   └── Rage.asset
│       └── Ranger/
│           ├── PrecisionShot.asset
│           └── Volley.asset
```

### Performance Considerations

1. **Cache References**: Store frequently used components
2. **Event Filtering**: Only call logic hooks when necessary
3. **Pool Objects**: Reuse skill effect objects
4. **Batch Updates**: Group stat recalculations

## Debugging and Testing

### Inspector Debugging
- CharacterStats shows calculated values in inspector
- OnValidate() updates stats in edit mode
- Class logic summaries display behavior descriptions

### Runtime Debugging
```csharp
// Add debug logs to ClassLogic
public override void OnAttackCalculated(Character attacker, Character target, ref int damage)
{
    int originalDamage = damage;
    // ... modify damage
    Debug.Log($"[{logicName}] {attacker.name}: {originalDamage} -> {damage} damage");
}
```

### Unit Testing
```csharp
[Test]
public void WarriorRageActivatesAtHalfHealth()
{
    var warrior = CreateTestWarrior();
    warrior.Stats.TakeDamage(60); // Bring to 40% health
    
    // Should be in rage mode
    var logic = warrior.CharacterClass.ClassLogic as WarriorLogic;
    Assert.IsTrue(logic.IsInRageMode(warrior));
}
```

## Migration from Old System

### Step-by-Step Migration

1. **Create ScriptableObject Assets**
   - Create CharacterClassSO for each existing class
   - Create ClassLogic assets for classes with special behaviors
   - Create SkillSO assets for existing abilities

2. **Update Character Prefabs**
   - Replace CharacterClass references with CharacterClassSO
   - Remove hardcoded stat assignments
   - Let stats auto-populate from class

3. **Update Combat Systems**
   - Modify AttackManager to use new class logic hooks
   - Update damage calculation to support class modifications
   - Add class logic callbacks to movement, healing, etc.

4. **Test and Validate**
   - Verify all classes work as expected
   - Check stat calculations are correct
   - Test class-specific behaviors (rage, crits, etc.)

5. **Cleanup**
   - Remove old CharacterClass.cs and derived classes
   - Remove hardcoded class logic from Character.cs
   - Update documentation and team workflows

## Future Enhancements

### Potential Extensions

1. **Multi-Class System**: Characters with multiple classes
2. **Class Evolution**: Classes that upgrade/evolve over time
3. **Dynamic Stat Modifiers**: Temporary buffs/debuffs system
4. **Skill Trees**: Branching skill progression
5. **Equipment Integration**: Items that modify class abilities
6. **Class Synergies**: Team-based class combinations

### Integration Opportunities

1. **Save System**: Serialize learned skills and class progression
2. **UI System**: Dynamic skill bars based on known skills
3. **AI System**: AI decision making based on available skills
4. **Balancing Tools**: Runtime stat tweaking for playtesting

## Conclusion

The Hybrid ScriptableObject Class System provides a powerful, flexible foundation for character progression that scales from simple stat-based classes to complex behavior-driven archetypes. By combining data-driven design with optional logic plug-ins, it enables both designers and programmers to create rich, varied gameplay experiences without sacrificing maintainability or performance.

The system's composition-based architecture ensures that adding new classes, skills, and behaviors remains straightforward while supporting the full complexity needed for engaging RPG mechanics.