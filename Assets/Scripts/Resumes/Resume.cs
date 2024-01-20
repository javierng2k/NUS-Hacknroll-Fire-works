using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Resume : MonoBehaviour
{
    [Header("Data")]
    Candidate m_Data;

    [Header("Clicking")]
    public float m_ClickTimeThreshold = 0.2f;
    private float m_InitialClickTime = 0.0f;
    private bool m_IsClick = true;

    private Vector3 m_PaperToMouseOffset;
    private float m_InitialZCoord;
    private Vector3 m_PrevMousePos = Vector3.zero;

    [Header("Physics")]
    public float m_MaxThrowVelocity = 30.0f;
    private Rigidbody m_rb;
    private Quaternion m_InitialRotation = Quaternion.identity;

    // flying to table
    private Vector3 m_FlyDir = Vector3.zero;
    private Vector3 m_TablePos = Vector3.zero;
    private float m_FlySpeed = 1.0f;

    [Header("Zoom state")]
    public float m_MaxZoomSpeed = 5.0f;
    public float m_ZoomRotationAngle = 720.0f;
    [HideInInspector] private bool m_ZoomAnimationPlaying = false;
    private Vector3 m_BeforeZoomPos = Vector3.zero;

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_InitialRotation = transform.rotation;
    }
    public void Initialize(Candidate data, Vector3 spawnPos, Vector3 landPos, float flySpeed)
    {
        m_Data = data;

        m_FlySpeed = flySpeed;

        // need to lerp to a position
        transform.position = spawnPos;
        transform.rotation = m_InitialRotation;

        m_TablePos = landPos;
        m_FlyDir = (landPos - spawnPos).normalized;
        StartCoroutine(FlyTowardsTable());
    }
    float CalculateDecleration(float initialVelocity, Vector3 finalPosition, Vector3 initialPosition)
    {
        float finalVelocity = 0.0f; // Final velocity is 0, as you want to come to a stop
        float distance = Vector3.Distance(finalPosition, initialPosition);

        // Calculate deceleration using the formula
        float deceleration = (finalVelocity * finalVelocity - initialVelocity * initialVelocity) / (2 * distance);

        return deceleration;
    }
    // Have decelaration, different from flying to position
    IEnumerator FlyTowardsTable()
    {
        m_rb.isKinematic = true;

        float deceleration = CalculateDecleration(m_FlySpeed, transform.position, m_TablePos);
        while (Vector3.Dot(m_FlyDir, (transform.position - m_TablePos).normalized) < 0)
        {
            // Move towards the target position
            m_FlySpeed += deceleration * Time.deltaTime;
            m_FlySpeed = Math.Max(m_FlySpeed, 3.0f);
            transform.position += m_FlySpeed * m_FlyDir * Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }

        m_rb.isKinematic = false;
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = m_InitialZCoord;

        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    void OnMouseDown()
    {
        m_InitialClickTime = Time.time;
        m_IsClick = true;

        m_InitialZCoord = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;
        m_PaperToMouseOffset = gameObject.transform.position - GetMouseAsWorldPoint();
    }

    void OnMouseDrag()
    {
        // if there is a resume that is currently focused, ignore this
        if (ResumeController.Instance.m_CurrResumeFocused != null)
            return;

        if (m_IsClick && Time.time - m_InitialClickTime < m_ClickTimeThreshold)
            return;

        m_IsClick = false;
        m_PrevMousePos = Input.mousePosition;
        transform.position = GetMouseAsWorldPoint() + m_PaperToMouseOffset;
        gameObject.transform.position = new Vector3(
            gameObject.transform.position.x,
            ResumeController.Instance.m_GrabYMinPos,
            gameObject.transform.position.z
        );
    }
    private void OnMouseUp()
    {
        if (m_IsClick)
        {
            if (ResumeController.Instance.m_CurrResumeFocused == this)
                return;

            ZoomUp();
            return;
        }

        // toss object
        Vector3 vel = Input.mousePosition - m_PrevMousePos;
        vel = new Vector3(vel.x, 0.0f, vel.y);
        Vector3 dir = vel.normalized;
        float magnitude = Mathf.Max(Vector3.Magnitude(vel), m_MaxThrowVelocity);

        m_rb.AddForce(magnitude * dir, ForceMode.Impulse);

        // reset 
        m_IsClick = true;
    }


    //TODO: if currently zoomed in on a resume, cannot drag ard
    private void ZoomUp()
    {
        // check if can change global state
        if (!ResumeController.Instance.SetCurrResumeFocused(this))
            return;

        //store old position
        m_rb.isKinematic = true;

        m_BeforeZoomPos = transform.position;

        // lerp to screen
        StartCoroutine(ZoomInAnimation());
    }

    public bool ZoomOut()
    {
        // if havent zoom finish yet, cannot zoom out
        if (m_ZoomAnimationPlaying)
            return false;

        m_rb.isKinematic = false;
        transform.position = new Vector3(m_BeforeZoomPos.x, ResumeController.Instance.m_GrabYMinPos, m_BeforeZoomPos.z);
        return true;
    }

    IEnumerator ZoomInAnimation()
    {
        m_ZoomAnimationPlaying = true;

        // Calculate the total distance to move
        Vector3 targetPos = ResumeController.Instance.m_FinalZoomPos.position;
        float totalDistance = Vector3.Distance(transform.position, targetPos);

        float currRotationAngle = 0.0f;

        // Move and rotate until reaching the target position
        float distanceLeft = 0.0f;
        Vector3 initialPos = transform.position;
        float lerpTime = 0.0f;
        while (lerpTime < 1.0f)
        {
            // Lerp to position
            lerpTime += Time.deltaTime * m_MaxZoomSpeed;
            transform.position = Vector3.Lerp(initialPos, targetPos, lerpTime);
            distanceLeft = Vector3.Distance(transform.position, targetPos);

            // Calculate the rotation for this frame
            float newRotation = Mathf.Lerp(0.0f, m_ZoomRotationAngle, distanceLeft / totalDistance);
            float rotationOffset = newRotation - currRotationAngle;
            currRotationAngle = newRotation;

            // Rotate the object
            transform.Rotate(Vector3.up, rotationOffset);

            // Check if we've completed angle to rotate
            if (currRotationAngle >= m_ZoomRotationAngle)
            {
                transform.rotation = m_InitialRotation; // reset rotation to initial angle
            }

            yield return null;
        }

        // Reached destination
        transform.rotation = m_InitialRotation;
        transform.position = targetPos;

        m_ZoomAnimationPlaying = false;
    }
}
