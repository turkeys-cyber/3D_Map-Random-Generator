using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 moveInput;
    [SerializeField]
    private float moveSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    }

    private void FixedUpdate()
    {
        //rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        rb.velocity = moveInput * moveSpeed;
    }
}
