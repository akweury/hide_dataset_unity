using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectStruct
{
    public int Id;
    public string Shape;
    public string Material;
    public float Size;
    public Vector3 Position;


    public ObjectStruct(int id, string shape, string material, float size)
    {
        this.Id = id;
        this.Shape = shape;
        this.Material = material;
        this.Size = size;
    }
}
