using UnityEngine;

public class QuickArrowShooter : MonoBehaviour
{
    public ArrowProjectile arrowPrefab;
    public float launchSpeed = 35f;
    public Transform target;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2) && arrowPrefab && target)
        {
            var arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            arrow.LaunchToward(transform.position, target.position, launchSpeed);
            Debug.Log("[Arrow] Launched");
        }
    }
}
