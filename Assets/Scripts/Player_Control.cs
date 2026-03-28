using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player_Control : MonoBehaviour
{
    Rigidbody2D playerBody;
    Animator playerAnim;
    [SerializeField] private float speed;
    [SerializeField] private float jump;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float airAcceleration = 10f;
    [SerializeField] private Transform attackPoint; // Ide húzzuk be az AttackPoint-ot
    [SerializeField] private float attackRange = 0.5f; // Mekkora legyen az ütés köre
    [SerializeField] private LayerMask enemyLayers; // Mit tekintsünk ellenségnek


    // 1. VÁLTOZÁS: Itt átírtuk CapsuleCollider2D-re
    private CapsuleCollider2D capsuleCollider;

    private float wallJumpCooldown;
    private float horizontalInput;
    private Vector2 checkpointPos;

    private void Awake()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerAnim = GetComponentInChildren<Animator>();

        // 2. VÁLTOZÁS: Itt is CapsuleCollider2D-t keresünk
        capsuleCollider = GetComponent<CapsuleCollider2D>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        // Flip sprite
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        // Animations
        playerAnim.SetBool("Run", horizontalInput != 0);
        playerAnim.SetBool("Grounded", isGrounded());
        playerAnim.SetBool("OnWall", onWall());

        // ÚJ SOR: Bekötjük az esést!
        // Ha nem vagy a földön, és az Y sebességed negatív (lefelé mész), akkor zuhansz.
        // A "Falling" nevû Bool-t pipálja be az Animatorban.
        playerAnim.SetBool("Falling", !isGrounded() && playerBody.linearVelocity.y < 0);

        // Támadás figyelése
        // Ha a bal egérgomb HELYETT az "F" billentyût akarod:
        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack();
        }

        if (wallJumpCooldown > 0.2f)
        {
            // ----- Wall stick (unchanged) -----
            if (onWall() && !isGrounded())
            {
                playerBody.gravityScale = 1;
                playerBody.linearVelocity = Vector2.zero;
            }
            else
            {
                playerBody.gravityScale = 3;
            }

            // ----- Movement logic (CHANGED) -----
            // If grounded, apply instant speed (like original)
            if (isGrounded())
            {
                playerBody.linearVelocity = new Vector2(horizontalInput * speed, playerBody.linearVelocity.y);
            }
            // If in the air (and not stuck to a wall), use smooth acceleration/deceleration
            else if (!onWall())
            {
                float targetSpeed = horizontalInput * speed;
                float newVelocityX = Mathf.MoveTowards(playerBody.linearVelocity.x, targetSpeed, airAcceleration * Time.deltaTime);
                playerBody.linearVelocity = new Vector2(newVelocityX, playerBody.linearVelocity.y);
            }

            // ----- Jump input (unchanged) -----
            if (Input.GetKey(KeyCode.Space))
            {
                Jump();
            }
        }
        else
        {
            wallJumpCooldown += Time.deltaTime;
        }
    }

    private void Jump()
    {
        if (isGrounded())
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, jump);
            playerAnim.SetTrigger("Jump");
        }
        else if (onWall() && !isGrounded())
        {
            if (horizontalInput < 0.01f && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            {
                playerBody.linearVelocity = new Vector2(-Mathf.Sign(transform.localScale.x) * 10, 7);
                transform.localScale = new Vector3(-Mathf.Sign(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else
                playerBody.linearVelocity = new Vector2(-Mathf.Sign(transform.localScale.x) * 5, 10);

            wallJumpCooldown = 0;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

    }

    void Start()
    {
        checkpointPos = transform.position; // A kezdõhelyed lesz az elsõ checkpoint
        // ... a többi kódod ami már ott van ...
    }

    // 3. VÁLTOZÁS: BoxCast helyett CapsuleCast-ot használunk a talaj érzékeléséhez
    private bool isGrounded()
    {
        // Közvetlenül megkérdezzük a Unity-t: Érintkezik a kapszula a Ground (Talaj) réteggel?
        return capsuleCollider.IsTouchingLayers(groundLayer);
    }

    // 4. VÁLTOZÁS: BoxCast helyett CapsuleCast-ot használunk a fal érzékeléséhez
    private bool onWall()
    {
        RaycastHit2D raycasHit = Physics2D.CapsuleCast(capsuleCollider.bounds.center, capsuleCollider.bounds.size, capsuleCollider.direction, 0f, new Vector2(transform.localScale.x, 0), .1f, wallLayer);
        return raycasHit.collider != null;
    }

    // Ez a függvény figyeli, ha beleesel a Triggerbe
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ha a tárgy neve, amibe beleestünk, "KillZone"
        if (collision.gameObject.name == "KillZone")
        {
            Die();
        }

        if (collision.CompareTag("Obstacle"))
        {
            StartCoroutine(DieAndRespawn());
        }
    }

    private void Die()
    {
        playerAnim.SetTrigger("Death"); // Elindítja a halál animációt
        playerBody.bodyType = RigidbodyType2D.Static; // Megfagyasztja a karaktert

        // Vár 1 másodpercet, hogy lásd a halált, majd hívja a RestartLevel-t
        Invoke("RestartLevel", 1f);
    }

    private void RestartLevel()
    {
        // Újratölti az aktuális pályát
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Attack()
    {
        playerAnim.SetTrigger("Attack");

        // Létrehozunk egy láthatatlan kört az AttackPoint körül, és megnézzük, mi van benne
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // Végigmegyünk mindenkin, akit eltaláltunk
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("Eltaláltuk: " + enemy.name);
            // Itt késõbb le tudjuk vonni az ellenség életét
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    // Ez a függvény (Coroutine) kezeli a halált és az újraéledést
    IEnumerator DieAndRespawn()
    {
        playerAnim.SetTrigger("Death"); // Halál animáció indítása
        playerBody.bodyType = RigidbodyType2D.Static; // Megállítja a fizikát, hogy ne ess tovább

        yield return new WaitForSeconds(1f); // Vár 1 másodpercet

        // Visszarakjuk a karaktert az utolsó checkpointra
        transform.position = checkpointPos;

        // Visszaállítjuk a fizikát és az animációt
        playerBody.bodyType = RigidbodyType2D.Dynamic;
        playerAnim.Rebind();
    }
}