using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class InputScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Animator anim;
    private Camera cam;
    private CharacterController controller;

    private Vector3 desiredMoveDirection;
    private Vector3 moveVector;

    public Vector2 moveAxis;
    private float verticalVel;

    [Header("Settings")]
    [SerializeField] float movementSpeed;
    [SerializeField] float rotationSpeed = 0.1f;
    [SerializeField] float fallSpeed = 1f;
    public float acceleration = 2;

    [Header("Booleans")]
    [SerializeField] bool blockRotationPlayer;
    private bool isGrounded;


    void Start()
    {
        anim = this.GetComponent<Animator>();
        cam = Camera.main;
        controller = this.GetComponent<CharacterController>();
    }

    void Update()
    {
        InputMagnitude();

        isGrounded = controller.isGrounded;

        if (controller.isGrounded && verticalVel < 0f)
            verticalVel = -2f;
        else
            verticalVel += Physics.gravity.y * Time.deltaTime;

        controller.Move(Vector3.up * verticalVel * fallSpeed * Time.deltaTime);
    }

    void PlayerMoveAndRotation()
    {
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        desiredMoveDirection = forward * moveAxis.y + right * moveAxis.x;

        if (!blockRotationPlayer)
        {
            if (desiredMoveDirection.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(desiredMoveDirection),
                    rotationSpeed * acceleration);

                controller.Move(desiredMoveDirection * Time.deltaTime * (movementSpeed * acceleration));
               
            }
        }
        else
        {
            Vector3 strafeDirection =
                transform.forward * moveAxis.y + transform.right * moveAxis.x;

            controller.Move(strafeDirection * Time.deltaTime * (movementSpeed * acceleration));
        }
    }

    public void LookAt(Vector3 pos)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), rotationSpeed);
    }

    public void RotateToCamera(Transform t)
    {
        var forward = cam.transform.forward;

        desiredMoveDirection = forward;
        Quaternion lookAtRotation = Quaternion.LookRotation(desiredMoveDirection);
        Quaternion lookAtRotationOnly_Y = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        t.rotation = Quaternion.Slerp(transform.rotation, lookAtRotationOnly_Y, rotationSpeed);
    }

    void InputMagnitude()
    {
        float inputMagnitude = moveAxis.magnitude;

        if (inputMagnitude > 0.1f)
            PlayerMoveAndRotation();

        anim.SetFloat("InputMagnitude", inputMagnitude * acceleration, 0.1f, Time.deltaTime);
    }

    #region Input

    public void OnMove(InputValue value)
    {
        moveAxis.x = value.Get<Vector2>().x;
        moveAxis.y = value.Get<Vector2>().y;
    }

    #endregion

    private void OnDisable()
    {
        anim.SetFloat("InputMagnitude", 0);
    }
}
