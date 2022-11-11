Shader "Render Depth" {
    SubShader {
    Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 vertex : POSITION;
                float depth : TEXCOORD0;
            };

            v2f vert (appdata_base v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // UNITY_MATRIX_MV := current model * view matrix
                // the minDepth is 0, other depths below than 0.
                // _ProjectionParams.w := 1/FarPlane
                o.depth = -mul( UNITY_MATRIX_MV, v.vertex ).z * _ProjectionParams.w; 
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                return float4( 1 - i.depth, 1 - i.depth, 1 - i.depth, 1);
            }
            ENDCG
        }
    }
}