// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit shader. Simplest possible colored shader.
// - no lighting
// - no lightmap support
// - no texture

Shader "ER/UnlitColor" {
    Properties{
        _Color("Main Color", Color) = (1,1,1,1)
        //新增 记录裁剪框的四个边界的值
        _Area("Area", Vector) = (-10000,-10000,10000,10000)
    }

        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass {
                CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag
                    #pragma target 2.0
                    #pragma multi_compile_fog

                    #include "UnityCG.cginc"

                    //新增，对应上面的_Area
                    float4 _Area;

                    struct appdata_t {
                        float4 vertex : POSITION;
                        UNITY_VERTEX_INPUT_INSTANCE_ID
                    };

                    struct v2f {
                        float4 vertex : SV_POSITION;
                        UNITY_FOG_COORDS(0)
                        UNITY_VERTEX_OUTPUT_STEREO
                        //新增，记录顶点的世界坐标
                        float2 worldPos : TEXCOORD1;
                    };

                    fixed4 _Color;

                    v2f vert(appdata_t v)
                    {
                        v2f o;
                        UNITY_SETUP_INSTANCE_ID(v);
                        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                        o.vertex = UnityObjectToClipPos(v.vertex);
                        UNITY_TRANSFER_FOG(o,o.vertex);

                        //新增，计算顶点的世界坐标
                        o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                        return o;
                    }

                    fixed4 frag(v2f i) : COLOR
                    {
                        fixed4 col = _Color;
                        UNITY_APPLY_FOG(i.fogCoord, col);
                        UNITY_OPAQUE_ALPHA(col.a);
                   
 
                        //新增，判断顶点坐标是否在裁剪框内
                        bool inArea = i.worldPos.x >= _Area.x && i.worldPos.x <= _Area.z && i.worldPos.y >= _Area.y && i.worldPos.y <= _Area.w;
                        if (!inArea) {
                            discard;
                        }

                        return col;
                    }
                ENDCG
            }
    }

}
