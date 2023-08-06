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

    [JsonProperty("random_position")]
    public bool IsRandomPosition;
    
    [JsonProperty("scene_type")]
    public string SceneType;
    
    [JsonProperty("scene_num")] public int SceneNum;
    
    [JsonProperty("min_shape_variation")] public int MinShapeVariation;
    [JsonProperty("max_shape_variation")] public int MaxShapeVariation;
    [JsonProperty("min_color_variation")] public int MinColorVariation;
    [JsonProperty("max_color_variation")] public int MaxColorVariation;
    [JsonProperty("shape_type")] public string ShapeType;

    public struct ObjProp
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
    }

    public static Dictionary<string, float> strFloMapping = new()
    {
        { "small", 0.5F },
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