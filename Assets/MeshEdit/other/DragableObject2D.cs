using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DragableObject2D : MonoBehaviour
{
    public bool dragable = true;

    private Vector2 error;

    private void OnMouseDown()
    {
        error = (Vector2)transform.position - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseDrag()
    {
        if (!dragable) return;

        Rigidbody2D rb;
        if (rb = GetComponent<Rigidbody2D>()) rb.velocity = Vector2.zero;

        transform.position = error + (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}
