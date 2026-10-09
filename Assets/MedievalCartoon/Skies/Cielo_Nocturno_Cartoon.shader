Shader "Medieval Cartoon/Night Sky"
{
    Properties
    {
        _ZenithColor ("Azul oscuro del cielo", Color) = (.025,.035,.085,1)
        _HorizonColor ("Gris azulado del horizonte", Color) = (.20,.24,.34,1)
        _CloudColor ("Color de nubes", Color) = (.10,.13,.20,1)
        _MoonColor ("Color de luna", Color) = (.83,.89,1,1)
        _MoonDirection ("Direccion de la luna", Vector) = (0,.6,1,0)
        _MoonSize ("Radio de luna en grados", Range(.3,8)) = 3.2
        _MoonGlow ("Halo lunar", Range(0,2)) = .45
        _Stars ("Cantidad de estrellas", Range(0,1)) = .6
        _StarBrightness ("Brillo de estrellas", Range(0,3)) = .9
        _Clouds ("Cantidad de nubes", Range(0,1)) = .35
        _CloudScale ("Tamano de nubes", Range(1,16)) = 5
        _SkyRotation ("Rotacion del cielo", Range(0,360)) = 0
        _Exposure ("Brillo del cielo", Range(.1,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            float4 _ZenithColor, _HorizonColor, _CloudColor, _MoonColor, _MoonDirection;
            float _MoonSize, _MoonGlow, _Stars, _StarBrightness, _Clouds, _CloudScale, _SkyRotation, _Exposure;
            v2f vert(appdata v)
            {
                v2f o; o.position = UnityObjectToClipPos(v.vertex); o.direction = v.vertex.xyz; return o;
            }
            float hash(float3 p) { return frac(sin(dot(p, float3(127.1,311.7,74.7))) * 43758.5453); }
            float noise(float3 p)
            {
                float3 i = floor(p), f = frac(p); f = f*f*(3-2*f);
                return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
            }
            float crater(float2 p, float2 center, float radius)
            {
                float distance = length(p-center); return 1-smoothstep(radius*.82,radius,distance);
            }
            half4 frag(v2f i) : SV_Target
            {
                float3 direction = normalize(i.direction);
                float radians = _SkyRotation * UNITY_PI/180;
                float3 sky = float3(cos(radians)*direction.x-sin(radians)*direction.z,direction.y,sin(radians)*direction.x+cos(radians)*direction.z);
                float height = saturate(direction.y);
                float3 color = lerp(_HorizonColor.rgb,_ZenithColor.rgb,pow(height,.45));
                // Slightly faceted, quiet clouds rather than realistic photographic clouds.
                float n = noise(sky*_CloudScale+float3(8,17,4))*.75 + noise(sky*_CloudScale*2+float3(1,3,5))*.25;
                float cloud = smoothstep(1-_Clouds*.68,1-_Clouds*.68+.11,n) * smoothstep(-.04,.12,direction.y);
                float3 cloudColor = _CloudColor.rgb * (.75+floor(n*4)*.15);
                color = lerp(color,cloudColor,cloud*.85);
                // A direction-space grid gives stars without a longitude seam at the horizon.
                float3 grid = sky*180;
                float3 cell = floor(grid), local = frac(grid)-.5;
                float seed = hash(cell);
                float radius = lerp(.06,.14,hash(cell+19));
                float aa = max(length(fwidth(grid))*.28,.01);
                float star = (1-smoothstep(radius,radius+aa,length(local))) * step(1-_Stars*.018,seed);
                color += float3(.72,.80,1) * star * _StarBrightness * smoothstep(.01,.16,height) * (1-cloud);
                float3 moon = normalize(_MoonDirection.xyz);
                float3 right = normalize(cross(abs(moon.y)>.99 ? float3(0,0,1) : float3(0,1,0),moon));
                float3 up = cross(moon,right);
                float radiusRadians = max(.0001,_MoonSize*UNITY_PI/180);
                float2 p = float2(dot(direction,right),dot(direction,up))/sin(radiusRadians);
                float angle = atan2(p.y,p.x);
                float polygonRadius = cos(UNITY_PI/12)/cos(fmod(angle+UNITY_PI*3,UNITY_PI/6)-UNITY_PI/12);
                float edge = max(fwidth(length(p)),.003);
                float disc = (1-smoothstep(polygonRadius-edge,polygonRadius+edge,length(p))) * step(0,dot(direction,moon));
                float marks = max(crater(p,float2(-.28,.28),.25),max(crater(p,float2(.27,-.14),.32),crater(p,float2(-.20,-.43),.16)));
                float shade = saturate(.85+.12*p.y-.1*p.x)-marks*.13;
                float glow = exp(-max(0,(1-dot(direction,moon))/(radiusRadians*radiusRadians))*.65)*_MoonGlow;
                color += _MoonColor.rgb * glow * (1-disc) *.3;
                color = lerp(color,_MoonColor.rgb*shade,disc);
                return half4(color*_Exposure,1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
