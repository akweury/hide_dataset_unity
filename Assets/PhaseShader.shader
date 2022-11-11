Shader "Render Phase" {
  SubShader {
  Tags { "RenderType" = "Opaque" } 
        Pass {    
        Tags { "LightMode" = "ForwardAdd" } 
        CGPROGRAM
 
        #pragma vertex vert  
        #pragma fragment frag 
        #include "UnityCG.cginc"

        uniform float4 _LightColor0; 
        uniform float4x4 unity_WorldToLight; 
        uniform sampler2D _LightTexture0; 
                
        struct vertexInput {
          float4 vertex : POSITION;
        };
        struct vertexOutput {
          float4 pos : SV_POSITION;
          float4 posWorld : TEXCOORD0;// position of the vertex (and fragment) in world space 
          float4 posLight : TEXCOORD1;// position of the vertex (and fragment) in light space
        };
 
        vertexOutput vert(vertexInput input) 
        {
          vertexOutput output;
          float4x4 modelMatrix = unity_ObjectToWorld;
          output.posWorld = mul(modelMatrix, input.vertex);
          output.posLight = mul(unity_WorldToLight, output.posWorld);
          output.pos = UnityObjectToClipPos(input.vertex);
          return output;
        }
 
        float4 frag(vertexOutput input) : COLOR
        {
          float cookieAttenuation = tex2D(_LightTexture0, input.posLight.xy / input.posLight.w + float2(0.5, 0.5)).a;
          return float4(cookieAttenuation*float3(1.0, 1.0, 1.0), 1.0);           
        }
        ENDCG
      }
   }
}