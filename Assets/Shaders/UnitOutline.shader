Shader "Custom/UnitOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,1,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.005
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        // First pass - Draw the outline
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }
            Cull Front
            ZWrite On
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };
            
            uniform float _OutlineWidth;
            uniform float4 _OutlineColor;
            
            v2f vert(appdata v)
            {
                v2f o;
                
                // Expand vertex along normal for outline
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                worldPos.xyz += worldNormal * _OutlineWidth;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.color = _OutlineColor;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
        
        // Second pass - Draw the normal object
        Pass
        {
            Name "BASE"
            Tags { "LightMode" = "ForwardBase" }
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                LIGHTING_COORDS(1,2)
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Simple lighting
                float nl = max(0, dot(normalize(i.normal), _WorldSpaceLightPos0.xyz));
                col.rgb *= nl * 0.8 + 0.2; // Add some ambient
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Diffuse"
}