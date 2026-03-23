using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Control : MonoBehaviour
{
    Rigidbody2D playerBody;
    Animator playerAnim;
    [SerializeField] private float speed;
    [SerializeField] private float jump;
    private bool isGrounded;
    private void Awake()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerAnim = GetComponentInChildren<Animator>();

    }

    // Update is called once per frame
    private void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        playerBody.linearVelocity = new Vector2(horizontalInput * speed, playerBody.linearVelocity.y);

        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        if (Input.GetKey(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        playerAnim.SetBool("Run", horizontalInput != 0);
        playerAnim.SetBool("Grounded", isGrounded);
    }
    private void Jump()
    {
        playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, jump);
        isGrounded = false;
        playerAnim.SetTrigger("Jump");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}


