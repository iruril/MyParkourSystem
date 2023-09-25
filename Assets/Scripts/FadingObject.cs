using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadingObject : MonoBehaviour, IEquatable<FadingObject>
{
    public List<Renderer> Renderers = new();
    public Vector3 Position;
    public List<Material> Materials = new();
    public float InitialAlpha { get; private set; }

    private void Awake()
    {
        Position = this.transform.position;

        if(Renderers.Count == 0)
        {
            Renderers.AddRange(this.GetComponentsInChildren<Renderer>());
        }
        foreach (Renderer renderer in Renderers)
        {
            Materials.AddRange(renderer.materials);
        }

        InitialAlpha = Materials[0].color.a;
    }

    public bool Equals(FadingObject other)
    {
        return Position.Equals(other.Position);
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }

}
