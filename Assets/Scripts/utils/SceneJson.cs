using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace utils
{
    public class SceneJson
    {
        [JsonProperty("scene")] 
        public List<ObjJson> Objs;
        
        [JsonProperty("file_name")] 
        public SceneConfigJson SceneConfig;
        
        
        public struct ObjJson
        {
            [JsonProperty("id")] public int Id;
            [JsonProperty("color")] public Vector3 Color;
            [JsonProperty("shape")] public string Shape;
            [JsonProperty("position")] public Vector3 Pos;
            [JsonProperty("size")] public float Size;
            [JsonProperty("material")] public string Material;
            
        }
        
        public struct SceneConfigJson
        {
            [JsonProperty("minDepth")] public float MinDepth;
            [JsonProperty("maxDepth")] public string MaxDepth;
            [JsonProperty("objects")] public List<ObjJson> Objects;
        }
        

    }
}