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
    // cr.xyz = centro, cr.w = raio
    float r = cr.w;
    // Pulso animado (remova se não quiser animação)
    // r += 0.3 * pow((0.5 + 0.5 * sin(2.0 * PI * iTime * 1.2)), 4.0);

    // Move para o centro do semicilindro
    vec3 q = p - cr.xyz;

    // SDF de cilindro infinito ao longo de X (meio-cilindro deitado no eixo X)
    float dCyl = length(q.yz) - r;

    // Recorte para metade superior (y >= 0) -- base plana para baixo
    float dSemi = max(dCyl, -q.y);

    // Limite comprimento (ex: comprimento l=4.0 ao longo de X)
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
    arco.d = max(dOuter, -dInner);
    arco.color = color;
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
    int nArcos = 6;

    vec3 centros[6];
    centros[0] = vec3(-12.0, 0.0, 16.0);
    centros[1] = vec3(-6.0, 0.0, 8.0);
    centros[2] = vec3(-20.0, 0.0, 12.0);
    centros[3] = vec3(0.0, 0.0, 10.0);

    float rot90 = PI * 0.5; // 90 graus em radianos

    for (int i = 0; i < nArcos; ++i) {
        Surface arco;
        vec3 centro = centros[i];
        if (i == 2) {
            arco = rotatedSemiCylinder(p, centro, raio, raioInterno, rot90, (i % 2 == 0) ? vec3(1.0, 0.5, 0.1) : vec3(0.1, 0.7, 0.8));
        } else {
            float dOuter = semicylinderDist(p, vec4(centro, raio));
            float dInner = semicylinderDist(p, vec4(centro, raioInterno));
            arco.d = max(dOuter, -dInner);
            arco.color = (i % 2 == 0) ? vec3(1.0, 0.5, 0.1) : vec3(0.1, 0.7, 0.8);
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
    // p.xz é a posição no plano
    vec2 pos = p.xz - center;
    float ang = atan(pos.y, pos.x); // [-PI, PI]
    float arcDist = abs(length(pos) - radius) - width;
    // Limitar ao setor angular desejado
    float angleMask = max(angleStart - ang, ang - angleEnd);
    float d = max(arcDist, angleMask);
    return max(d, abs(p.y)); // só no plano y=0
}

// Estrada curva mais aberta (raio maior, setor menor)
float plazaPathDist(vec3 p) {
    float width = 3;
    // Parâmetros do arco da estrada
    vec2 center = vec2(-28.0, 4.0); // centro mais afastado no eixo X
    float radius = 30.0;          // raio maior para curva mais suave
    float angleStart = radians(0.0);    // início do arco
    float angleEnd = radians(90.0);     // fim do arco (curva mais aberta)
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
    // --- Parâmetro para posição do lobby ---
    vec2 lobbyCenter = vec2(-20.0, 4.0); // Altere livremente a posição do quadrado/lobby

    // Arcos coloridos
    Surface arches = multiArches(p);

    // Gramado (verde)
    Surface grass;
    grass.color = vec3(0.2, 0.6, 0.2);
    grass.d = planeDist(p, vec4(0.0, 1.0, 0.0, 0.0)); // y=0

    // Caminho (plano claro)
    Surface path;
    path.color = vec3(0.85, 0.8, 0.7);
    float caminho = rectPathDist(p, vec2(-12.0, 4.0), vec2(20.0, 4.0), 2.5);
    path.d = max(p.y, caminho);

    Surface path2;
    path2.color = vec3(0.85, 0.8, 0.7);
    float caminho2 = rectPathDist(p, vec2(-20.0, 4.0), vec2(-20.0, 40.0), 2.5);
    path2.d = max(p.y, caminho2);

    // Lobby de concreto (quadrado grande)
    Surface lobby;
    lobby.color = vec3(0.85, 0.8, 0.7); // concreto
    lobby.d = squareDist(p, lobbyCenter, 8.0);

    // Grama interna do lobby (quadrado menor)
    Surface lobbyGrass;
    lobbyGrass.color = vec3(0.2, 0.6, 0.2); // grama
    lobbyGrass.d = squareDist(p, lobbyCenter, 4.0);

    // Caminho poligonal (cinza claro)
    Surface plazaPath;
    plazaPath.color = vec3(0.85, 0.8, 0.7); // cor clara tipo concreto
    plazaPath.d = plazaPathDist(p);

    // Círculo vermelho pequeno acima do chão em (0,0)
    Surface refCircle;
    refCircle.color = vec3(1.0, 0.0, 0.0); // vermelho puro
    float circleRadius = 0.5;
    float circleY = 0.25; // bem acima do chão/caminho
    refCircle.d = length(vec3(p.x, p.y - circleY, p.z)) - circleRadius;

    // União dos objetos (círculo vermelho tem prioridade visual)
    Surface s = unionS(grass, arches);
    s = unionS(s, path);
    s = unionS(s, path2);
    s = unionS(s, lobby);
    s = unionS(s, lobbyGrass);
    s = unionS(s, refCircle);
    s = unionS(s, plazaPath);
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
    // Detecta se é arco pela cor
    bool isArch = 
        all(greaterThanEqual(s.color, vec3(0.09))) &&
        all(lessThanEqual(s.color, vec3(1.01)));

    if (isArch) {
        // Normal estimada
        vec3 n = estimateNormal(p);
        // Direção do raio de câmera
        vec3 viewDir = normalize(CamPos - p);
        // Se normal aponta para dentro (face interna), retorna cor de fundo
        // if (dot(n, viewDir) < 0.0) {
        //     return COLOR_BACKGROUND;
        // }
    }

    // Luz do "sol" girando ao redor do cilindro
    float sunRadius = 10.0;
    float sunHeight = 6.0;
    float sunAngle = iTime * 0.5; // velocidade de rotação

    // Sol girando em torno do eixo X (pode ajustar para Y ou Z se preferir)
    vec3 lp = vec3(
        0.0,
        sunHeight * cos(sunAngle),
        sunRadius * sin(sunAngle)
    );

    vec3 lColor= vec3(1.0, 0.95, 0.85); // cor amarelada de sol
    vec3 ld = normalize(lp-p);
    vec3 n = estimateNormal(p);
    float r =clamp(dot(ld,n),0.0,1.0);
    float ka =0.3;
    float kd=0.5;
    float ks =0.20;
    vec3 eye = normalize(p-CamPos);
    vec3 R =normalize(reflect(n,ld));
    float phi = clamp(dot(R,eye),0.0,1.0);
    vec3 col=s.color*ka+s.color*r*kd+lColor*ks*pow(phi,10.0);
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


void main ()
{
    vec2 uv = (gl_FragCoord.xy-0.5*iResolution.xy)/iResolution.xy;
    float ra =iResolution.x/iResolution.y;
    uv.x*=ra;

    // --- Câmera livre com mouse ---
    float theta = (iMouse.x / iResolution.x) * TAU; // rotação horizontal
    float phi = (iMouse.y / iResolution.y) * PI;    // rotação vertical
    float zoom = 12.0 - 8.0 * clamp(iMouse.w, 0.0, 1.0); // zoom com scroll (opcional)

    // Limitar phi para evitar flip
    phi = clamp(phi, 0.1, PI - 0.1);

    // Coordenadas esféricas para posição da câmera
    vec3 Cam;
    Cam.x = zoom * sin(phi) * cos(theta);
    Cam.y = zoom * cos(phi);
    Cam.z = zoom * sin(phi) * sin(theta);

    vec3 Target = vec3(0.0, 1.0, 6.0); // foco da câmera
    mat3 M = setCamera(Cam + Target, Target);

    vec3 rd = normalize(vec3(uv.x, uv.y, 0.5));
    rd = M * rd;
    Surface sd = rayMarching(Cam + Target, rd);
    vec3 col = COLOR_BACKGROUND;
    if (sd.d < maxDist)
    {
        vec3 p = Cam + Target + sd.d * rd;
        col = getLight(p, sd, Cam + Target);
    }
    col = pow(col, vec3(0.4545));
    C = vec4(col, 1.0);
}
