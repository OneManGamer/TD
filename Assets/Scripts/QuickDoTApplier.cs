using UnityEngine;
using TD.Combat;

public class QuickDotApplier : MonoBehaviour
{
    public GameObject enemy;
    public float tickDamage = 2f;
    public int ticks = 5;
    public float interval = 0.5f;
    public DamageType type = DamageType.Poison;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3) && enemy)
        {
            var dot = enemy.GetComponent<DoTAttachment>() ?? enemy.AddComponent<DoTAttachment>();
            dot.StartDoT(tickDamage, ticks, interval, type, gameObject);
            Debug.Log($"[DoT] {type} ×{ticks} every {interval}s");
        }
    }
}
