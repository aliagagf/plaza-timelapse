in vec3 aColor;
in vec4 aPosition;
out vec4 C;
uniform sampler2D iChannel0;
uniform sampler2D iChannel1;
uniform sampler2D iChannel2;
uniform sampler2D iChannel3;
uniform sampler2D iChannel4;
uniform vec2 iResolution;
uniform vec4 iMouse;
uniform float iTime;
uniform int iFrame;

#define PI 3.1459
#define TAU PI*2.0

const vec3 COLOR_BACKGROUND = vec3(0.4, 0.6, 1.0);

float minDist = 0.01;
float maxDist = 100.;
int maxIt = 100;

struct Surface {
    vec3 color;
    float d;
    vec2 uv;
    int materialType;
};

mat4 rotX (in float angle)
{
    float rad = radians (angle);
    float c = cos (rad);
    float s = sin (rad);
    mat4 mat = mat4 (vec4 (1.0, 0.0, 0.0, 0.0),
                     vec4 (0.0,   c,   s, 0.0),
                     vec4 (0.0,  -s,   c, 0.0),
                     vec4 (0.0, 0.0, 0.0, 1.0));
    return mat;
}

mat4 rotY (in float angle)
{
    float rad = radians (angle);
    float c = cos (rad);
    float s = sin (rad);
    mat4 mat = mat4 (vec4(  c, 0.0,  -s, 0.0),
                     vec4(0.0, 1.0, 0.0, 0.0),
                     vec4(  s, 0.0,   c, 0.0),
                     vec4(0.0, 0.0, 0.0, 1.0));
    return mat;
}

mat4 rotZ (in float angle)
{
    float rad = radians (angle);
    float c = cos (rad);
    float s = sin (rad);
    mat4 mat = mat4 (vec4(  c,   s, 0.0, 0.0),
                     vec4( -s,   c, 0.0, 0.0),
                     vec4(0.0, 0.0, 1.0, 0.0),
                     vec4(0.0, 0.0, 0.0, 1.0));
    return mat;
}

vec3 opTransf (vec3 p, mat4 m)
{
    return vec4 (m * vec4 (p, 1.)).xyz;
}

float semicylinderDist(vec3 p, vec4 cr)
{
    float r = cr.w;
    vec3 q = p - cr.xyz;
    float dCyl = length(q.yz) - r;
    float dSemi = max(dCyl, -q.y);
    float l = 4.0;
    float dCap = max(abs(q.x) - l * 0.5, dSemi);
    return dCap;
}

float planeDist(vec3 p,vec4 nd)
{
    return dot(p,nd.xyz)-nd.w;
}

Surface unionS(Surface s1,Surface s2)
{
    return (s1.d<s2.d)? s1:s2;
}
float arcDist(vec3 p, vec4 cr, float angleMin, float angleMax)
{
    float r = cr.w;
    r += 0.3 * pow((0.5 + 0.5 * sin(2.0 * PI * iTime * 1.2)), 4.0);
    vec3 q = p - cr.xyz;
    float dCyl = length(q.xz) - r;
    float h = 2.0;
    float dCap = max(abs(q.y) - h * 0.5, dCyl);
    float angle = atan(q.z, q.x);
    float dAngle = max(angleMin - angle, angle - angleMax);
    float dArc = max(dCap, dAngle);
    return dArc;
}

Surface rotatedSemiCylinder(vec3 p, vec3 centro, float raio, float raioInterno, float angleRad, vec3 color) {
    Surface arco;
    vec3 q = p - centro;
    float x = q.x * cos(angleRad) - q.z * sin(angleRad);
    float z = q.x * sin(angleRad) + q.z * cos(angleRad);
    vec3 pRot = vec3(x, q.y, z) + centro;
    float dOuter = semicylinderDist(pRot, vec4(centro, raio));
    float dInner = semicylinderDist(pRot, vec4(centro, raioInterno));
    arco.d = min(abs(dOuter), abs(dInner)) * sign(dOuter);
    arco.color = color;
    arco.materialType = 3;
    return arco;
}

Surface getRotatedArchExample(vec3 p) {
    vec3 centro = vec3(-20.0, 0.0, 12.0);
    float raio = 3.0;
    float raioInterno = raio - 0.05;
    float rot90 = PI * 0.5;
    vec3 color = vec3(1.0, 0.5, 0.1);
    return rotatedSemiCylinder(p, centro, raio, raioInterno, rot90, color);
}

Surface multiArches(vec3 p) {
    Surface result;
    result.d = 1e5;
    result.color = vec3(0.0);
    float raio = 3;
    float raioInterno = raio - 0.05;
    int nArcos = 4;
    vec3 centros[4];
    centros[0] = vec3(-10.0, -1.0, 0.0);
    centros[1] = vec3(0.0, -1.0, 12.0);
    centros[2] = vec3(20.0, -1.0, 26.0);
    centros[3] = vec3(-5.0, -1.0, 27.0);
    float rot90 = PI * 0.5;
    for (int i = 0; i < nArcos; ++i) {
        Surface arco;
        vec3 centro = centros[i];
        if (i == 1) {
            arco = rotatedSemiCylinder(p, centro, raio, raioInterno, rot90, (i % 2 == 0) ? vec3(1.0, 0.5, 0.1) : vec3(0.1, 0.7, 0.8));
            arco.materialType = 3;
        } else {
            float dOuter = semicylinderDist(p, vec4(centro, raio));
            float dInner = semicylinderDist(p, vec4(centro, raioInterno));
            arco.d = min(abs(dOuter), abs(dInner)) * sign(dOuter);
            arco.color = (i % 2 == 0) ? vec3(1.0, 0.5, 0.1) : vec3(0.1, 0.7, 0.8);
            arco.materialType = 3;
        }
        if (arco.d < result.d) {
            result = arco;
        }
    }
    return result;
}

float rectPathDist(vec3 p, vec2 a, vec2 b, float width) {
    vec2 pa = p.xz - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    vec2 proj = a + h * ba;
    float d = length(p.xz - proj) - width;
    return max(d, abs(p.y));
}

float curvedRoadDist(vec3 p, vec2 center, float radius, float angleStart, float angleEnd, float width) {
    vec2 pos = p.xz - center;
    float ang = atan(pos.y, pos.x);
    float arcDist = abs(length(pos) - radius) - width;
    float angleMask = max(angleStart - ang, ang - angleEnd);
    float d = max(arcDist, angleMask);
    return max(d, abs(p.y));
}

float plazaPathDist(vec3 p) {
    float width = 3;
    vec2 center = vec2(5.0, 0.0);
    float radius = 30.0;
    float angleStart = radians(0.0);
    float angleEnd = radians(90.0);
    float d = curvedRoadDist(p, center, radius, angleStart, angleEnd, width);
    return d;
}

float squareDist(vec3 p, vec2 c, float s) {
    vec2 d = abs(p.xz - c) - vec2(s, s);
    float outside = length(max(d, 0.0));
    float inside = min(max(d.x, d.y), 0.0);
    return max(p.y, outside + inside);
}

float verticalCylinderDist(vec3 p, vec3 center, float radius, float height) {
    vec2 d = abs(vec2(length(p.xz - center.xz), p.y - center.y)) - vec2(radius, height * 0.5);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

Surface getSceneDist(vec3 p)
{
    vec2 lobbyCenter = vec2(0.0, 0.0);
    Surface arches = multiArches(p);
    
    Surface grass;
    grass.color = vec3(0.2, 0.6, 0.2);
    grass.d = planeDist(p, vec4(0.0, 1.0, 0.0, 0.0));
    grass.uv = p.xz * 0.05;
    grass.materialType = 1;

    Surface path;
    path.color = vec3(0.85, 0.8, 0.7);
    float caminho = rectPathDist(p, vec2(8.0, 0.0), vec2(1000.0, 0.0), 2.5);
    path.d = max(p.y, caminho);
    path.uv = p.xz * 0.05;
    path.materialType = 2;

    Surface path3;
    path3.color = vec3(0.85, 0.8, 0.7);
    float caminho3 = rectPathDist(p, vec2(-8.0, 0.0), vec2(-1000.0, 0.0), 2.5);
    path3.d = max(p.y, caminho3);
    path3.uv = p.xz * 0.05;
    path3.materialType = 2;

    Surface path2;
    path2.color = vec3(0.85, 0.8, 0.7);
    float caminho2 = rectPathDist(p, vec2(0.0, 0.0), vec2(0.0, 1000.0), 2.5);
    path2.d = max(p.y, caminho2);
    path2.uv = p.xz * 0.05;
    path2.materialType = 2;

    Surface pathCylinder;
    pathCylinder.color = vec3(0.85, 0.8, 0.7);
    float caminhoCylinder = rectPathDist(p, vec2(-100.0, -21.0), vec2(100.0, -21.0), 10);
    pathCylinder.d = max(p.y, caminhoCylinder);
    pathCylinder.uv = p.xz * 0.05;
    pathCylinder.materialType = 4;

    Surface lobby;
    lobby.color = vec3(0.85, 0.8, 0.7);
    lobby.d = squareDist(p, lobbyCenter, 8.0);
    lobby.uv = p.xz * 0.05;
    lobby.materialType = 2;

    Surface lobbyGrass;
    lobbyGrass.color = vec3(0.2, 0.6, 0.2);
    lobbyGrass.d = squareDist(p, lobbyCenter, 4.0);
    lobbyGrass.uv = p.xz * 0.05;
    lobbyGrass.materialType = 1;

    Surface plazaPath;
    plazaPath.color = vec3(0.85, 0.8, 0.7);
    plazaPath.d = plazaPathDist(p);
    plazaPath.uv = p.xz * 0.05;
    plazaPath.materialType = 2;

    Surface plazaPath2;
    plazaPath2.color = vec3(0.85, 0.8, 0.7);
    float width2 = 3.0;
    vec2 center2 = vec2(8.0, 0.0);
    float radius2 = 30.0;
    float angleStart2 = radians(90.0);
    float angleEnd2 = radians(180.0);
    float d2 = curvedRoadDist(p, center2, radius2, angleStart2, angleEnd2, width2);
    plazaPath2.d = d2;
    plazaPath2.uv = p.xz * 0.05;
    plazaPath2.materialType = 2;

    Surface cylinder;
    cylinder.color = vec3(1.0);
    cylinder.d = verticalCylinderDist(p, vec3(50.0, 2.0, -22.0), 4.0, 100.0);
    cylinder.uv = vec2(0.0);
    cylinder.materialType = 2;

    Surface cuboid;
    cuboid.color = vec3(0.2, 0.2, 0.2);
    vec3 cuboidCenter = vec3(0.0, 0.5, -10.5);
    float cuboidLengthX = 200.0;
    float cuboidSizeY = 0.5;
    float cuboidSizeZ = 1.0;
    vec3 dCuboid = abs(p - cuboidCenter) - vec3(cuboidLengthX * 0.5, cuboidSizeY * 0.5, cuboidSizeZ * 0.5);
    float distCuboid = max(max(dCuboid.x, dCuboid.y), dCuboid.z);
    vec3 openingCenter = vec3(0.0, 0.5, -10.5);
    float openingWidth = 4.0;
    float openingHeight = 0.6;
    float openingDepth = 1.1;
    vec3 dOpening = abs(p - openingCenter) - vec3(openingWidth * 0.5, openingHeight * 0.5, openingDepth * 0.5);
    float distOpening = max(max(dOpening.x, dOpening.y), dOpening.z);
    distCuboid = max(distCuboid, -distOpening);
    cuboid.d = distCuboid;
    cuboid.uv = vec2(0.0);
    cuboid.materialType = 3;

    Surface cuboid2;
    cuboid2.color = vec3(0.2, 0.2, 0.2);
    vec3 cuboidCenter2 = vec3(0.0, 0.5, -30.5);
    vec3 dCuboid2 = abs(p - cuboidCenter2) - vec3(cuboidLengthX * 0.5, cuboidSizeY * 0.5, cuboidSizeZ * 0.5);
    float distCuboid2 = max(max(dCuboid2.x, dCuboid2.y), dCuboid2.z);
    cuboid2.d = distCuboid2;
    cuboid2.uv = vec2(0.0);
    cuboid2.materialType = 3;

    Surface connector;
    connector.color = vec3(0.85, 0.8, 0.7);
    float connectorWidth = 2.0;
    vec2 connectorA = vec2(0.0, -21.0);
    vec2 connectorB = vec2(0.0, -8.0);
    float dConnector = rectPathDist(p, connectorA, connectorB, connectorWidth);
    connector.d = max(p.y, dConnector);
    connector.uv = p.xz * 0.05;
    connector.materialType = 2;

    Surface s = unionS(grass, arches);
    s = unionS(s, path);
    s = unionS(s, path3);
    s = unionS(s, path2);
    s = unionS(s, lobby);
    s = unionS(s, lobbyGrass);
    s = unionS(s, plazaPath);
    s = unionS(s, plazaPath2);
    s = unionS(s, cylinder);
    s = unionS(s, cuboid);
    s = unionS(s, cuboid2);
    s = unionS(s, connector);
    s = unionS(s, pathCylinder);
    return s;
}

Surface rayMarching(vec3 ro,vec3 rd)
{
    int i=0;
    float da=0.0;
    vec3 p = ro+da*rd;
    Surface d_o =getSceneDist(p);
    while ((da<maxDist)&&(d_o.d>minDist)&&(i<maxIt))
    {
        da+=d_o.d;
        p =ro+da*rd;
        d_o =getSceneDist(p);
        i++;
    }
    if((i<maxIt)&&(da<maxDist))
        d_o.d =da;
    else
        d_o.d =  maxDist;
    return d_o;
}

vec3 estimateNormal(vec3 p)
{
    float ep = 0.01;
    float d = getSceneDist(p).d;
    vec3 n =vec3 (d-getSceneDist(vec3(p.x-ep,p.y,p.z)).d,d-getSceneDist(vec3(p.x,p.y-ep,p.z)).d,d-getSceneDist(vec3(p.x,p.y,p.z-ep)).d);
    return normalize(n);
}

vec3 getLight(vec3 p, Surface s, vec3 CamPos)
{
    vec3 n = estimateNormal(p);
    float sunRadius = 40.0;
    float sunHeight = 12.0;
    float sunAngle = iTime * 0.5;
    vec3 lp = vec3(0.0, sunHeight * cos(sunAngle), sunRadius * sin(sunAngle));
    vec3 lColor= vec3(1.0, 0.95, 0.85);
    vec3 ld = normalize(lp-p);
    float r =clamp(dot(ld,n),0.0,1.0);
    float ka =0.3;
    float kd=0.5;
    float ks =0.20;
    float shininess = 10.0;
    vec3 baseColor = s.color;
    vec3 specColor = vec3(1.0);
    if (s.materialType == 1) {
        baseColor = texture(iChannel0, s.uv).rgb;
    }
    if (s.materialType == 2) {
        baseColor = texture(iChannel1, s.uv).rgb;
    }
    if (s.materialType == 3) {
        kd = 0.08;
        ks = 2.0;
        shininess = 120.0;
        specColor = baseColor;
    }
    if (s.materialType == 4) {
        baseColor = texture(iChannel4, s.uv).rgb;
    }
    vec3 eye = normalize(p-CamPos);
    vec3 R =normalize(reflect(n,ld));
    float phi = clamp(dot(R,eye),0.0,1.0);
    vec3 col=baseColor*ka+baseColor*r*kd+specColor*ks*pow(phi,shininess);
    Surface ss =rayMarching(p+100.0*minDist*n,ld);
    if(ss.d<length(p-lp))
        col*=0.2;
    return col;
}
mat2 Rot(float a)
{
    float s = sin(a);
    float c = cos(a);
    return mat2(c, -s, s, c);
}

mat3 setCamera(vec3 CamPos,vec3 Look_at)
{
    vec3 cd =normalize(Look_at-CamPos);
    vec3 cv = cross(cd,vec3(0.0,1.0,0.0));
    vec3 cu = cross(cv,cd);
    return mat3(cv,cu,cd);
}

vec3 getSkyColor(float t, float y) {
    vec3 nightTop    = vec3(0.0, 0.0, 0.0);
    vec3 nightBottom = vec3(0.0, 0.0, 0.0);
    vec3 dawnTop     = vec3(1.0, 0.6, 0.2);
    vec3 dawnBottom  = vec3(1.0, 0.3, 0.0);
    vec3 dayTop      = vec3(0.4, 0.7, 1.0);
    vec3 dayBottom   = vec3(0.7, 0.9, 1.0);
    vec3 duskTop     = vec3(52.0/255.0, 21.0/255.0, 57.0/255.0);
    vec3 duskBottom  = vec3(54.0/255.0, 1.0/255.0, 63.0/255.0);
    vec3 night = mix(nightBottom, nightTop, y);
    vec3 dawn  = mix(dawnBottom, dawnTop, y);
    vec3 day   = mix(dayBottom, dayTop, y);
    vec3 dusk  = mix(duskBottom, duskTop, y);
    
    if (t == 0.0) {
        return day;
    } else if (t < 0.15) {
        float f = smoothstep(0.1, 0.15, t);
        return mix(day, dusk, f);
    } else if (t < 0.2) {
        float f = smoothstep(0.15, 0.2, t);
        return mix(dusk, night, f);
    } else if (t < 0.4) {
        float f = smoothstep(0.35, 0.4, t); 
        return mix(night, dawn, f);
    } else if (t < 0.45) {
        float f = smoothstep(0.4, 0.45, t);
        return mix(dawn, day, f);
    } else if (t < 0.7) {
        float f = smoothstep(0.65, 0.7, t);
        return mix(day, dusk, f);
    } else if (t < 0.75) {
        float f = smoothstep(0.7, 0.75, t);
        return mix(dusk, night, f);
    } else if (t < 0.9){
        float f = smoothstep(0.85, 0.9, t); 
        return mix(night, dawn, f);
    } else if (t < 0.95) {
        float f = smoothstep(0.9, 0.95, t);
        return mix(dawn, day, f);
    } else {
        return day;
    }
}

void main ()
{
    vec2 uv = (gl_FragCoord.xy-0.5*iResolution.xy)/iResolution.xy;
    float ra =iResolution.x/iResolution.y;
    uv.x*=ra;
    float theta = iTime * 0.2 + PI - 0.8;
    float zoom = 12.0 - 8.0 * clamp(iMouse.w, 0.0, 1.0);
    float alturaExtra = 8.0;
    vec3 Cam;
    Cam.x = zoom * cos(theta);
    Cam.y = alturaExtra;
    Cam.z = zoom * sin(theta);
    vec3 Target = vec3(0.0, 4.0, 0.0);
    mat3 M = setCamera(Cam, Target);
    vec3 rd = normalize(vec3(uv.x, uv.y, 0.5));
    rd = M * rd;
    float dayTime = mod(iTime / 24.0, 1.0);
    float skyY = clamp((uv.y + 0.5), 0.0, 1.0);
    Surface sd = rayMarching(Cam + Target, rd);
    vec3 col = getSkyColor(dayTime, skyY);
    if (sd.d < maxDist)
    {
        vec3 p = Cam + Target + sd.d * rd;
        col = getLight(p, sd, Cam + Target);
    }
    col = pow(col, vec3(0.4545));
    C = vec4(col, 1.0);
}