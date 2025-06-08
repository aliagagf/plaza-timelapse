cmake_minimum_required(VERSION 3.16)
set(CMAKE_CXX_STANDARD 17)

project(RayMarching)

# Incluir diretórios do projeto
include_directories(${PROJECT_SOURCE_DIR}/includes)

# GLFW
set(GLFW_DIR "C:/libs/glfw")
include_directories(${GLFW_DIR}/include)
link_directories(${GLFW_DIR}/lib-mingw-w64)

# Adiciona executável
add_executable(RayMarching
    stb_image/stb_image.cpp
    glad.c
    RayMarching.cpp
    Shader.h
)

# Vincular bibliotecas
target_link_libraries(RayMarching
    glfw3   # ou glfw3dll dependendo do nome do .a
    opengl32
)
