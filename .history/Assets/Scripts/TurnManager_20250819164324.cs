using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public KeyCode nextTurnKey = KeyCode.Q; // Set this in inspector if you want
    private Queue<Unit> turnQueue = new Queue<Unit>();
    private Unit currentUnit;

    void Start()
    {
        // Grab all units from UnitManager at start
        List<Unit> allUnits = UnitManager.Instance.GetAllUnits();

        foreach (Unit unit in allUnits)
        {
            turnQueue.Enqueue(unit);
        }

        Debug.Log("Turn order initialized with " + turnQueue.Count + " units.");

        StartNextTurn();
    }

    void Update()
    {
        if (Input.GetKeyDown(nextTurnKey)) // configurable input
        {
            Debug.Log("Key pressed: " + nextTurnKey);
            EndTurn();
        }
    }

    private void StartNextTurn()
    {
        if (turnQueue.Count == 0)
        {
            Debug.Log("No units left in the turn queue.");
            return;
        }

        currentUnit = turnQueue.Peek(); // Look at the unit whose turn it is
        Debug.Log(currentUnit.unitName + "'s turn!");
    }

    private void EndTurn()
    {
        if (currentUnit != null)
        {
            turnQueue.Dequeue(); // Remove the finished unit
            turnQueue.Enqueue(currentUnit); // Add it back to the end of the queue
            Debug.Log(currentUnit.unitName + " ended their turn.");
        }

        StartNextTurn();
    }
}
