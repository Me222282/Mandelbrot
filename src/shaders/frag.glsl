#version 450 core

layout(location = 0) out vec4 colour;

in vec2 tex_Coords;

uniform dvec2 uScale;
uniform dvec2 uOffset;
uniform int uMaxIter;

vec3 spectral_color(float l)        // RGB <0,1> <- lambda l <400,700> [nm]
{
    float t;  vec3 c=vec3(0.0,0.0,0.0);
            if ((l>=400.0)&&(l<410.0)) { t=(l-400.0)/(410.0-400.0); c.r=    +(0.33*t)-(0.20*t*t); }
    else if ((l>=410.0)&&(l<475.0)) { t=(l-410.0)/(475.0-410.0); c.r=0.14         -(0.13*t*t); }
    else if ((l>=545.0)&&(l<595.0)) { t=(l-545.0)/(595.0-545.0); c.r=    +(1.98*t)-(     t*t); }
    else if ((l>=595.0)&&(l<650.0)) { t=(l-595.0)/(650.0-595.0); c.r=0.98+(0.06*t)-(0.40*t*t); }
    else if ((l>=650.0)&&(l<700.0)) { t=(l-650.0)/(700.0-650.0); c.r=0.65-(0.84*t)+(0.20*t*t); }
            if ((l>=415.0)&&(l<475.0)) { t=(l-415.0)/(475.0-415.0); c.g=             +(0.80*t*t); }
    else if ((l>=475.0)&&(l<590.0)) { t=(l-475.0)/(590.0-475.0); c.g=0.8 +(0.76*t)-(0.80*t*t); }
    else if ((l>=585.0)&&(l<639.0)) { t=(l-585.0)/(639.0-585.0); c.g=0.84-(0.84*t)           ; }
            if ((l>=400.0)&&(l<475.0)) { t=(l-400.0)/(475.0-400.0); c.b=    +(2.20*t)-(1.50*t*t); }
    else if ((l>=475.0)&&(l<560.0)) { t=(l-475.0)/(560.0-475.0); c.b=0.7 -(     t)+(0.30*t*t); }
    return c;
}

float Mandelbrot(dvec2 c)
{
    dvec2 z = dvec2(0.0);
    dvec2 zz = dvec2(0.0);
    
    int i = 0;
    while ((zz.x + zz.y) <= 4.0 && i < uMaxIter)
    {
        z = dvec2(zz.x - zz.y, 2.0 * z.x * z.y) + c;
        zz = dvec2(z.x * z.x, z.y * z.y);
        i++;
    }
    
    return float(i) - log(log(sqrt(float(zz.x) + float(zz.y))) / log(2.0));
    // return i;
}

void main()
{
	float i = Mandelbrot((tex_Coords - uOffset) * uScale);
    
    float q = i / float(uMaxIter);
    q = pow(q, 0.2);
    colour = vec4(spectral_color(400.0 + (300.0 * q)), 1.0);
}