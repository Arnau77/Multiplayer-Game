using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    int Health { get; set; }

    public void RecieveDamage();
}

public class Destructible : MonoBehaviour,IDamageable
{
    private int _health = 100;
    public int Health { get => _health; set => _health = value; }

    public void RecieveDamage()
    {
        Health -= 50;
        if(Health <= 0)
        {
            Health = 0;
            Destroy(gameObject);
        }
    }

}
