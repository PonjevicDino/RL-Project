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

    private JointMotor2D motor;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = this.transform.parent.Find("GameManager").GetComponent<GameManager>();

        motor = new JointMotor2D();
        motor.maxMotorTorque = 10000;
        motor.motorSpeed = 0;  // Initial speed is set to zero
        frontTire.motor = motor;
        backTire.motor = motor;
        StartPos = new Vector3(3.0f, StartPos.y, StartPos.z);
    }

    private void FixedUpdate()
    {
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, StartPos.x, transform.position.x), transform.position.y);

        // Stop car speed on game over
        if (gameManager.isDie && moveStop)
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }

        // Consume fuel based on movement
        fuel -= fuelConsumption * Mathf.Abs(movement) * Time.fixedDeltaTime + 0.00025f;

        // Adjust movement based on button presses
        if (gameManager.BrakeBtnPressed)
        {
            movement = Mathf.Clamp(movement - 0.05f, -1f, 0f);
        }
        else if (gameManager.GasBtnPressed)
        {
            movement = Mathf.Clamp(movement + 0.05f, 0f, 1f);
        }
        else
        {
            movement = 0;
        }


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

        gameManager.FuelConsume();  // Update fuel usage
    }
}