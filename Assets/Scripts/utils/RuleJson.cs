using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class RuleJson
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

    public struct ObjProp
    {
        [JsonProperty("size")] public string size;
        [JsonProperty("shape")] public string Shape;
        [JsonProperty("material")] public string Material;
        [JsonProperty("x")] public float x;
        [JsonProperty("y")] public float y;
        [JsonProperty("z")] public float z;
    }

    public static Dictionary<string, float> strFloMapping = new()
    {
        { "small", 0.4F },
        { "big", 0.7F },

        { "cube", 0 },
        { "sphere", 1 },

        { "matt_orange", 6 },
    };

    public static Dictionary<string, string> strStrMapping = new()
    {
        { "cube_model", "cube" },
        { "sphere_model", "sphere" }
    };
}