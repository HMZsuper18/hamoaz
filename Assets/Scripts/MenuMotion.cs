using UnityEngine;

public class CameraSway : MonoBehaviour
{
    public float swayDegrees = 1.0f;
    public float swaySpeed = 0.25f;
    Quaternion baseRot;

    void Start() => baseRot = transform.rotation;

    void Update()
    {
        float t = Time.time * swaySpeed;
        transform.rotation = baseRot * Quaternion.Euler(
            Mathf.Sin(t) * swayDegrees,
            Mathf.Cos(t * 0.8f) * swayDegrees,
            0f);
    }
}
