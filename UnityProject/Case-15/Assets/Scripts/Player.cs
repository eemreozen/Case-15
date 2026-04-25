using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // GetAxis yerine GetAxisRaw kullanıyoruz. 
        // Bu sayede değerler küsüratlı artmaz; anında -1, 0 veya 1 olur.
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Çapraz hareketin normalden hızlı olmasını engeller
        movement.Normalize();
    }

    void FixedUpdate()
    {
        // Hareket uygulama kısmı aynı kalıyor
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }
}