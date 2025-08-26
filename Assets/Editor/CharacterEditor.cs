using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Character))]
public class CharacterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Character character = (Character)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        // Add separator
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Calculated Stats", EditorStyles.boldLabel);
        
        if (character.Stats != null)
        {
            // Display calculated stats in a nice format
            EditorGUILayout.LabelField("Max HP", $"{character.Stats.MaxHP} (Base: {character.Stats.BaseMaxHP})");
            EditorGUILayout.LabelField("Current HP", character.Stats.CurrentHP.ToString());
            EditorGUILayout.LabelField("Attack", $"{character.Stats.Attack} (Base: {character.Stats.BaseAttack})");
            EditorGUILayout.LabelField("Defense", $"{character.Stats.Defense} (Base: {character.Stats.BaseDefense})");
            EditorGUILayout.LabelField("Speed", $"{character.Stats.Speed} (Base: {character.Stats.BaseSpeed})");
            EditorGUILayout.LabelField("Attack Range", $"{character.Stats.AttackRange} (Base: {character.Stats.BaseAttackRange})");
            
            // Show alive status
            EditorGUILayout.LabelField("Status", character.Stats.IsAlive ? "Alive" : "Defeated");
        }
        
        // Add class info
        if (character.CharacterClass != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Class Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Class Name", character.CharacterClass.className);
            EditorGUILayout.LabelField("Description", character.CharacterClass.description);
            
            // Show bonuses
            EditorGUILayout.LabelField("Stat Bonuses:");
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Health", character.CharacterClass.healthBonus.ToString());
            EditorGUILayout.LabelField("Attack", character.CharacterClass.attackBonus.ToString());
            EditorGUILayout.LabelField("Defense", character.CharacterClass.defenseBonus.ToString());
            EditorGUILayout.LabelField("Speed", character.CharacterClass.speedBonus.ToString());
            EditorGUILayout.LabelField("Attack Range", character.CharacterClass.attackRangeBonus.ToString());
            EditorGUI.indentLevel--;
        }
        
        // Add refresh button
        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Stats"))
        {
            if (character.CharacterClass != null)
            {
                // Force reinitialize
                character.Stats.InitializeFromClassSO(character.CharacterClass, character);
                EditorUtility.SetDirty(character);
            }
        }
    }
}