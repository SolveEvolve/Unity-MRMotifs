Shader "Custom/StereoscopicHemisphere"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} 
        // Set _TargetEye to 0 for left eye, or 1 for right eye.
        _TargetEye ("Target Eye", Range(0, 1)) = 0  
    }
    SubShader
    {
        // Use the Background queue so it renders behind other objects.
        Tags { "RenderType"="Background" "Queue"="Background" }
        Pass
        {
            // Always render this pass and disable depth writing.
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Enable instancing and single-pass stereo variants.
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            // Include URP core helper functions.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                // Pass the stereo eye index from vertex to fragment.
                float eyeIndex : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _TargetEye; // 0 for left, 1 for right.

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Convert object-space position to clip space.
                OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
                // Transform UVs.
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                // Always use unity_StereoEyeIndex.
                OUT.eyeIndex = unity_StereoEyeIndex;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Read the eye index (0 for left, 1 for right)
                float eyeIndex = IN.eyeIndex;

                // Determine which eye is currently rendering.
                float isLeftEye  = 1.0 - step(0.5, eyeIndex); // 1 if eyeIndex < 0.5, else 0.
                float isRightEye = step(0.5, eyeIndex);         // 1 if eyeIndex >= 0.5, else 0.

                // For the material's target eye:
                // When _TargetEye is 0, targetLeft becomes 1 and targetRight becomes 0.
                // When _TargetEye is 1, targetLeft becomes 0 and targetRight becomes 1.
                float targetLeft  = 1.0 - step(0.5, _TargetEye);
                float targetRight = step(0.5, _TargetEye);

                // Compute the mask value.
                float mask = isLeftEye * targetLeft + isRightEye * targetRight;

                // Discard the fragment if the mask is less than 0.5.
                clip(mask - 0.5);

                // Otherwise, output the texture color.
                return tex2D(_MainTex, IN.uv);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Forward"
}