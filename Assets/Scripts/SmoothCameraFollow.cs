using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SmoothCameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private float dampening;

    public Transform target;

    private Vector3 velocity = Vector3.zero;
    private void FixedUpdate()
    {
        Vector3 targetposition = target.position + offset;
        targetposition.z = transform.position.z;
        transform.position = Vector3.SmoothDamp(transform.position, targetposition, ref velocity, dampening);
    }
}
