using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class CrushDetector : MonoBehaviour
{
    [Header("Crush Settings")]
    public float crushImpulseThreshold = 8f;
    public float opposingDotThreshold = -0.5f;

    private Rigidbody2D rb;

    private struct ContactData
    {
        public Vector2 normal;
        public float relativeSpeed;
    }

    private readonly List<ContactData> frameContacts = new List<ContactData>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        float speed = collision.relativeVelocity.magnitude;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D c = collision.GetContact(i);

            frameContacts.Add(new ContactData
            {
                normal = c.normal,
                relativeSpeed = speed
            });
        }
    }

    // 2. Einmal pro Physik-Frame auswerten
    void FixedUpdate()
    {
        print(frameContacts.Count);
        if (frameContacts.Count == 0)
            return;

        bool hasOpposing = false;
        float maxPressure = 0f;

        // Druck grob über relative Geschwindigkeit + Anzahl Kontakte
        for (int i = 0; i < frameContacts.Count; i++)
        {
            maxPressure = Mathf.Max(maxPressure, frameContacts[i].relativeSpeed);

            for (int j = i + 1; j < frameContacts.Count; j++)
            {
                float dot = Vector2.Dot(frameContacts[i].normal, frameContacts[j].normal);

                if (dot < opposingDotThreshold)
                {
                    hasOpposing = true;
                }
            }
        }

        if (hasOpposing && maxPressure >= crushImpulseThreshold)
        {
            OnCrushed(maxPressure);
        }

        frameContacts.Clear();
    }

    private void OnCrushed(float force)
    {
        Debug.Log($"CRUSHED detected! Pressure: {force}");
    }
}