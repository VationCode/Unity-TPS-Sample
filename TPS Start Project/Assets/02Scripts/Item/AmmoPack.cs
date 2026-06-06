using UnityEngine;

public class AmmoPack : MonoBehaviour, IItem
{
    public int ammo = 30;

    public void Use(GameObject target)
    {
        var playerShooter = target.GetComponent<PlayerShooter>();
        if(playerShooter != null && playerShooter.Gun != null)
        {
            playerShooter.Gun.AmmoRemain += ammo;
        }

        Destroy(gameObject);
    }
}