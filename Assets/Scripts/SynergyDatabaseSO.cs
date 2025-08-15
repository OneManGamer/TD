using System.Collections.Generic;
using UnityEngine;

namespace TD.Combat
{
  [CreateAssetMenu(menuName = "TD/Synergy Database", fileName = "SynergyDatabase")]
  public class SynergyDatabaseSO : ScriptableObject
  {
    public List<SynergyRuleSO> rules = new List<SynergyRuleSO>();
  }
}
