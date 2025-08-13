using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeathHook : MonoBehaviour {
    public int goldOnKill = 10;
    void Start() { GetComponent<Health>().OnDeath += OnDied; }
    void OnDied(Health h) { LevelController.Instance.OnEnemyKilled(goldOnKill); }
}
