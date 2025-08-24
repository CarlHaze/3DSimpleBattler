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
        CreateFolder($"{basePath}/Classes/Core");
        CreateFolder($"{basePath}/Classes/Advanced");
        CreateFolder($"{basePath}/Classes/NPC");
        CreateFolder($"{basePath}/ClassLogic");
        CreateFolder($"{basePath}/Skills/Common");
        CreateFolder($"{basePath}/Skills/Warrior");
        CreateFolder($"{basePath}/Skills/Ranger");
        CreateFolder($"{basePath}/Skills/Mage");
        CreateFolder($"{basePath}/Skills/Ultimate");
        CreateFolder($"{basePath}/StatusEffects/Buffs");
        CreateFolder($"{basePath}/StatusEffects/Debuffs");
        
        AssetDatabase.Refresh();
        Debug.Log("ScriptableObject folder structure created successfully!");
    }
    
    private static void CreateFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"Created folder: {path}");
        }
        else
        {
            Debug.Log($"Folder already exists: {path}");
        }
    }
    
    [MenuItem("SimpleBattler/Create Class Template")]
    public static void CreateClassTemplate()
    {
        // Create a basic Warrior class as an example
        var warrior = ScriptableObject.CreateInstance<CharacterClassSO>();
        warrior.className = "Warrior";
        warrior.description = "A fierce melee fighter with high health and attack power";
        warrior.baseMaxHP = 100;
        warrior.baseAttack = 10;
        warrior.baseDefense = 5;
        warrior.baseSpeed = 5;
        warrior.baseAttackRange = 1;
        warrior.healthBonus = 20;
        warrior.attackBonus = 5;
        
        AssetDatabase.CreateAsset(warrior, "Assets/ScriptableObjects/Classes/Core/Warrior_Template.asset");
        AssetDatabase.SaveAssets();
        
        Debug.Log("Created Warrior template in Classes/Core/");
    }
}