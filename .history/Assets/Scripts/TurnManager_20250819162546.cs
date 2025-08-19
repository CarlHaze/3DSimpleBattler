using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public List<Unit> turnOrder = new List<Unit>();
    private int currentTurn = 0;

    void Start()
    {
        turnOrder = new List<Unit>(UnitManager.Instance.allUnits);
        StartTurn();
    }

        void Update()
    {
        // Press Space to advance turn manually
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

        // If player unit, wait for input to select target and ability
        // If AI unit, pick an enemy automatically
        if (!activeUnit.isPlayerUnit)
        {
            List<Unit> enemies = UnitManager.Instance.GetEnemies(activeUnit);
            if (enemies.Count > 0)
            {
                Unit target = enemies[Random.Range(0, enemies.Count)];
                activeUnit.AttackTarget(target);
            }

            EndTurn();
        }
    }

    public void EndTurn()
    {
        currentTurn++;
        if (currentTurn >= turnOrder.Count)
            currentTurn = 0;

        StartTurn();
    }
}
