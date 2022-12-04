using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public partial class VisualObjs
{
    public struct Obj3D
    {
        public Vector3 p;
        public float radius;
    }
    
    public struct ObjectStruct
    {
        public int Id;
        public string Shape;
        public string Material;
        public float Size;
        public Vector3 Position;
    }
    public class SceneStruct
    {
        public int ImageIndex;
        public List<ObjectStruct> Objects;
        public DirectionStruct Directions;

        public SceneStruct(int objNum, int imageIndex)
        {
            Objects = new List<ObjectStruct>(new ObjectStruct[objNum]);
            ImageIndex = imageIndex;
        }
    }

    public class Calibration
    {
        public Matrix4x4 K;
        public Matrix4x4 R;
        public Vector3 t;

        public Calibration(Matrix4x4 KK, Matrix4x4 RR, Vector3 tt)
        {
            K = KK;
            R = RR;
            t = tt;
        }
    }

    public struct DirectionStruct
    {
        public Vector3 Behind;
        public Vector3 Front;
        public Vector3 Left;
        public Vector3 Right;
        public Vector3 Above;
        public Vector3 Below;
    }

    public class DepthMap
    {
        public Color[] depths;
        public int width;
        public int height;
        public float minDepth;
        public float maxDepth;

        public DepthMap(int w, int h)
        {
            depths = new Color[w * h];
            width = w;
            height = h;
            minDepth = (float)Mathf.Infinity;
            maxDepth = (float)0.0;
        }
    }

    public struct Point3D
    {
        public float x;
        public float y;
        public float z;
    }

    public class Rules
    {
        [JsonProperty("target")]
        public ObjProp target;
        
        [JsonProperty("Objs")]
        public List<ObjProp> objs;
    }

    public struct ObjProp
    {
        [JsonProperty("size")]
        private string size;
        
        [JsonProperty("position")]
        private string position;
    }




}
//
// public class Utils
// {
// }