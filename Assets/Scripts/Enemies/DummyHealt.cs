using UnityEngine;
using TMPro;

public class DummyHealt : MonoBehaviour, IDamageAble
{
    public float maxHP = 100f;
    private float currentHealt;

    public TextMeshProUGUI hpText;
    void Start()
    {
        currentHealt = maxHP;
        UpdateHPText();
    }

    public void Damage(float damageAmount)
    {
        currentHealt -= damageAmount;

        if (currentHealt <= 0)
        {
            Die();
        }

        currentHealt = Mathf.Clamp(currentHealt, 0, maxHP);

        UpdateHPText();

    }

    void UpdateHPText()
    {
        hpText.text = Mathf.RoundToInt(currentHealt).ToString();
    }

    private void Die()
    {
        // Itt kezelheted az ellenség halálát, például animáció lejátszása, pontok adása stb.
        Debug.Log("Enemy died!");
        Destroy(gameObject); // Eltávolítja az ellenséget a játékból
    }
}