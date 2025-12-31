Shader "Custom/PixelASCII_BlackGlyphs"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _PixelSize ("Pixel Size", Range(8,512)) = 32
        _Density ("ASCII Density", Range(0,1)) = 0.3
        _NoiseAmount ("Noise Amount", Range(0,1)) = 0.1
        _GlyphScale ("Glyph Scale", Range(0.5,3)) = 1.0
        [Toggle] _DebugGlyphs ("Debug Glyphs", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        float _PixelSize;
        float _Density;
        float _NoiseAmount;
        float _GlyphScale;
        float _DebugGlyphs;
        fixed4 _Color;
        
        struct Input
        {
            float2 uv_MainTex;
        };
        
        // Better pseudo-random function
        float hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }
        
        // ASCII-like glyphs inspired by Japanese/Cyrillic (8 glyphs, 5x3 each = 15 pixels)
        float glyphs[120] = {
            // Glyph 0: ツ (Tsu katakana-inspired)
            1,0,1, 1,0,1, 0,0,0, 0,1,0, 1,0,1,
            // Glyph 1: Я (Cyrillic Ya)
            1,1,1, 1,0,1, 1,1,0, 1,0,1, 1,0,1,
            // Glyph 2: カ (Ka katakana-inspired)
            1,1,1, 0,1,0, 1,0,0, 1,0,0, 1,0,0,
            // Glyph 3: Ж (Cyrillic Zh)
            1,0,1, 1,0,1, 0,1,0, 1,0,1, 1,0,1,
            // Glyph 4: 木 (Tree kanji-inspired)
            0,1,0, 1,1,1, 0,1,0, 1,0,1, 1,0,1,
            // Glyph 5: Д (Cyrillic D)
            1,1,1, 1,0,1, 1,0,1, 1,0,1, 1,1,1,
            // Glyph 6: シ (Shi katakana-inspired)
            1,0,0, 0,1,0, 0,0,1, 1,0,0, 0,1,0,
            // Glyph 7: Ф (Cyrillic F)
            0,1,0, 1,1,1, 1,0,1, 1,1,1, 0,1,0
        };
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Calculate pixelated block position FIRST
            float2 blockPos = floor(IN.uv_MainTex * _PixelSize);
            float2 blockUV = frac(IN.uv_MainTex * _PixelSize);
            
            // Sample texture with pixelated UV
            float2 pixelatedUV = blockPos / _PixelSize;
            fixed4 c = tex2D(_MainTex, pixelatedUV) * _Color;
            
            // Convert to greyscale
            float gray = dot(c.rgb, float3(0.299, 0.587, 0.114));
            
            // Add noise per pixel block (same noise for entire block)
            float noise = hash21(blockPos) * 2.0 - 1.0;
            gray = saturate(gray + noise * _NoiseAmount);
            
            // Random value per block
            float blockRandom = hash21(blockPos);
            
            // Decide if this block gets a glyph
            float hasGlyph = step(blockRandom, _Density);
            
            // Choose random glyph (0-7)
            int glyphIndex = (int)(hash21(blockPos + 0.1) * 7.999);
            glyphIndex = clamp(glyphIndex, 0, 7);
            
            // Calculate position within the glyph (5 rows x 3 cols)
            // blockUV goes from 0 to 1 within each pixel block
            // We want to divide that space into a 3x5 grid for our glyph
            float2 glyphUV = blockUV * float2(30.0, 50.0) / _GlyphScale;
            int gx = (int)floor(glyphUV.x);
            int gy = (int)floor(glyphUV.y);
            
            // Get glyph pixel value
            float glyphValue = 0.0;
            
            // Check if we're within the glyph bounds
            if (gx >= 0 && gx < 3 && gy >= 0 && gy < 5)
            {
                int glyphPixelIndex = gy * 3 + gx;
                int glyphStart = glyphIndex * 15;
                glyphValue = glyphs[glyphStart + glyphPixelIndex];
            }
            
            // Apply glyph as black marks on the surface
            float finalValue = gray;
            
            // Debug mode: show all glyphs in red
            if (_DebugGlyphs > 0.5)
            {
                if (glyphValue > 0.5)
                {
                    o.Albedo = float3(1, 0, 0); // Red for debugging
                    return;
                }
            }
            
            // If this block has a glyph AND we're on a glyph pixel, make it black
            if (hasGlyph > 0.5 && glyphValue > 0.5)
            {
                finalValue = 0.0;  // Black glyph
            }
            
            o.Albedo = finalValue.xxx;
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
