// File: EnemyDefinitionSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "TD/Data/Enemy")]
public class EnemyDefinitionSO : ScriptableObject
{
    public float maxHP = 50f;
    public float moveSpeed = 2.5f;
    public int bountyGold = 5;
    public Health.Resist[] resistances;
}
