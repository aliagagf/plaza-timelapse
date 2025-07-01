in vec3 aColor;
in vec4 aPosition;
out vec4 C;
uniform sampler2D iChannel0;
uniform sampler2D iChannel1;
uniform sampler2D iChannel2;
uniform sampler2D iChannel3;
uniform vec2 iResolution;
uniform vec4 iMouse;
uniform float iTime;
uniform int iFrame;


#define PI 3.1459
#define TAU PI*2.0

const vec3 COLOR_BACKGROUND = vec3(0.4, 0.6, 1.0); // azul claro

/*  Install  Istructions

sudo apt-get install g++ cmake git
 sudo apt-get install libsoil-dev libglm-dev libassimp-dev libglew-dev libglfw3-dev libxinerama-dev libxcursor-dev
libxi-dev libfreetype-dev libgl1-mesa-dev xorg-dev

git clone https://github.com/JoeyDeVries/LearnOpenGL.git*/

float minDist = 0.01;
float maxDist = 100.;
int maxIt = 100;

struct Surface {
    vec3 color;
    float d;
    vec2 uv;
    int materialType; // 0 = default, 1 = grass, 2 = path, 3 = metálico
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

    mat4 mat = mat4 (vec4 (  c, 0.0,  -s, 0.0),
                     vec4 (0.0, 1.0, 0.0, 0.0),
                     vec4 (  s, 0.0,   c, 0.0),
                     vec4 (0.0, 0.0, 0.0, 1.0));

    return mat;
}

mat4 rotZ (in float angle)
{
    float rad = radians (angle);
    float c = cos (rad);
    float s = sin (rad);

    mat4 mat = mat4 (vec4 (  c,   s, 0.0, 0.0),
                     vec4 ( -s,   c, 0.0, 0.0),
                     vec4 (0.0, 0.0, 1.0, 0.0),
                     vec4 (0.0, 0.0, 0.0, 1.0));

    return mat;
}

// Tarefa

//Operaçoes  de translação

// Operações de escala

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
    // cr.xyz = centro, cr.w = raio
    float r = cr.w;
    // Pulso animado
    r += 0.3 * pow((0.5 + 0.5 * sin(2.0 * PI * iTime * 1.2)), 4.0);

    // Move para o centro do arco
    vec3 q = p - cr.xyz;

    // SDF de cilindro infinito ao longo de Y
    float dCyl = length(q.xz) - r;

    // Limite altura (ex: altura h=2.0)
    float h = 2.0;
    float dCap = max(abs(q.y) - h * 0.5, dCyl);

    // Limite angular (arco)
    float angle = atan(q.z, q.x); // [-PI, PI]
    float dAngle = max(angleMin - angle, angle - angleMax);

    // Se estiver fora do arco, penaliza distância
    float dArc = max(dCap, dAngle);

    return dArc;
}

// Função para arco semicilíndrico rotacionado em angleRad no plano XZ
Surface rotatedSemiCylinder(vec3 p, vec3 centro, float raio, float raioInterno, float angleRad, vec3 color) {
    Surface arco;
    vec3 q = p - centro;
    float x = q.x * cos(angleRad) - q.z * sin(angleRad);
    float z = q.x * sin(angleRad) + q.z * cos(angleRad);
    vec3 pRot = vec3(x, q.y, z) + centro;
    float dOuter = semicylinderDist(pRot, vec4(centro, raio));
    float dInner = semicylinderDist(pRot, vec4(centro, raioInterno));
    // SDF para arco oco: distância até a parede mais próxima (externa ou interna)
    arco.d = min(abs(dOuter), abs(dInner)) * sign(dOuter);
    arco.color = color;
    arco.materialType = 3; // 3 = metálico
    return arco;
}

// Exemplo de uso para um arco específico:
Surface getRotatedArchExample(vec3 p) {
    // Parâmetros do arco
    vec3 centro = vec3(-20.0, 0.0, 12.0);
    float raio = 3.0;
    float raioInterno = raio - 0.05;
    float rot90 = PI * 0.5; // 90 graus
    vec3 color = vec3(1.0, 0.5, 0.1);
    return rotatedSemiCylinder(p, centro, raio, raioInterno, rot90, color);
}

// Novo: retorna cor e distância do arco mais próximo entre vários arcos coloridos
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
    centros[3] = vec3(-4.0, -1.0, 29.0);
    float rot90 = PI * 0.5;
    for (int i = 0; i < nArcos; ++i) {
        Surface arco;
        vec3 centro = centros[i];
        if (i == 1) {
            arco = rotatedSemiCylinder(p, centro, raio, raioInterno, rot90, (i % 2 == 0) ? vec3(1.0, 0.5, 0.1) : vec3(0.1, 0.7, 0.8));
            arco.materialType = 3; // 3 = metálico
        } else {
            float dOuter = semicylinderDist(p, vec4(centro, raio));
            float dInner = semicylinderDist(p, vec4(centro, raioInterno));
            // SDF para arco oco: distância até a parede mais próxima (externa ou interna)
            arco.d = min(abs(dOuter), abs(dInner)) * sign(dOuter);
            arco.color = (i % 2 == 0) ? vec3(1.0, 0.5, 0.1) : vec3(0.1, 0.7, 0.8);
            arco.materialType = 3; // 3 = metálico
        }
        if (arco.d < result.d) {
            result = arco;
        }
    }
    return result;
}

// SDF para um segmento retangular no plano y=0
float rectPathDist(vec3 p, vec2 a, vec2 b, float width) {
    // p.xz é a posição no plano
    vec2 pa = p.xz - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    vec2 proj = a + h * ba;
    float d = length(p.xz - proj) - width;
    return max(d, abs(p.y)); // só no plano y=0
}

// SDF para um arco de estrada curva no plano y=0
float curvedRoadDist(vec3 p, vec2 center, float radius, float angleStart, float angleEnd, float width) {
    vec2 pos = p.xz - center;
    float ang = atan(pos.y, pos.x);
    float arcDist = abs(length(pos) - radius) - width;
    float angleMask = max(angleStart - ang, ang - angleEnd);
    float d = max(arcDist, angleMask);
    return max(d, abs(p.y));
}

// Estrada curva mais aberta (raio maior, setor menor)
float plazaPathDist(vec3 p) {
    float width = 3;
    vec2 center = vec2(5.0, 0.0);
    float radius = 30.0;
    float angleStart = radians(0.0);
    float angleEnd = radians(90.0);
    float d = curvedRoadDist(p, center, radius, angleStart, angleEnd, width);
    return d;
}

// SDF para um quadrado no plano y=0, centrado em c, com semi-lado s
float squareDist(vec3 p, vec2 c, float s) {
    vec2 d = abs(p.xz - c) - vec2(s, s);
    float outside = length(max(d, 0.0));
    float inside = min(max(d.x, d.y), 0.0);
    return max(p.y, outside + inside);
}

Surface getSceneDist(vec3 p)
{
    // --- Novo centro do quadrado/lobby ---
    vec2 lobbyCenter = vec2(0.0, 0.0); // centro da cena

    // Arcos coloridos
    Surface arches = multiArches(p);

    // Gramado (verde)
    Surface grass;
    grass.color = vec3(0.2, 0.6, 0.2);
    grass.d = planeDist(p, vec4(0.0, 1.0, 0.0, 0.0)); // y=0
    grass.uv = p.xz * 0.05; // UV para textura de grama
    grass.materialType = 1; // 1 = grass

    // Caminho (plano claro)
    Surface path;
    path.color = vec3(0.85, 0.8, 0.7);
    float caminho = rectPathDist(p, vec2(8.0, 0.0), vec2(40.0, 0.0), 2.5);
    path.d = max(p.y, caminho);
    path.uv = p.xz * 0.05; // UV para textura do caminho
    path.materialType = 2;

    // Caminho reto duplicado do outro lado do quadrado (espelhado no eixo X)
    Surface path3;
    path3.color = vec3(0.85, 0.8, 0.7);
    float caminho3 = rectPathDist(p, vec2(-8.0, 0.0), vec2(-40.0, 0.0), 2.5);
    path3.d = max(p.y, caminho3);
    path3.uv = p.xz * 0.05;
    path3.materialType = 2;

    Surface path2;
    path2.color = vec3(0.85, 0.8, 0.7);
    float caminho2 = rectPathDist(p, vec2(0.0, 0.0), vec2(0.0, 36.0), 2.5);
    path2.d = max(p.y, caminho2);
    path2.uv = p.xz * 0.05;
    path2.materialType = 2;

    // Lobby de concreto (quadrado grande)
    Surface lobby;
    lobby.color = vec3(0.85, 0.8, 0.7); // concreto
    lobby.d = squareDist(p, lobbyCenter, 8.0);
    lobby.uv = p.xz * 0.05;
    lobby.materialType = 2;

    // Grama interna do lobby (quadrado menor)
    Surface lobbyGrass;
    lobbyGrass.color = vec3(0.2, 0.6, 0.2); // grama
    lobbyGrass.d = squareDist(p, lobbyCenter, 4.0);
    lobbyGrass.uv = p.xz * 0.05;
    lobbyGrass.materialType = 1;

    // Caminho poligonal (cinza claro)
    Surface plazaPath;
    plazaPath.color = vec3(0.85, 0.8, 0.7);
    plazaPath.d = plazaPathDist(p);
    plazaPath.uv = p.xz * 0.05;
    plazaPath.materialType = 2;

    // Caminho poligonal duplicado do outro lado do quadrado (espelhado no eixo X)
    Surface plazaPath2;
    plazaPath2.color = vec3(0.85, 0.8, 0.7);
    float width2 = 3.0;
    vec2 center2 = vec2(8.0, 0.0); // centro espelhado
    float radius2 = 30.0;
    float angleStart2 = radians(90.0);
    float angleEnd2 = radians(180.0);
    float d2 = curvedRoadDist(p, center2, radius2, angleStart2, angleEnd2, width2);
    plazaPath2.d = d2;
    plazaPath2.uv = p.xz * 0.05;
    plazaPath2.materialType = 2;

    // União dos objetos
    Surface s = unionS(grass, arches);
    s = unionS(s, path);
    s = unionS(s, path3);
    s = unionS(s, path2);
    s = unionS(s, lobby);
    s = unionS(s, lobbyGrass);
    s = unionS(s, plazaPath);
    s = unionS(s, plazaPath2);
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
    // Efeito metálico para arcos
    if (s.materialType == 3) {
        kd = 0.1; // menos difuso
        ks = 1.0; // mais especular
        shininess = 80.0; // brilho mais concentrado
        specColor = baseColor; // cor especular igual à cor base (efeito metálico)
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
mat2 Rot(float a) //2D
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

// Função para interpolar a cor do céu conforme o tempo do dia e altura do pixel (gradiente vertical)
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
    if (t < 0.2) {
        return night;
    } else if (t < 0.35) {
        float f = smoothstep(0.3, 0.35, t);
        return mix(night, dawn, f);
    } else if (t < 0.4) {
        float f = smoothstep(0.35, 0.4, t);
        return mix(dawn, day, f);
    } else if (t < 0.65) {
        float f = smoothstep(0.6, 0.65, t);
        return mix(day, dusk, f);
    } else if (t < 0.7) {
        float f = smoothstep(0.65, 0.7, t);
        return mix(dusk, night, f);
    } else if (t < 1.0) {
        return night;
    }
}

void main ()
{
    vec2 uv = (gl_FragCoord.xy-0.5*iResolution.xy)/iResolution.xy;
    float ra =iResolution.x/iResolution.y;
    uv.x*=ra;
    // Panorâmica aérea automática
    float theta = iTime * 0.2; // rotação azimutal automática
    float zoom = 12.0 - 8.0 * clamp(iMouse.w, 0.0, 1.0);
    float alturaExtra = 10.0; // valor extra para aumentar a altura da câmera
    vec3 Cam;
    Cam.x = zoom * cos(theta);
    Cam.y = alturaExtra;
    Cam.z = zoom * sin(theta);
    vec3 Target = vec3(0.0, 1.0, 0.0);
    mat3 M = setCamera(Cam + Target, Target);
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