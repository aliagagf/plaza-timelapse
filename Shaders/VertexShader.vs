#version 330 core
layout (location = 0) in vec3 aPos;
layout(location = 1) in vec3 aColor; // se não usar, pode remover
layout(location = 2) in vec2 aTexCoord; // se não usar, pode remover

out vec3 ourColor;
out vec2 TexCoord;

void main()
{
    gl_Position = vec4(aPos, 1.0);
    ourColor = aColor;
    TexCoord = aTexCoord;
}
