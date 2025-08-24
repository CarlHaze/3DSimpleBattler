# ScriptableObjects Directory

This directory contains all data-driven assets for the character class system.

## Folder Structure

### 📁 Classes/
Character class definitions (CharacterClassSO assets)
- `Core/` - Basic starter classes (Warrior, Ranger, Mage, etc.)
- `Advanced/` - Specialized or higher-tier classes
- `NPC/` - Enemy and non-player character classes

### 📁 ClassLogic/
Optional behavior scripts for classes with special mechanics
- Only create these when classes need unique behaviors
- Most classes can work with just data (no logic needed)

### 📁 Skills/
All skill definitions (SkillSO assets)
- `Common/` - Skills usable by multiple classes
- `[ClassName]/` - Class-exclusive skills
- `Ultimate/` - Powerful end-game abilities

### 📁 StatusEffects/
Buff and debuff definitions
- `Buffs/` - Positive effects
- `Debuffs/` - Negative effects

## Quick Start

1. **Create a new class**: Right-click in Classes/Core → Create → SimpleBattler → Character Class
2. **Add special behavior**: Create ClassLogic asset and assign to class
3. **Create skills**: Right-click in Skills/[Category] → Create → SimpleBattler → Skill
4. **Assign to character**: Drag class SO to Character component in prefab

## Naming Conventions

- Classes: `Warrior.asset`, `FireMage.asset`
- Logic: `WarriorLogic.asset`, `MageLogic.asset`  
- Skills: `Fireball.asset`, `Charge.asset`
- Effects: `Poison.asset`, `Strength.asset`

For detailed documentation, see `Hybrid-ScriptableObject-Class-System.md` in the root directory.