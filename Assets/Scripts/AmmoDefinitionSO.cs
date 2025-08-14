// File: AmmoDefinitionSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "Ammo", menuName = "TD/Data/Ammo")]
public class AmmoDefinitionSO : ScriptableObject
{
    public string id = "arrow_basic";
    public bool infinite = true;
    public int perShot = 1;
    public int maxCarry = 999;
}
