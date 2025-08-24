# ScriptableObject Folder Structure Guide

## Recommended Folder Organization

```
Assets/
├── ScriptableObjects/
│   ├── Classes/
│   │   ├── Core/
│   │   │   ├── Warrior.asset
│   │   │   ├── Ranger.asset
│   │   │   ├── Mage.asset
│   │   │   └── Rogue.asset
│   │   ├── Advanced/
│   │   │   ├── Paladin.asset
│   │   │   ├── Assassin.asset
│   │   │   └── Necromancer.asset
│   │   └── NPC/
│   │       ├── Guard.asset
│   │       ├── Merchant.asset
│   │       └── Boss_Dragon.asset
│   ├── ClassLogic/
│   │   ├── WarriorLogic.asset
│   │   ├── RangerLogic.asset
│   │   ├── MageLogic.asset
│   │   └── BossLogic.asset
│   ├── Skills/
│   │   ├── Common/
│   │   │   ├── BasicAttack.asset
│   │   │   ├── Block.asset
│   │   │   ├── Rest.asset
│   │   │   └── Dodge.asset
│   │   ├── Warrior/
│   │   │   ├── Charge.asset
│   │   │   ├── WhirlwindStrike.asset
│   │   │   ├── DefensiveStance.asset
│   │   │   └── BattleCry.asset
│   │   ├── Ranger/
│   │   │   ├── PrecisionShot.asset
│   │   │   ├── Volley.asset
│   │   │   ├── Track.asset
│   │   │   └── ConcealedPosition.asset
│   │   ├── Mage/
│   │   │   ├── Fireball.asset
│   │   │   ├── Heal.asset
│   │   │   ├── MagicMissile.asset
│   │   │   └── Teleport.asset
│   │   └── Ultimate/
│   │       ├── DragonBreath.asset
│   │       ├── TimeStop.asset
│   │       └── Resurrection.asset
│   └── StatusEffects/
│       ├── Buffs/
│       │   ├── Strength.asset
│       │   ├── Speed.asset
│       │   ├── Regeneration.asset
│       │   └── MagicShield.asset
│       └── Debuffs/
│           ├── Poison.asset
│           ├── Slow.asset
│           ├── Weakness.asset
│           └── Silence.asset
```

## Detailed Folder Purpose

### 📁 Classes/
**Purpose**: Character class definitions (CharacterClassSO assets)

**Subfolders**:
- `Core/` - Basic starter classes available to players
- `Advanced/` - Higher-tier or specialized classes
- `NPC/` - Enemy and non-player character classes
- `Hidden/` - Secret or unlockable classes

**Naming Convention**: 
- Use class name directly: `Warrior.asset`, `Ranger.asset`
- For variants: `WarriorElite.asset`, `RangerForest.asset`

### 📁 ClassLogic/
**Purpose**: Behavior scripts for classes with special mechanics

**Organization**: 
- One asset per logic type
- Name matches corresponding class: `WarriorLogic.asset`
- Generic logic can be shared: `CriticalHitLogic.asset`

**Naming Convention**:
- `[ClassName]Logic.asset` for class-specific
- `[BehaviorName]Logic.asset` for shared behaviors

### 📁 Skills/
**Purpose**: All skill definitions (SkillSO assets)

**Subfolders by Category**:
- `Common/` - Skills available to multiple classes
- `[ClassName]/` - Class-exclusive skills
- `Ultimate/` - Powerful late-game skills
- `Passive/` - Always-active abilities

**Naming Convention**:
- Use descriptive skill names: `Fireball.asset`, `PrecisionShot.asset`
- Include power level for variants: `Fireball.asset`, `GreaterFireball.asset`

### 📁 StatusEffects/
**Purpose**: Buff and debuff definitions (StatusEffectSO assets)

**Subfolders**:
- `Buffs/` - Positive effects
- `Debuffs/` - Negative effects
- `Neutral/` - Status markers (marked, tracked, etc.)

## Alternative Organization Strategies

### Strategy 1: By Game Progression
```
ScriptableObjects/
├── Tier1_Beginner/
│   ├── Classes/
│   ├── Skills/
│   └── Logic/
├── Tier2_Intermediate/
├── Tier3_Advanced/
└── Endgame/
```

### Strategy 2: By Content Type
```
ScriptableObjects/
├── Player/
│   ├── Classes/
│   ├── Skills/
│   └── Logic/
├── Enemies/
│   ├── Classes/
│   ├── Skills/
│   └── Logic/
└── NPCs/
```

### Strategy 3: By Designer Team
```
ScriptableObjects/
├── Combat_Team/
│   ├── WarriorClass/
│   ├── RangerClass/
│   └── Skills/
├── Magic_Team/
│   ├── MageClass/
│   ├── Spells/
│   └── Effects/
└── AI_Team/
    └── EnemyClasses/
```

## Folder Creation Steps

### Method 1: Manual Creation
1. Right-click in Project window
2. Create → Folder
3. Follow the structure above
4. Create assets in appropriate folders

### Method 2: Script-Generated Folders
```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class ScriptableObjectFolderSetup
{
    [MenuItem("SimpleBattler/Setup SO Folders")]
    public static void CreateFolderStructure()
    {
        string basePath = "Assets/ScriptableObjects";
        
        // Create main folders
        Directory.CreateDirectory($"{basePath}/Classes/Core");
        Directory.CreateDirectory($"{basePath}/Classes/Advanced");
        Directory.CreateDirectory($"{basePath}/Classes/NPC");
        Directory.CreateDirectory($"{basePath}/ClassLogic");
        Directory.CreateDirectory($"{basePath}/Skills/Common");
        Directory.CreateDirectory($"{basePath}/Skills/Warrior");
        Directory.CreateDirectory($"{basePath}/Skills/Ranger");
        Directory.CreateDirectory($"{basePath}/Skills/Mage");
        Directory.CreateDirectory($"{basePath}/Skills/Ultimate");
        Directory.CreateDirectory($"{basePath}/StatusEffects/Buffs");
        Directory.CreateDirectory($"{basePath}/StatusEffects/Debuffs");
        
        AssetDatabase.Refresh();
        Debug.Log("ScriptableObject folder structure created!");
    }
}
#endif
```

## Asset Creation Workflow

### 1. Create Class Logic (if needed)
```
Location: ScriptableObjects/ClassLogic/
Steps:
1. Right-click in ClassLogic folder
2. Create → SimpleBattler → Class Logic → [Type] Logic
3. Name: [ClassName]Logic
4. Configure behavior settings
```

### 2. Create Character Class
```
Location: ScriptableObjects/Classes/Core/
Steps:
1. Right-click in appropriate Classes subfolder
2. Create → SimpleBattler → Character Class
3. Name: [ClassName]
4. Set stats, bonuses, description
5. Assign ClassLogic if created
6. Configure starting skills
```

### 3. Create Skills
```
Location: ScriptableObjects/Skills/[Category]/
Steps:
1. Right-click in appropriate Skills subfolder
2. Create → SimpleBattler → Skill
3. Name: [SkillName]
4. Configure damage, costs, requirements
5. Add to class's available/starting skills
```

## Asset Naming Conventions

### Character Classes
- **Format**: `[ClassName].asset`
- **Examples**: `Warrior.asset`, `FireMage.asset`, `EliteGuard.asset`
- **Variants**: `WarriorBerserker.asset`, `WarriorDefender.asset`

### Class Logic
- **Format**: `[ClassName]Logic.asset`
- **Examples**: `WarriorLogic.asset`, `RangerLogic.asset`
- **Shared**: `CriticalHitLogic.asset`, `RegenerationLogic.asset`

### Skills
- **Format**: `[SkillName].asset`
- **Examples**: `Fireball.asset`, `Heal.asset`, `Charge.asset`
- **Tiers**: `Fireball.asset`, `GreaterFireball.asset`, `MasterFireball.asset`

### Status Effects
- **Format**: `[EffectName].asset`
- **Examples**: `Poison.asset`, `Strength.asset`, `MagicShield.asset`

## Best Practices

### Organization Tips
1. **Use Consistent Naming** - Makes searching easier
2. **Group by Usage** - Related assets in same folder
3. **Separate Player/Enemy** - Different balance considerations
4. **Version Control Friendly** - Avoid deep nesting
5. **Designer Accessible** - Clear folder purposes

### Maintenance Guidelines
1. **Regular Cleanup** - Remove unused assets
2. **Reference Tracking** - Know which assets are in use
3. **Backup Strategy** - Version control or regular exports
4. **Team Coordination** - Agree on folder conventions
5. **Documentation** - Update this structure as system grows

### Performance Considerations
1. **Asset Loading** - Don't load entire folders unnecessarily
2. **Build Size** - Remove test/unused assets before build
3. **Memory Usage** - Consider asset size and quantity
4. **Search Performance** - Avoid extremely deep folder structures

## Integration with Version Control

### Git Considerations
```gitignore
# Don't ignore ScriptableObject assets
!Assets/ScriptableObjects/
!Assets/ScriptableObjects/**/*.asset
!Assets/ScriptableObjects/**/*.asset.meta

# But ignore temporary/test assets
Assets/ScriptableObjects/Test/
Assets/ScriptableObjects/**/*_temp.asset
```

### Team Workflow
1. **Asset Ownership** - Assign folders to team members
2. **Merge Strategy** - Use Unity's text serialization
3. **Review Process** - Check new assets before merging
4. **Backup Protocol** - Regular exports of SO configurations

## Example Asset Population

### Starter Warrior Setup
```
Classes/Core/Warrior.asset:
- Base HP: 100, Bonus: +20
- Base Attack: 10, Bonus: +5
- Class Logic: WarriorLogic
- Starting Skills: [BasicAttack, Block, Charge]

ClassLogic/WarriorLogic.asset:
- Rage Threshold: 0.5
- Rage Damage Multiplier: 1.5
- Rage Defense Bonus: 3

Skills/Warrior/Charge.asset:
- Damage: 12
- Range: 2
- Required Classes: [Warrior]
```

This structure provides flexibility for growth while maintaining organization and team productivity!