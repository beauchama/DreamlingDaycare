Shader "Hidden/ProtoSprite/GPUDrawLine"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            int _ProtoSprite_BrushSize;
            int2 _ProtoSprite_CursorPixel;
            int2 _ProtoSprite_PreviousCursorPixel;
            float4 _ProtoSprite_Color;
            float4 _ProtoSprite_SpriteRect;
            int _ProtoSprite_BrushShape;

            float2 VectorToSegment(float2 p, float2 v, float2 w)
            {
                // Return minimum distance between line segment vw and point p
                float l2 = length(w-v) * length(w-v);  // i.e. |w-v|^2 -  avoid a sqrt
                if (l2 == 0.0) return p - v;   // v == w case

                // Consider the line extending the segment, parameterized as v + t (w - v).
                // We find projection of point p onto the line. 
                // It falls where t = [(p-v) . (w-v)] / |w-v|^2
                // We clamp t from [0,1] to handle points outside the segment vw.
                float t = max(0, min(1, dot(p - v, w - v) / l2));
                float2 projection = v + t * (w - v);  // Projection falls on the segment
                return p - projection;
            }

            float DistanceToSegment(float2 p, float2 segmentStart, float2 segmentEnd)
            {
                return length(VectorToSegment(p, segmentStart, segmentEnd));
            }

            float2 CalculateClosestPointOnLine(float2 target, float2 p1, float2 p2)
            {
                float2 lineDirection = p2 - p1;
                float lineLength = length(lineDirection);

                // Ensure the line has a length greater than zero
                if (lineLength > 0)
                {
                    lineDirection /= lineLength;

                    // Calculate the projection of T onto the line
                    float dotProduct = dot(target - p1, lineDirection);
                    //dotProduct = clamp(dotProduct, 0, lineLength);

                    return (p1 + lineDirection * dotProduct);
                }

                // If the line has zero length, return one of the line endpoints
                return p1;
            }

            float CalculateDistanceToClosestPointOnLine(float2 target, float2 p1, float2 p2)
            {
                float2 p = CalculateClosestPointOnLine(target, p1, p2);

                return length(p - target);
            }


            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                int2 textureSize = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);

                float2 pixel = (int2)(i.uv * textureSize) + float2(0.5, 0.5);
                
                float4 pixelRect = _ProtoSprite_SpriteRect;

                if (pixel.x < pixelRect.x || pixel.y < pixelRect.y || pixel.x >= pixelRect.z || pixel.y >= pixelRect.w)
                    return col;

                float2 mousePixel = _ProtoSprite_CursorPixel;
                float2 previousMousePixel = _ProtoSprite_PreviousCursorPixel;

                float radius = _ProtoSprite_BrushSize * 0.5;

                if (_ProtoSprite_BrushSize % 2 == 1)
                {
                    mousePixel += float2(0.5, 0.5);
                    previousMousePixel += float2(0.5,0.5);
                }

                if (_ProtoSprite_BrushShape == 0) // Circle
                {
                    // Radius offset to get nicer 3 pixel circle brush size as a cross shape rather than a 3x3 square
                    if (_ProtoSprite_BrushSize == 3)
                        radius -= 0.1f;

                    float distance = DistanceToSegment(pixel, mousePixel, previousMousePixel);

                    if (distance < radius)
                    {
                        col = _ProtoSprite_Color;
                    }
                }
                else // Square
                {
                    float2 offset = float2(radius, radius);

                    if ((previousMousePixel.x > mousePixel.x && previousMousePixel.y > mousePixel.y)
                        || (previousMousePixel.x < mousePixel.x && previousMousePixel.y < mousePixel.y))
                    {
                        offset = float2(-radius, radius);
                    }

                    float2 p1 = previousMousePixel - offset;
                    float2 p2 = mousePixel - offset;

                    float2 p3 = previousMousePixel + offset;
                    float2 p4 = mousePixel + offset;

                    // If pixel is between lines 1-2 and 3-4 then color it
                    float2 pixelOnLine1 = CalculateClosestPointOnLine(pixel, p1, p2);
                    float2 pixelOnLine2 = CalculateClosestPointOnLine(pixel, p3, p4);

                    float2 minBound = min(previousMousePixel, mousePixel) - radius;
                    float2 maxBound = max(previousMousePixel, mousePixel) + radius;

                    if ((pixel.y > pixelOnLine1.y && pixel.y < pixelOnLine2.y) || (pixel.x > pixelOnLine1.x && pixel.x < pixelOnLine2.x))
                    {
                        if (pixel.x > minBound.x && pixel.x < maxBound.x && pixel.y > minBound.y && pixel.y < maxBound.y)
                            col = _ProtoSprite_Color;
                    }
                }
                
                return col;
            }
            ENDCG
        }
    }
}
