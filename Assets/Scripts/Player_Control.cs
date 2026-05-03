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
    [SerializeField] private float attackRange = 1.7f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private float damageAmount = 25f;
    [SerializeField] private float wallJumpHeight = 10f;
    [SerializeField] private float wallJumpDistance = 20f;

    private bool isClimbing;
    private bool isDead = false;

    [SerializeField] private AudioClip checkpointSound;
    private AudioSource audioSource;

    private CapsuleCollider2D capsuleCollider;

    private float wallJumpCooldown;
    private float horizontalInput;
    private Vector2 checkpointPos;

    public Animator transition;
    public float transitionTime = 1.25f;

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
        if (isDead) return;

        horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        playerAnim.SetBool("Run", horizontalInput != 0 && isGrounded() && !isClimbing);
        playerAnim.SetBool("Grounded", isGrounded());
        playerAnim.SetBool("OnWall", onWall());
        playerAnim.SetBool("Falling", !isGrounded() && playerBody.linearVelocity.y < 0 && !isClimbing);

        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack();
        }

        if (isClimbing)
        {
            playerBody.gravityScale = 0;
            playerBody.linearVelocity = new Vector2(horizontalInput * (speed * 0.5f), verticalInput * climbSpeed);
            playerAnim.SetBool("isClimbing", true);
        }
        else
        {
            playerBody.gravityScale = 3;
            playerAnim.SetBool("isClimbing", false);

            if (wallJumpCooldown > 0.2f)
            {
                if (onWall() && !isGrounded())
                {
                    playerBody.gravityScale = 1;
                    playerBody.linearVelocity = Vector2.zero;
                }

                if (isGrounded())
                {
                    playerBody.linearVelocity = new Vector2(horizontalInput * speed, playerBody.linearVelocity.y);
                }
                else if (!onWall())
                {
                    float targetSpeed = horizontalInput * speed;
                    float newVelocityX = Mathf.MoveTowards(playerBody.linearVelocity.x, targetSpeed, airAcceleration * Time.deltaTime);
                    playerBody.linearVelocity = new Vector2(newVelocityX, playerBody.linearVelocity.y);
                }

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
            playerBody.linearVelocity = new Vector2(-Mathf.Sign(transform.localScale.x) * wallJumpHeight, wallJumpDistance);
            wallJumpCooldown = 0;
            playerAnim.SetTrigger("Jump");
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
        // ITT VAN A JAVÍTÁS: Csak a KillZone-t figyeli, semmi mást!
        if (collision.gameObject.name == "KillZone" && !isDead)
        {
            StartCoroutine(DieAndRespawn());
        }

        if (collision.CompareTag("Checkpoint"))
        {
            checkpointPos = collision.transform.position + new Vector3(0, 1f, 0);
            if (checkpointSound != null)
            {
                audioSource.PlayOneShot(checkpointSound);
            }
            Debug.Log("Checkpoint mentve!");
        }

        if (collision.CompareTag("Ladder") && !isDead)
        {
            isClimbing = true;
        }

        if (collision.CompareTag("NextLevel") && !isDead)
        {
            LoadNextLevel();
            Debug.Log("Bementem a kapuba!");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isClimbing = false;
            playerBody.gravityScale = 3;
        }
    }

    private void Attack()
    {
        playerAnim.SetTrigger("Attack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            IDamageAble idamageable = enemy.GetComponent<IDamageAble>();
            if (idamageable != null)
            {
                idamageable.Damage(damageAmount);
            }
            Debug.Log("Eltaláltuk: " + enemy.name);
        }
    }

    IEnumerator DieAndRespawn()
    {
        isDead = true;

        playerAnim.SetTrigger("Death");
        playerBody.linearVelocity = Vector2.zero;
        playerBody.bodyType = RigidbodyType2D.Static;

        yield return new WaitForSeconds(1f);

        transform.position = checkpointPos;

        playerBody.bodyType = RigidbodyType2D.Dynamic;
        playerBody.gravityScale = 3;
        isClimbing = false;
        isDead = false;
        playerAnim.Rebind();
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    public void LoadNextLevel()
    {
        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        transition.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(levelIndex);
    }
}