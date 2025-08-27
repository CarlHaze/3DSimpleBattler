TurnManager Features:

  1. Battle Phases:
    - Placement: Unit placement phase
    - Combat: Turn-based combat
    - BattleEnd: Battle finished
  2. Turn Order System:
    - Speed-based turn order (highest speed goes
  first)
    - Player bias for tied speeds (playerSpeedBias =      
  0.1f)
    - Automatic turn progression
  3. Player Turn Management:
    - Auto-selects current player unit
    - Only allows selecting the current turn's unit       
  during combat
    - Ends turn after completing an action
  (attack/skill)
  4. Basic Enemy AI:
    - Finds nearest player unit
    - Attacks if in range
    - Passes turn if no valid targets
  5. Battle Management:
    - Automatically transitions from placement to
  combat when placement is complete
    - Win/lose conditions (no enemies left = victory,     
   no players left = defeat)
    - Unit death handling with turn order updates

  Integration with Existing Systems:

  - ActionMenuController: Only allows selecting
  current turn's unit during combat
  - AttackManager: Ends player turn after successful      
  attack (with 0.5s delay)
  - SkillManager: Will end turn after skill use
  (similar to attacks)

  How it Works:

  1. Start: Place your units (existing placement
  system)
  2. Combat Begins: When placement is complete,
  turn-based combat starts
  3. Turn Order: Calculated by speed with player bias     
   for ties
  4. Player Turns: Select your unit (auto-selected),      
  choose move/attack, turn ends
  5. Enemy Turns: Basic AI attacks nearest player or      
  passes
  6. Victory/Defeat: Battle ends when all units of        
  one side are defeated

  The system is now ready to use! You can add a
  TurnManager to your scene and it will automatically     
   handle the transition from placement to turn-based     
   combat.
