using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace utils
{
    public class SceneJson
    {
        [JsonProperty("scene")] 
        public List<ObjJson> Objs;
        
        [JsonProperty("pred_scene")] 
        public List<ObjJson> PredObjs;
        
        [JsonProperty("file_name")] 
        public SceneConfigJson SceneConfig;
        
        
        public struct ObjJson
        {
            [JsonProperty("id")] public int Id;
            [JsonProperty("color")] public float[] Color;
            [JsonProperty("shape")] public string Shape;
            [JsonProperty("position")] public float[] Pos;
            [JsonProperty("size")] public float Size;
            [JsonProperty("material")] public string Material;
            [JsonProperty("screenPosition")] public float[] screenPos;
            
        }
        
        public struct SceneConfigJson
        {
            [JsonProperty("minDepth")] public float MinDepth;
            [JsonProperty("maxDepth")] public string MaxDepth;
            [JsonProperty("objects")] public List<ObjJson> Objects;
        }
        

    }
}