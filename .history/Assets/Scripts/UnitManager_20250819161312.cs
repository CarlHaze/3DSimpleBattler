using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance;

    public List<Unit> allUnits = new List<Unit>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
            allUnits.Add(unit);
    }

    public void UnregisterUnit(Unit unit)
    {
        if (allUnits.Contains(unit))
            allUnits.Remove(unit);
    }

    // Get all enemy units of a given unit
    public List<Unit> GetEnemies(Unit unit)
    {
        List<Unit> enemies = new List<Unit>();
        foreach (Unit u in allUnits)
        {
            if (u.isPlayerUnit != unit.isPlayerUnit)
                enemies.Add(u);
        }
        return enemies;
    }
}
