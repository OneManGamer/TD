#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TD.Combat.EditorTools
{
  public static class TDDefaultAssetsGenerator
  {
    [MenuItem("Tools/TD/Create Default Damage Assets")]
    public static void CreateAll()
    {
      string root = "Assets/TD/Combat/Data";
      string statusDir = $"{root}/Status";
      string rulesDir = $"{root}/Rules";
      string resDir = "Assets/Resources/TD";

      EnsureDir(root);
      EnsureDir(statusDir);
      EnsureDir(rulesDir);
      EnsureDir(resDir);

      // Create status tags.
      var tags = new Dictionary<string, StatusTagSO>();
      StatusTagSO MakeTag(string id, float dur, bool stackable = false, int maxStacks = 1, string group = "")
      {
        var asset = ScriptableObject.CreateInstance<StatusTagSO>();
        asset.TagId = id;
        asset.DefaultDuration = dur;
        asset.Stackable = stackable;
        asset.MaxStacks = Mathf.Max(1, maxStacks);
        asset.ExclusiveGroup = group;
        string path = $"{statusDir}/{id}.asset";
        AssetDatabase.CreateAsset(asset, path);
        tags[id] = asset;
        return asset;
      }

      MakeTag("Wet", 4f, false, 1, "Temperature");
      MakeTag("Chilled", 4f, false, 1, "Temperature");
      MakeTag("Frozen", 1f, false, 1, "Temperature");
      MakeTag("OnFire", 4f);
      MakeTag("Corroded", 5f);
      MakeTag("Poisoned", 6f, true, 5);
      MakeTag("Overloaded", 3f);
      MakeTag("Shrapnel", 10f, true, 3);
      MakeTag("Brittle", 2f);
      MakeTag("Stunned", 0.25f);
      MakeTag("Scalded", 3f);
      MakeTag("Bleeding", 5f, true, 3);
      MakeTag("EMPShocked", 0.5f);
      MakeTag("Sear", 2f);
      MakeTag("AgitatedPoison", 3f);

      AssetDatabase.SaveAssets();

      // Helper to make a rule asset.
      SynergyRuleSO MakeRule(string name, StatusTagSO primer, DamageType det,
                             float mult, float flat, bool consume, StatusApplication[] apply = null,
                             bool addChain = false, int extraChain = 0)
      {
        var r = ScriptableObject.CreateInstance<SynergyRuleSO>();
        r.name = name;
        r.primerTag = primer;
        r.detonator = det;
        r.damageMultiplier = mult <= 0f ? 1f : mult;
        r.flatBonusDamage = flat;
        r.consumePrimerOnDetonate = consume;
        r.applyOnDetonate = apply ?? System.Array.Empty<StatusApplication>();
        r.addExtraChain = addChain;
        r.extraChainAmount = extraChain;
        string path = $"{rulesDir}/{name}.asset";
        AssetDatabase.CreateAsset(r, path);
        return r;
      }

      // Build rules.
      var list = new List<SynergyRuleSO>();

      list.Add(MakeRule("Wet_Shock",
        tags["Wet"], DamageType.Shock, 1.5f, 0f, true,
        new [] { new StatusApplication(tags["Stunned"], 0.25f, 1) }, addChain: true, extraChain: 1));

      list.Add(MakeRule("Wet_Cold",
        tags["Wet"], DamageType.Cold, 1f, 0f, true,
        new [] { new StatusApplication(tags["Frozen"], 1.0f, 1) }));

      list.Add(MakeRule("Wet_Fire",
        tags["Wet"], DamageType.Fire, 1f, 0f, true,
        new [] { new StatusApplication(tags["Scalded"], 3.0f, 1) }));

      list.Add(MakeRule("Chilled_Impact",
        tags["Chilled"], DamageType.Impact, 1f, 15f, false,
        new [] { new StatusApplication(tags["Brittle"], 2.0f, 1) }));

      list.Add(MakeRule("Frozen_Impact",
        tags["Frozen"], DamageType.Impact, 1.25f, 20f, true));

      list.Add(MakeRule("Corroded_Pierce",
        tags["Corroded"], DamageType.Pierce, 1.2f, 0f, false));

      list.Add(MakeRule("Poisoned_Shock",
        tags["Poisoned"], DamageType.Shock, 1.1f, 0f, false,
        new [] { new StatusApplication(tags["AgitatedPoison"], 3.0f, 1) }));

      list.Add(MakeRule("Shrapnel_Slash",
        tags["Shrapnel"], DamageType.Slash, 1.3f, 0f, true,
        new [] { new StatusApplication(tags["Bleeding"], 5.0f, 1) }));

      list.Add(MakeRule("OnFire_Energy",
        tags["OnFire"], DamageType.Energy, 1.2f, 0f, false,
        new [] { new StatusApplication(tags["Sear"], 2.0f, 1) }));

      list.Add(MakeRule("Overloaded_Energy",
        tags["Overloaded"], DamageType.Energy, 1f, 0f, true,
        new [] { new StatusApplication(tags["EMPShocked"], 0.5f, 1) }));

      list.Add(MakeRule("Wet_Acid",
        tags["Wet"], DamageType.Acid, 0.5f, 0f, false));

      // Create database in Resources so SynergyService can auto-load it.
      var db = ScriptableObject.CreateInstance<SynergyDatabaseSO>();
      db.rules = list;
      AssetDatabase.CreateAsset(db, $"{resDir}/SynergyDatabase.asset");

      // Create a neutral resistance profile as a template.
      var rp = ScriptableObject.CreateInstance<DamageResistanceSO>();
      rp.entries = new List<DamageResistanceSO.Entry>();
      foreach (DamageType t in System.Enum.GetValues(typeof(DamageType)))
      {
        rp.entries.Add(new DamageResistanceSO.Entry { type = t, multiplier = 1f });
      }
      AssetDatabase.CreateAsset(rp, $"{root}/Templates/NeutralResistance.asset");

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log("TD Default Damage assets created.  Resources/TD/SynergyDatabase.asset is ready.");
    }

    private static void EnsureDir(string path)
    {
      if (!AssetDatabase.IsValidFolder(path))
      {
        var parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
          string next = $"{cur}/{parts[i]}";
          if (!AssetDatabase.IsValidFolder(next))
          {
            AssetDatabase.CreateFolder(cur, parts[i]);
          }
          cur = next;
        }
      }
    }
  }
}
#endif
