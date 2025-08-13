using UnityEngine;

public class Projectile : MonoBehaviour {
    public float speed = 18f;
    public float damage = 12f;
    public float life = 2f;
    Transform target;

    public void Init(Transform t, float dmg) {
        target = t;
        damage = dmg;
        Destroy(gameObject, life);
    }

    void Update() {
        if (target == null) { Destroy(gameObject); return; }
        Vector3 dir = target.position - transform.position;
        float step = speed * Time.deltaTime;
        if (dir.magnitude <= step) Hit();
        else {
            transform.position += dir.normalized * step;
            transform.LookAt(target.position);
        }
    }

    void Hit() {
        var h = target.GetComponent<Health>();
        if (h != null) h.TakeDamage(damage);
        Destroy(gameObject);
    }
}
