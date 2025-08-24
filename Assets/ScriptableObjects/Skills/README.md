# Skills Directory

This directory contains all skill definitions organized by category.

## Folder Organization

### Common/
Skills that can be used by multiple classes
- Basic Attack, Block, Rest, Dodge
- Generic utility skills
- Shared between 2+ classes

### Class-Specific Folders (Warrior/, Ranger/, Mage/)
Skills exclusive to a particular class
- Class-defining abilities
- Thematic skills that reinforce class identity
- Advanced techniques unique to the class

### Ultimate/
Powerful late-game skills
- High damage or game-changing effects
- Significant costs or cooldowns
- Usually require prerequisites
- End-game content skills

## Skill Creation Guidelines

### Skill Types
- **Active**: Player must activate (most combat skills)
- **Passive**: Always active (stat bonuses, special effects)
- **Toggle**: Can be turned on/off (stances, auras)

### Target Types
- **Self**: Only affects the caster
- **Ally**: Friendly units only
- **Enemy**: Hostile units only
- **AnyUnit**: Can target friend or foe
- **Ground**: Target a location
- **Area**: Affects multiple units in an area

### Naming Conventions
- Use descriptive names: `Fireball`, `PrecisionShot`, `HealingLight`
- For tiers: `Heal` → `GreaterHeal` → `MasterHeal`
- Avoid generic names: Use `Charge` not `Skill1`

### Balance Considerations
- Higher damage = higher cost/cooldown
- Area effects = reduced individual damage
- Utility skills = lower/no damage but useful effects
- Class restrictions = more powerful effects allowed

## Examples by Category

### Common Skills
- `BasicAttack.asset` - Standard weapon attack
- `Block.asset` - Reduce incoming damage
- `Rest.asset` - Recover health over time

### Warrior Skills  
- `Charge.asset` - Move and attack with bonus damage
- `DefensiveStance.asset` - Increase defense, reduce movement
- `BattleCry.asset` - Buff nearby allies

### Ranger Skills
- `PrecisionShot.asset` - High accuracy, critical chance
- `Volley.asset` - Multiple projectiles
- `Track.asset` - Reveal enemy information

### Mage Skills
- `Fireball.asset` - Area damage spell
- `Heal.asset` - Restore ally health
- `Teleport.asset` - Instant movement