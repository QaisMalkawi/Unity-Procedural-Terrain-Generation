using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(InputManager))]
public class RBPlayer : MonoBehaviour
{

    [SerializeField] float speed, runMultiplier, lookingSpeed, jumpHeight, groundRadius, feetHeight;
    [SerializeField] Vector2 CamLock;
    [SerializeField] LayerMask walkable;


    InputManager input;
    Rigidbody rb;
    Vector3 velocity;
    Vector2 lookInput;
    float xInput, yInput, xRotation;
    bool isGrounded;
    Camera camera;

    void Start()
    {
        input = GetComponent<InputManager>();
        rb = GetComponent<Rigidbody>();
        camera = GetComponentInChildren<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics.CheckSphere(transform.position - (transform.up * feetHeight), groundRadius, walkable, QueryTriggerInteraction.Ignore);
        if (input.hasControl)
        {
            xInput = input.move.x;
            yInput = input.move.y;
            Move();
            Look();
        }
    }

    void Move()
    {
        if (isGrounded)
        {
            velocity = new Vector3(xInput, 0, yInput).normalized * (speed * (input.sprint ? runMultiplier : 1));

            rb.AddForceAtPosition(transform.right * velocity.x + transform.forward * velocity.z, transform.position);

            rb.velocity = Vector3.ClampMagnitude(rb.velocity, speed * (input.sprint ? runMultiplier : 1));

            if (input.jump)
            {
                rb.velocity += Vector3.up * jumpHeight;
                input.jump = false;
            }
        }
        else
        {
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, speed * runMultiplier);
            input.jump = false;
        }

        if (xInput == 0 && yInput == 0)
        {
            rb.velocity = new Vector3(Mathf.Lerp(rb.velocity.x, 0, 5 * Time.deltaTime), rb.velocity.y, Mathf.Lerp(rb.velocity.z, 0, 5 * Time.deltaTime));
        }
    }

    void Look()
    {
        lookInput = input.look;

        rb.transform.Rotate(0, lookInput.x * lookingSpeed * Time.deltaTime, 0);
        camera.transform.Rotate(lookInput.y * lookingSpeed * Time.deltaTime, 0, 0);

        xRotation += lookInput.y * lookingSpeed * Time.deltaTime;
        xRotation = ClampAngle(xRotation, CamLock.x, CamLock.y);
        camera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position - (transform.up * feetHeight), groundRadius);
    }
    static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}