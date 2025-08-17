using UnityEngine;

namespace TD.Combat
{
  /// <summary>
  /// Synergy logic (pre-resolve), including:
  /// - Cold ladder: Wet + Cold -> Chilled (+1), Chilled(x3)->Frozen(2s); FreezeImmune(5s) on shatter; Cold during immunity -> Chilled only
  /// - Heat interaction: Fire/Energy removes Chilled
  /// - Shatter: Frozen + Impact -> x1.25 (no flat), add FreezeImmune(5s)
  /// - Conductive window: ONLY when Wet is consumed by Cold -> Chilled; lasts 3.0s; cleared if Frozen occurs
  /// - Shock: Wet OR Conductive -> x1.5 + Stunned(0.25), then consume that primer
  /// Falls back to database rules for everything else.
  /// </summary>
  public static class SynergyService
  {
    static SynergyDatabaseSO _db;
    static bool _triedLoad;

    // Tag refs
    static StatusTagSO _wet, _chilled, _frozen, _freezeImmune, _conductive, _stunned;

    static void EnsureLoaded()
    {
      if (!_db && !_triedLoad)
      {
        _triedLoad = true;
        _db = Resources.Load<SynergyDatabaseSO>("TD/SynergyDatabase");
        if (_db == null)
          Debug.LogWarning("[SynergyService] Could not load Resources/TD/SynergyDatabase.asset. Custom logic still runs.");
      }

      // find common tags (from DB rules or by scanning loaded assets)
      if (_wet == null || _chilled == null || _frozen == null || _stunned == null)
        ResolveCommonTags();

      // runtime-only / helper tags
      if (_freezeImmune == null)
      {
        _freezeImmune = ScriptableObject.CreateInstance<StatusTagSO>();
        _freezeImmune.name = "FreezeImmune";
        _freezeImmune.TagId = "FreezeImmune";
        _freezeImmune.DefaultDuration = 10f;
        _freezeImmune.MaxDurationCap = 10f;
        _freezeImmune.Stackable = false;
        _freezeImmune.MaxStacks = 1;
        _freezeImmune.ExclusiveGroup = ""; // coexists with Cold/Heat
      }
      if (_conductive == null)
      {
        _conductive = ScriptableObject.CreateInstance<StatusTagSO>();
        _conductive.name = "Conductive";
        _conductive.TagId = "Conductive";
        _conductive.DefaultDuration = 3.0f;  // per your request
        _conductive.MaxDurationCap = 3.0f;
        _conductive.Stackable = false;
        _conductive.MaxStacks = 1;
        _conductive.ExclusiveGroup = ""; // neutral helper tag
      }
    }

    static void ResolveCommonTags()
    {
      // 1) From DB rules
      if (_db)
      {
        for (int i = 0; i < _db.rules.Count; i++)
        {
          var r = _db.rules[i];
          if (r == null || r.primerTag == null) continue;
          var t = r.primerTag;
          string id = string.IsNullOrEmpty(t.TagId) ? t.name : t.TagId;
          if (_wet     == null && id == "Wet")     _wet     = t;
          if (_chilled == null && id == "Chilled") _chilled = t;
          if (_frozen  == null && id == "Frozen")  _frozen  = t;
          if (_stunned == null && id == "Stunned") _stunned = t;
        }
      }
      // 2) Fallback: scan loaded assets
      var all = Resources.FindObjectsOfTypeAll<StatusTagSO>();
      for (int i = 0; i < all.Length; i++)
      {
        var t = all[i]; if (!t) continue;
        string id = string.IsNullOrEmpty(t.TagId) ? t.name : t.TagId;
        if (_wet     == null && (id == "Wet"     || t.name == "Wet"))       _wet     = t;
        if (_chilled == null && (id == "Chilled" || t.name == "Chilled"))   _chilled = t;
        if (_frozen  == null && (id == "Frozen"  || t.name == "Frozen"))    _frozen  = t;
        if (_stunned == null && (id == "Stunned" || t.name == "Stunned"))   _stunned = t;
      }
    }

    public static StatusTagSO WetTag          { get { EnsureLoaded(); return _wet; } }
    public static StatusTagSO ChilledTag      { get { EnsureLoaded(); return _chilled; } }
    public static StatusTagSO FrozenTag       { get { EnsureLoaded(); return _frozen; } }
    public static StatusTagSO FreezeImmuneTag { get { EnsureLoaded(); return _freezeImmune; } }
    public static StatusTagSO ConductiveTag   { get { EnsureLoaded(); return _conductive; } }

    // --------- PRE-RESOLVE HOOK ----------
    public static void OnPreResolveHit(GameObject source, GameObject target, DamageType detonator, ref float finalDamage)
    {
      EnsureLoaded();
      if (!target) return;

      var controller = target.GetComponentInParent<StatusController>();
      if (!controller) { FallbackRules(ref finalDamage, source, target, detonator); return; }

      // Heat removes Chilled (slow)
      if (detonator == DamageType.Fire || detonator == DamageType.Energy)
      {
        if (_chilled != null && controller.Has(_chilled))
        {
          controller.ConsumeTag(_chilled);
          Debug.Log("[Synergy] Heat removed Chilled");
        }
        FallbackRules(ref finalDamage, source, target, detonator);
        return;
      }

      // ----- Shock path: Wet OR Conductive fuels the reaction -----
      if (detonator == DamageType.Shock)
      {
        bool hasWet        = (_wet != null)        && controller.Has(_wet);
        bool hasConductive = (_conductive != null) && controller.Has(_conductive);

        if (hasWet || hasConductive)
        {
          finalDamage *= 1.5f; // match Wet_Shock rule fantasy
          if (_stunned != null) controller.Add(_stunned, 0.25f, 1);
          Debug.Log("[Synergy] Shock reaction: x1.5 + Stunned(0.25) (Wet/Conductive) -> consumes primer");

          // consume the primer that enabled it
          if (hasWet) controller.ConsumeTag(_wet);
          else        controller.ConsumeTag(_conductive);
          return; // handled; skip DB
        }

        FallbackRules(ref finalDamage, source, target, detonator);
        return;
      }

      // ----- Cold path: ladder + Conductive window + immunity gate -----
      if (detonator == DamageType.Cold)
      {
        bool hasImmune = (_freezeImmune != null) && controller.Has(_freezeImmune);

        // Wet + Cold => progress to Chilled, consume Wet, apply Conductive (3.0s)
        if (_wet != null && controller.Has(_wet))
        {
          controller.ConsumeTag(_wet);
          if (_chilled != null) controller.Add(_chilled, -1f, 1); // +1 & refresh-to-max
          if (_conductive != null) controller.Add(_conductive, _conductive.DefaultDuration, 1); // 3.0s by default
          Debug.Log("[Synergy] Wet + Cold -> Chilled (+1), Wet consumed, Conductive(3.0s) applied");
          return;
        }

        // Cold during FreezeImmune => (re)apply Chilled only (no Frozen)
        if (hasImmune)
        {
          if (_chilled != null)
          {
            controller.Add(_chilled, -1f, 1);
            Debug.Log("[Synergy] FreezeImmune active: Cold -> Chilled (no Frozen)");
          }
          return;
        }

        // Chilled + Cold => +1 stack; at 3 stacks => Frozen(2s), clear Chilled AND Conductive
        if (_chilled != null && controller.Has(_chilled))
        {
          controller.Add(_chilled, -1f, 1);
          int stacks = controller.GetStacks(_chilled);
          Debug.Log($"[Synergy] Chilled + Cold -> stack now {stacks}");
          if (stacks >= 3 && _frozen != null)
          {
            controller.ConsumeTag(_chilled);
            // ensure Conductive disappears when Frozen is achieved
            if (_conductive != null && controller.Has(_conductive))
              controller.ConsumeTag(_conductive);

            controller.Add(_frozen, 2f, 1); // force 2s
            Debug.Log("[Synergy] Chilled reached 3 -> Frozen (2s) (Conductive cleared)");
          }
          return;
        }

        FallbackRules(ref finalDamage, source, target, detonator);
        return;
      }

      // ----- Impact shatter -----
      if (detonator == DamageType.Impact)
      {
        if (_frozen != null && controller.Has(_frozen))
        {
          finalDamage *= 1.25f;           // multiplier only
          controller.ConsumeTag(_frozen); // shatter
          controller.Add(_freezeImmune, 10f, 1);
          Debug.Log("[Synergy] Shatter: Frozen + Impact -> x1.25, Frozen consumed, FreezeImmune(5s)");
          return;
        }
      }

      // Everything else uses authored DB rules
      FallbackRules(ref finalDamage, source, target, detonator);
    }

    // --------- POST-RESOLVE HOOK ----------
    public static void OnPostResolveHit(GameObject source, GameObject target, DamageType detonator, float dealtDamage)
    {
      // reserved for VFX/SFX/analytics
    }

    public static void ApplyFreezeImmunity(StatusController controller, float seconds = 10f)
    {
      EnsureLoaded();
      if (controller != null && _freezeImmune != null)
        controller.Add(_freezeImmune, seconds, 1);
    }

    // Database pass-through for any other authored rules
    static void FallbackRules(ref float finalDamage, GameObject source, GameObject target, DamageType detonator)
    {
      if (_db == null || _db.rules == null || _db.rules.Count == 0) return;

      var controller = target ? target.GetComponentInParent<StatusController>() : null;

      for (int i = 0; i < _db.rules.Count; i++)
      {
        var r = _db.rules[i];
        if (r == null || r.primerTag == null) continue;
        if (r.detonator != detonator) continue;
        if (controller == null || !controller.Has(r.primerTag)) continue;

        float before = finalDamage;
        float mult = (r.damageMultiplier <= 0f) ? 1f : r.damageMultiplier;
        finalDamage = finalDamage * mult + r.flatBonusDamage;

        if (r.applyOnDetonate != null && r.applyOnDetonate.Length > 0)
          controller.ApplyMany(r.applyOnDetonate);

        if (r.consumePrimerOnDetonate)
          controller.ConsumeTag(r.primerTag);

        if (r.addExtraChain && r.extraChainAmount > 0)
          Debug.Log($"[Synergy] Chain +{r.extraChainAmount} from rule '{r.name}'");

        Debug.Log($"[Synergy] Applied '{r.name}': {before:0.###} → {finalDamage:0.###} (x{mult} +{r.flatBonusDamage})");
      }
    }
  }
}
