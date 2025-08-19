using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public List<Unit> turnOrder = new List<Unit>();
    private int currentTurn = 0;
    public float attackRange = 1f; // Distance at which units can attack

    void Start()
    {
        // Initialize turn order with all units
        turnOrder = new List<Unit>(UnitManager.Instance.allUnits);
        StartTurn();
    }

    void Update()
    {
        // Press Space to manually advance turns
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
        }
    }

    void StartTurn()
    {
        if (turnOrder.Count == 0) return;

        Unit activeUnit = turnOrder[currentTurn];
        Debug.Log($"It's {activeUnit.unitName}'s turn!");

        // Get all enemies for this unit
        List<Unit> enemies = UnitManager.Instance.GetEnemies(activeUnit);
        if (enemies.Count == 0)
        {
            Debug.Log("No enemies left!");
            return;
        }

        // Automatically attack the first enemy within range
        Unit target = null;
        foreach (Unit e in enemies)
        {
            float dist = Vector3.Distance(activeUnit.transform.position, e.transform.position);
            if (dist <= attackRange)
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

        // End turn automatically (or you can call this manually later)
        EndTurn();
    }

    public void EndTurn()
    {
        currentTurn++;
        if (currentTurn >= turnOrder.Count)
            currentTurn = 0;

        StartTurn();
    }
}
