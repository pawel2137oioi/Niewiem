// COPYRIGHT 2010, 2011, 2013, 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//           P R E C I P I T A T I O N   O B J E C T   S H A D E R            //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 worldViewProjection;  // model -> world -> view -> projection
float4x4 invView;              // inverse view

float3 LightVector; // Direction vector to sun, used for day-night darkening

float particleSize;

float2 cameraTileXZ;
float currentTime;
float4 PrecipitationEnvironment; // strength, daylight, twilight, rain fraction
float PrecipitationVisibility;
#include "WeatherLighting.fxh"

static float2 texCoords[4] = { float2(0, 0), float2(1, 0), float2(1, 1), float2(0, 1) };
static float2 offsets[4] = { float2(-0.5f, 0.5f), float2(0.5f, 0.5f), float2(0.5f, -0.5f), float2(-0.5f, -0.5f) };

texture precipitation_Tex;

sampler PrecipitationSamp = sampler_state
{
	texture = (precipitation_Tex);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	MipLodBias = 0;
	AddressU = Wrap;
	AddressV = Wrap;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 StartPosition_StartTime : POSITION0;
	float4 EndPosition_EndTime : POSITION1;
	float4 TileXZ_Vertex : POSITION2;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position	: POSITION;
	float2 TexCoord : TEXCOORD0;
    float Distance : TEXCOORD1;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

// Precipitation vertex shader - calculates raindrop/snowflake positions.
VERTEX_OUTPUT VSPrecipitation(uniform bool Advanced, in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	
	float age = (currentTime - In.StartPosition_StartTime.w) / (In.EndPosition_EndTime.w - In.StartPosition_StartTime.w);
	int vertIdx = (int)In.TileXZ_Vertex.z;
	float3 right = invView[0].xyz;
    float3 motion = In.StartPosition_StartTime.xyz - In.EndPosition_EndTime.xyz;
    float3 up = motion * rsqrt(max(dot(motion,motion),0.000001));
    float2 size = particleSize.xx;
    if (Advanced) {
        float variation = 0.75 + frac(In.StartPosition_StartTime.w * 13.731) * 0.5;
        size *= lerp(float2(0.16,0.16), float2(0.08,0.80), PrecipitationEnvironment.w) * variation;
        if (PrecipitationEnvironment.w < 0.5) up = invView[1].xyz;
    }
	
	In.StartPosition_StartTime.xyz = lerp(In.StartPosition_StartTime.xyz, In.EndPosition_EndTime.xyz, age);
	In.StartPosition_StartTime.xz += (cameraTileXZ - In.TileXZ_Vertex.xy) * float2(-2048, 2048);
	In.StartPosition_StartTime.xyz += right * offsets[vertIdx].x * size.x;
	In.StartPosition_StartTime.xyz += up * offsets[vertIdx].y * size.y;
	
	Out.Position = mul(float4(In.StartPosition_StartTime.xyz, 1), worldViewProjection);
	Out.TexCoord = texCoords[vertIdx];
    Out.Distance = length(In.StartPosition_StartTime.xyz - invView[3].xyz);
	
	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

// This function dims the lighting at night, with a transition period as the sun rises/sets.
void _PSApplyDay2Night(inout float4 Color)
{
	// The following constants define the beginning and the end conditions of the day-night transition
	const float startNightTrans = 0.1; // The "NightTrans" values refer to the Y postion of LightVector
	const float finishNightTrans = -0.1;
	const float minDarknessCoeff = 0.15;

	// Internal variables
	// The following two are used to interpoate between day and night lighting (y = mx + b)
	// Can't use lerp() here, as overall dimming action is too complex
	float slope = (1.0 - minDarknessCoeff) / (startNightTrans - finishNightTrans); // "m"
	float incpt = 1.0 - slope * startNightTrans; // "b"
	// This is the return value used to darken scenery
	float adjustment;

    if (LightVector.y < finishNightTrans)
      adjustment = minDarknessCoeff;
    else if (LightVector.y > startNightTrans)
      adjustment = 0.9; // Scenery is fully lit during the day
    else
      adjustment = slope * LightVector.y + incpt;

	Color.rgb *= adjustment;
}

float4 PSPrecipitation(in VERTEX_OUTPUT In) : COLOR0
{
	float4 color = tex2D(PrecipitationSamp, In.TexCoord);
	_PSApplyDay2Night(color);
	return color;
}

float4 PSPrecipitationAdvanced(in VERTEX_OUTPUT In) : COLOR0
{
    float2 p = In.TexCoord * 2 - 1;
    float rain = exp2(-p.x * p.x * 18) * saturate(1 - abs(p.y));
    float snow = 1 - smoothstep(0.12,1,dot(p,p));
    float liquidity = PrecipitationEnvironment.w;
    float shape = lerp(snow,rain,liquidity);
    float daylight = PrecipitationEnvironment.y;
    float3 tint = lerp(float3(0.065,0.085,0.13),float3(0.90,0.94,1.0),daylight);
    tint *= lerp(1.0,0.72,liquidity);
    tint += WeatherSunTint(LightVector.y) * PrecipitationEnvironment.z * 0.18;
    float fade = smoothstep(0.5,2,In.Distance)
        * exp(-In.Distance / max(20,PrecipitationVisibility))
        * (1 - smoothstep(80,160,In.Distance));
    return float4(tint,shape * fade * lerp(0.85,0.48,liquidity));
}

VERTEX_OUTPUT VSPrecipitationLegacy(in VERTEX_INPUT In) { return VSPrecipitation(false,In); }
VERTEX_OUTPUT VSPrecipitationAdvanced(in VERTEX_INPUT In) { return VSPrecipitation(true,In); }

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique Precipitation
{
	pass Pass_0
	{
		VertexShader = compile vs_4_0_level_9_1 VSPrecipitationLegacy();
		PixelShader = compile ps_4_0_level_9_1 PSPrecipitation();
	}
}

technique PrecipitationAdvanced {
    pass Pass_0 {
        VertexShader = compile vs_4_0 VSPrecipitationAdvanced();
        PixelShader = compile ps_4_0 PSPrecipitationAdvanced();
    }
}
