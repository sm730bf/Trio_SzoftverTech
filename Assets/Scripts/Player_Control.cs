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
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float climbSpeed = 5f;
    private bool isClimbing;
    [SerializeField] private AudioClip checkpointSound; // Ide húzzuk majd a hangot
    private AudioSource audioSource; // Ez lesz a "hangszórónk"

    private CapsuleCollider2D capsuleCollider;

    private float wallJumpCooldown;
    private float horizontalInput;
    private Vector2 checkpointPos;

    private void Awake()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerAnim = GetComponentInChildren<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        checkpointPos = transform.position;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Flip sprite
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        // Animations
        playerAnim.SetBool("Run", horizontalInput != 0 && isGrounded() && !isClimbing);
        playerAnim.SetBool("Grounded", isGrounded());
        playerAnim.SetBool("OnWall", onWall());
        playerAnim.SetBool("Falling", !isGrounded() && playerBody.linearVelocity.y < 0 && !isClimbing);

        // Támadás
        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack();
        }

        // --- MOZGÁS ÉS MÁSZÁS LOGIKA ---
        if (isClimbing)
        {
            playerBody.gravityScale = 0;
            playerBody.linearVelocity = new Vector2(horizontalInput * speed, verticalInput * climbSpeed);
        }
        else
        {
            playerBody.gravityScale = 3;

            // Csak akkor mozgunk normálisan, ha nincs wall jump cooldown
            if (wallJumpCooldown > 0.2f)
            {
                // Falon csúszás
                if (onWall() && !isGrounded())
                {
                    playerBody.gravityScale = 1;
                    playerBody.linearVelocity = Vector2.zero;
                }

                // Vízszintes mozgás
                if (isGrounded())
                {
                    playerBody.linearVelocity = new Vector2(horizontalInput * speed, playerBody.linearVelocity.y);
                }
                else if (!onWall()) // Levegõben mozgás
                {
                    float targetSpeed = horizontalInput * speed;
                    float newVelocityX = Mathf.MoveTowards(playerBody.linearVelocity.x, targetSpeed, airAcceleration * Time.deltaTime);
                    playerBody.linearVelocity = new Vector2(newVelocityX, playerBody.linearVelocity.y);
                }

                // Ugrás figyelése
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Jump();
                }
            }
            else
            {
                wallJumpCooldown += Time.deltaTime;
            }
        }

        // Mászás logika belül
        if (isClimbing)
        {
            playerBody.gravityScale = 0;
            // Csak függõlegesen mozogjon, vagy csak minimális vízszintes mozgást engedjünk!
            playerBody.linearVelocity = new Vector2(horizontalInput * (speed * 0.5f), verticalInput * climbSpeed);
            playerAnim.SetBool("isClimbing", true);
        }
        else
        {
            playerBody.gravityScale = 3;
            playerAnim.SetBool("isClimbing", false); // Ne felejtsd el kikapcsolni!
                                                     // ... többi kód ...
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
            // Wall Jump irányítás
            playerBody.linearVelocity = new Vector2(-Mathf.Sign(transform.localScale.x) * 5, 10);
            wallJumpCooldown = 0;
        }
    }

    private bool isGrounded()
    {
        return capsuleCollider.IsTouchingLayers(groundLayer);
    }

    private bool onWall()
    {
        RaycastHit2D raycasHit = Physics2D.CapsuleCast(capsuleCollider.bounds.center, capsuleCollider.bounds.size, capsuleCollider.direction, 0f, new Vector2(transform.localScale.x, 0), .1f, wallLayer);
        return raycasHit.collider != null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "KillZone")
        {
            StartCoroutine(DieAndRespawn());
        }

        if (collision.CompareTag("Obstacle"))
        {
            StartCoroutine(DieAndRespawn());
        }

        if (collision.CompareTag("Checkpoint"))
        {
            // Hozzáadunk +1-et az Y tengelyhez, hogy kicsit magasabbról essen le
            checkpointPos = collision.transform.position + new Vector3(0, 1f, 0);
            

            if (checkpointSound != null)
            {
                audioSource.PlayOneShot(checkpointSound);
            }

            Debug.Log("Checkpoint mentve!");
        }

        if (collision.CompareTag("Ladder"))
        {
            isClimbing = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isClimbing = false;
            playerBody.gravityScale = 3; // Biztonsági visszaállítás
        }
    }

    private void Attack()
    {
        playerAnim.SetTrigger("Attack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("Eltaláltuk: " + enemy.name);
        }
    }

    IEnumerator DieAndRespawn()
    {
        playerAnim.SetTrigger("Death");
        playerBody.bodyType = RigidbodyType2D.Static;
        yield return new WaitForSeconds(1f);

        transform.position = checkpointPos;

        playerBody.bodyType = RigidbodyType2D.Dynamic;
        playerBody.gravityScale = 3;
        isClimbing = false;
        playerAnim.Rebind();
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}