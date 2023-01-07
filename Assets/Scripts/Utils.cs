using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public partial class VisualObjs
{
    public struct Obj3D
    {
        public Vector3 p;
        public float radius;

        public Obj3D(Vector3 p, float radius)
        {
            this.p = p;
            this.radius = radius;
        }
    }

    public class ObjectStruct
    {
        public int Id;
        public string Shape;
        public string Material;
        public float Size;
        public Vector3 Position;


        public ObjectStruct(int id, string shape, string material)
        {
            this.Id = id;
            this.Shape = shape;
            this.Material = material;
        }
    }

    public class SceneStruct
    {
        public List<ObjectStruct> Objects;

        public SceneStruct(int objNum)
        {
            Objects = new List<ObjectStruct>(new ObjectStruct[objNum]);
            
        }
    }

    // public class Calibration
    // {
    //     public Matrix4x4 K;
    //     public Matrix4x4 R;
    //     public Vector3 t;
    //
    //     public Calibration(Matrix4x4 KK, Matrix4x4 RR, Vector3 tt)
    //     {
    //         K = KK;
    //         R = RR;
    //         t = tt;
    //     }
    // }

    public struct DirectionStruct
    {
        public Vector3 Behind;
        public Vector3 Front;
        public Vector3 Left;
        public Vector3 Right;
        public Vector3 Above;
        public Vector3 Below;
    }

    // public class DepthMap
    // {
    //     public Color[] depths;
    //     public int width;
    //     public int height;
    //     public float minDepth;
    //     public float maxDepth;
    //
    //     public DepthMap(int w, int h)
    //     {
    //         depths = new Color[w * h];
    //         width = w;
    //         height = h;
    //         minDepth = (float)Mathf.Infinity;
    //         maxDepth = (float)0.0;
    //     }
    // }

    public struct Point3D
    {
        public float x;
        public float y;
        public float z;
    }

    public class Rules
    {
        [JsonProperty("objs")] 
        public List<ObjProp> Objs;
        
        [JsonProperty("rule_objs_per_scene")] 
        public int RuleObjPerScene;

        [JsonProperty("random_objs_per_scene")]
        public int RandomObjPerScene;

        [JsonProperty("train_num")] public int TrainNum;
        [JsonProperty("test_num")] public int TestNum;
        [JsonProperty("val_num")] public int ValNum;
    }

    public struct ObjProp
    {
        [JsonProperty("size")] public string size;
        [JsonProperty("shape")] public string Shape;
        [JsonProperty("material")] public string Material;
        [JsonProperty("x")] public float x;
        [JsonProperty("y")] public float y;
        [JsonProperty("z")] public float z;
    }

    private Dictionary<string, float> strFloMapping = new()
    {
        { "small", 0.4F },
        { "big", 0.7F },

        { "cube", 0 },
        { "sphere", 1 },

        { "matt_orange", 6 },
    };

    private Dictionary<string, string> strStrMapping = new()
    {
        { "cube_model", "cube" },
        { "sphere_model", "sphere" }
    };
}