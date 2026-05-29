using System;
using UnityEngine;

public class GizmoIndicator : MonoBehaviour
{
    [SerializeField] private float radius = 0.4f;
    [SerializeField] private Color color = new Color(0.01568627f, 1, 0.2099669f);
    [SerializeField] private bool usePosAndScaleOfObject = false;
    [SerializeField] private bool drawFilled = true;
    [SerializeField] private bool drawWire = false;
    [SerializeField] private bool drawOnlyOnSelect = false;
    [SerializeField] private bool drawForwardLine = false;


    public enum ShapeType
    {
        Cube,
        Sphere
    }

    [SerializeField] private ShapeType shape;

    private void OnDrawGizmos()
    {
        if(drawOnlyOnSelect)
            return;

        if(drawForwardLine)
        {
            Vector3 start = transform.position;
            Vector3 end = start + transform.forward * 2f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.05f);
        }

        Gizmos.color = color;  // set color

        // draw filled gizmos
        if(drawFilled)
        {
            if(usePosAndScaleOfObject)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                if(shape == ShapeType.Cube)
                    Gizmos.DrawCube(Vector3.zero, Vector3.one);
                else if(shape == ShapeType.Sphere)
                    Gizmos.DrawSphere(Vector3.zero, 1);
            } else {
                if(shape == ShapeType.Cube)
                    Gizmos.DrawCube(transform.position, new Vector3(radius*2, radius*2, radius*2));
                else if(shape == ShapeType.Sphere)
                    Gizmos.DrawSphere(transform.position, radius);
            }
        }

        // draw filled gizmos
        if(drawWire)
        {
            if(usePosAndScaleOfObject)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                if(shape == ShapeType.Cube)
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                else if(shape == ShapeType.Sphere)
                    Gizmos.DrawWireSphere(Vector3.zero, 1);
            } else {
                if(shape == ShapeType.Cube)
                    Gizmos.DrawWireCube(transform.position, new Vector3(radius*2, radius*2, radius*2));
                else if(shape == ShapeType.Sphere)
                    Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if(!drawOnlyOnSelect)
            return;

        if(drawForwardLine)
        {
            Vector3 start = transform.position;
            Vector3 end = start + transform.forward * 2f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.05f);
        }

        Gizmos.color = color;  // set color

        // draw filled gizmos
        if(drawFilled)
        {
            if(usePosAndScaleOfObject)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                if(shape == ShapeType.Cube)
                    Gizmos.DrawCube(Vector3.zero, Vector3.one);
                else if(shape == ShapeType.Sphere)
                    Gizmos.DrawSphere(Vector3.zero, 1);
            } else {
                if(shape == ShapeType.Cube)
                    Gizmos.DrawCube(transform.position, new Vector3(radius*2, radius*2, radius*2));
                else if(shape == ShapeType.Sphere)
                    Gizmos.DrawSphere(transform.position, radius);
            }
        }

        // draw filled gizmos
        if(drawWire)
        {
            if(usePosAndScaleOfObject)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                if(shape == ShapeType.Cube)
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                else if(shape == ShapeType.Sphere)
                    Gizmos.DrawWireSphere(Vector3.zero, 1);
            } else {
                if(shape == ShapeType.Cube)
                    Gizmos.DrawWireCube(transform.position, new Vector3(radius*2, radius*2, radius*2));
                else if(shape == ShapeType.Sphere)
                    Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }
}