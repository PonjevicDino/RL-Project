using UnityEngine;

public class CarController : MonoBehaviour {

    [SerializeField]
    private WheelJoint2D frontTire, backTire;

    [SerializeField]
    private float speed;

    private float movement, moveSpeed, fuel = 1, fuelConsumption = 0.1f;
    public float Fuel { get => fuel; set { fuel = value; } }

    public bool moveStop = false;

    public Vector3 StartPos { get; set; }

    private void Update() {

    }

    private JointMotor2D motor;

    private void Start()
    {
        motor = new JointMotor2D();
        motor.maxMotorTorque = 10000;
        motor.motorSpeed = 0;  // Initial speed is set to zero
        frontTire.motor = motor;
        backTire.motor = motor;
    }

    private void FixedUpdate()
    {
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, StartPos.x, transform.position.x), transform.position.y);

        moveSpeed = Mathf.Clamp(movement * speed, -speed, speed);

        if (moveSpeed.Equals(0) || fuel <= 0)
        {   // Stop the car if no input or no fuel
            frontTire.useMotor = false;
            backTire.useMotor = false;
        }
        else
        {
            frontTire.useMotor = true;
            backTire.useMotor = true;
            motor.motorSpeed = moveSpeed;  // Update only the speed
            frontTire.motor = motor;
            backTire.motor = motor;
        }

        // Stop car speed on game over
        if (GameManager.Instance.isDie && moveStop)
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }

        // Consume fuel based on movement
        fuel -= fuelConsumption * Mathf.Abs(movement) * Time.fixedDeltaTime + 0.00025f;

        // Adjust movement based on button presses
        if (GameManager.Instance.GasBtnPressed)
        {
            movement = Mathf.Clamp(movement + 0.05f, 0f, 1f);
        }
        else if (GameManager.Instance.BrakeBtnPressed)
        {
            movement = Mathf.Clamp(movement - 0.05f, -1f, 0f);
        }
        else
        {
            movement = 0;
        }

        GameManager.Instance.FuelConsume();  // Update fuel usage
    }
}