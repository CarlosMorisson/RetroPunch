using UnityEngine;

public class SimpleProjectille : ProjectileBase
{

    protected override void HandleHit(GameObject hitTarget, Vector3 hitPoint)
    {
        print("bateu");
    }
    protected override void OnProjectileDestroy()
    {

    }

}
