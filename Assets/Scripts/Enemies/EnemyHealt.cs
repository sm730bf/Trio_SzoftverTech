using UnityEngine;

public class EnemyHealt : MonoBehaviour, IDamageAble
{

    [SerializeField] private float maxHealth = 100f;

    private float currentHealt;

    

    private void Start()
    {
        // Inicializálhatod itt az életet, ha szükséges
        currentHealt = maxHealth;
    }

    public void Damage(float damageAmount)
    {
        currentHealt -= damageAmount;

        if (currentHealt <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Itt kezelheted az ellenség halálát, például animáció lejátszása, pontok adása stb.
        Debug.Log("Enemy died!");
        Destroy(gameObject); // Eltávolítja az ellenséget a játékból
    }
}


