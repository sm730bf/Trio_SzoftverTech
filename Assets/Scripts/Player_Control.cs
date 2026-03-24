using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Control : MonoBehaviour
{
    Rigidbody2D playerBody;
    Animator playerAnim;
    [SerializeField] private float speed;
    [SerializeField] private float jump;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float airAcceleration = 10f;
    private BoxCollider2D boxCollider;
    private float wallJumpCooldown;
    private float horizontalInput;


    private void Awake()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerAnim = GetComponentInChildren<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();

    }

    // Update is called once per frame
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
            // Note: if onWall() is true and not grounded, velocity was already set to zero above,
            // so we don't apply any additional movement.

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
            if(horizontalInput < 0.01f && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
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

    private bool isGrounded()
    {
        RaycastHit2D raycasHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, Vector2.down, .1f, groundLayer);
        return raycasHit.collider != null;
    }

    private bool onWall()
    {
        RaycastHit2D raycasHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, new Vector2(transform.localScale.x, 0), .1f, wallLayer);
        
        return raycasHit.collider != null;
       
    }
}


