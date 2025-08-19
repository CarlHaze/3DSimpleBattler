void StartTurn()
{
    if (turnOrder.Count == 0) return;

    Unit activeUnit = turnOrder[currentTurn];
    Debug.Log($"It's {activeUnit.unitName}'s turn!");

    List<Unit> enemies = UnitManager.Instance.GetEnemies(activeUnit);
    if (enemies.Count == 0)
    {
        Debug.Log("No enemies left!");
        return;
    }

    // Automatically attack the first enemy within range (1 unit distance)
    Unit target = null;
    float attackRange = 1f; // adjust if needed

    foreach (Unit e in enemies)
    {
        if (Vector3.Distance(activeUnit.transform.position, e.transform.position) <= attackRange)
        {
            target = e;
            break;
        }
    }

    if (target != null)
    {
        activeUnit.AttackTarget(target);
    }
    else
    {
        Debug.Log($"{activeUnit.unitName} has no enemies in range.");
    }

    EndTurn();
}
