Shader "Hidden/ProtoSprite/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GrabPass ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // #0
        Pass
        {
            // No culling or depth
            Cull Off ZWrite Off ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _GrabPass;
            float4 _MainTex_TexelSize;

            fixed4 frag(v2f i) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, i.uv);
                half4 grabPassColor = tex2D(_GrabPass, i.uv);
                half4 outlineColor = float4(0,0,0,0);

                if (texColor.r < 0.1)
                {
                    for (float y = -1; y <= 1; y += 1) {
                        for (float x = -1; x <= 1; x += 1) {
                            float2 offset = float2(x, y) * _MainTex_TexelSize.xy;

                            outlineColor += tex2D(_MainTex, i.uv + offset) * float4(1, 1, 1, 1);
                        }
                    }
                }

                float val = step((grabPassColor.r + grabPassColor.g + grabPassColor.b) / 3.0, 0.5);

                outlineColor.a = outlineColor.r;

                outlineColor.rgb = float3(val, val, val);

                return outlineColor;
        }
        ENDCG
        }

        // #1
        Pass
        {
            // No culling or depth
            Cull Off ZWrite Off ZTest Always

            Tags { "RenderType" = "Opaque" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                return col;
            }
            ENDCG
        }

        // #2
            Pass
        {
            // No culling or depth
            Cull Off
            ZTest Always
            Lighting Off
            ZWrite On

            Tags { "RenderType" = "Opaque" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _SpriteRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = v.uv2;// TRANSFORM_TEX(v.uv2, _MainTex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= col.a;

                float4 checkerColorA = float4(0.5, 0.5, 0.5, 1.0);
                float4 checkerColorB = float4(0.75, 0.75, 0.75, 1.0);

                float pixelAmount = 16;

                float2 pixelCoord = floor(i.uv2 * _SpriteRect.zw);

                float2 Pos = floor(pixelCoord / pixelAmount);
                float PatternMask = (Pos.x + (Pos.y% 2.0)) % 2.0;

                float4 checkerColor = lerp(checkerColorA, checkerColorB, PatternMask);

                // gamma to linear
                #if !UNITY_COLORSPACE_GAMMA
                    checkerColor = pow(checkerColor, 2.2);
                #endif

                //col.rgb = lerp(checkerColor.rgb, col.rgb, col.a);
                col.rgb = checkerColor.rgb * (1.0 - col.a) + col.rgb * col.a;
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}
