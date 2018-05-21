// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "UI/WeirdWeb"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = v.texcoord;

                OUT.color = v.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float points[8] = {
                    0.25, 0.5, 
                    0.75, 0.5, 
                    0.5, 0.25, 
                    0.5, 0.75
                };
                
                float2 p0 = float2(IN.texcoord.x, IN.texcoord.y);

                int len = 4;
                float closestDist = 1<<12;
                int closestI = 0;

                for (int i = 0; i < len; i++){
                    float2 p = float2(points[i*2], points[i*2 + 1]);
                    float m = length(p - p0);
                    
                    if (m < closestDist) {
                        closestDist = m;
                        closestI = i;
                    }
                }
                closestI = closestI*2;
                float2 p1 = float2(points[closestI], points[closestI+1]);
                float2 p2 = float2(0, 0);
                if (closestI == 0) { 
                    p2 = float2(points[0], points[1]);
                }
                else if (closestI == len-1) {
                    p2 = float2(points[len*2-2], points[len*2-1]);
                } else {
                    p2 = float2(points[closestI+2], points[closestI+3]);
                }
                /*
                float2 p2 = float2(0, 0);

                if (closestI == 0) { 
                    p2 = float2(points[0], points[1]);
                }
                else if (closestI == len-1) {
                    p2 = float2(points[len*2-2], points[len*2-1]);
                }
                else {
                    float iM1 = (closestI-1)*2;
                    float iP1 = (closestI+1)*2;

                    float2 pM1 = float2(points[iM1], points[iM1 +1]);
                    float2 pP1 = float2(points[iP1], points[iP1 +1]);

                    float distMinusOne = length(p0 - pM1);
                    float distPlusOne = length(p0 - pP1);

                    p2 = distMinusOne < distPlusOne ? pM1 : pP1;
                }
                */
                
                float term1 = (p2.y - p1.y) * p0.x;
                float term2 = (p2.x - p1.x) * p0.y;
                float term3 = p2.x * p1.y;
                float term4 = p2.y * p1.x;
                float dist;
                dist = abs(term1 - term2 + term3 - term4) / length(p1 - p2);
                //dist = length(p1 - p0);
                //dist = abs((p2.x-p1.x)*(p1.y-p0.y) - (p1.x-p0.x)*(p2.y-p1.y)) / length(p1 - p2);

                //if (dist < 0.001) return float4(1, 1, 1, 1);
                if (dist < 0.005) return float4(1, 1, 1, 1);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}