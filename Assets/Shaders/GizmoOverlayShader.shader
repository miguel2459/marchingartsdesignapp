Shader "Custom/GizmoOverlayShader"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0, 0, 1) // Default to red
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay+1" }
        LOD 100

        Pass
        {
            ZTest Always // Ensures rendering above other objects
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color; // Color property

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Directly return the color
                return _Color;
            }
            ENDCG
        }
    }
}
