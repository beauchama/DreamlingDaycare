Shader "Hidden/ProtoSprite/DrawHoverImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ProtoSprite_HoverImageTex ("HoverImageTexture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            sampler2D _ProtoSprite_HoverImageTex;
            float4 _ProtoSprite_HoverImageTex_TexelSize;

            float4 _ProtoSprite_SpriteRect;

            int2 _ProtoSprite_HoverImagePosition;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                int2 textureSize = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);

                float2 pixel = (int2)(i.uv * textureSize);
                
                float4 pixelRect = _ProtoSprite_SpriteRect;

                if (pixel.x < pixelRect.x || pixel.y < pixelRect.y || pixel.x >= pixelRect.z || pixel.y >= pixelRect.w)
                    return col;

                float2 hoverImageUV = ((pixel - _ProtoSprite_HoverImagePosition) + 0.5) / float2(_ProtoSprite_HoverImageTex_TexelSize.z, _ProtoSprite_HoverImageTex_TexelSize.w);

                if (hoverImageUV.x >= 0.0 && hoverImageUV.x < 1.0 && hoverImageUV.y >= 0.0 && hoverImageUV.y < 1.0)
                {
                    float4 hoverImageValue = tex2D(_ProtoSprite_HoverImageTex, hoverImageUV);

                    if (hoverImageValue.a > 0.0)
                    {
                        col = hoverImageValue;
                    }
                }

                return col;
            }
            ENDCG
        }
    }
}
