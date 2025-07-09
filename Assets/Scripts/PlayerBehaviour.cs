using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    public static PlayerBehaviour instance;
	[SerializeField] public float _moveSpeed = 5f; // Velocidade de movimento

	 private Rigidbody2D rb;
	private Vector2 movement;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Awake()
    {
        instance = this;
		rb = GetComponent<Rigidbody2D>();
	}  	

	void Update()
	{
		movement.x = Input.GetAxisRaw("Horizontal");
		movement.y = Input.GetAxisRaw("Vertical");
		movement = movement.normalized; 
	}

	void FixedUpdate()
	{
		rb.MovePosition(rb.position + movement * _moveSpeed * Time.fixedDeltaTime);
	}
}
