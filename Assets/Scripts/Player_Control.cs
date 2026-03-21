using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Control : MonoBehaviour
{
    Rigidbody2D playerBody;
    [SerializeField] private float speed;
    [SerializeField] private float jump;
    private void Awake()
    {
        playerBody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
     playerBody.linearVelocity = new Vector2(Input.GetAxis("Horizontal") * speed, playerBody.linearVelocity.y);

    if(Input.GetKey(KeyCode.Space))
        {
            playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, jump);
        }
    }
}
