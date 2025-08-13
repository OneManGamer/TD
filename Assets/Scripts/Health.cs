using UnityEngine;

public class Health : MonoBehaviour {
    public float maxHP = 100f;
    public float currentHP;
    public System.Action<Health> OnDeath;

    void Awake() { currentHP = maxHP; }

    public void TakeDamage(float dmg) {
        if (currentHP <= 0) return;
        currentHP -= dmg;
        if (currentHP <= 0) {
            currentHP = 0;
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
