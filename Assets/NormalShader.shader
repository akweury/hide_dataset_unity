Shader "Render Normals" {
    SubShader {
    Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 vertex : POSITION;
                float3 color : COLOR;
            };

            v2f vert (appdata_base v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // transforms a point from object space to camera's clip space in homogeneous coordinates
                half3 worldNormal = UnityObjectToWorldDir(v.normal); 
                half3 viewNormal = mul((float3x3) UNITY_MATRIX_V, worldNormal); // current view matrix
                o.color = viewNormal * 0.5 + 0.5; // scale and bias the normal from range [-1,1] to [0,1]
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                return float4( i.color, 1); // 1 as the alpha component
            }
            ENDCG
        }
    }
}