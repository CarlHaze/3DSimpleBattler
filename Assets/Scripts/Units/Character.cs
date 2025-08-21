using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] private string characterName;
    [SerializeField] private CharacterStats stats = new CharacterStats();

    public string CharacterName => characterName;
    public CharacterStats Stats => stats;

    private void Awake()
    {
        stats.Initialize();
    }
}