#include <glad/glad.h>
#include <glm/glm.hpp>
#include <GLFW/glfw3.h>
#include "Shader.h"
#include "stb_image/stb_image.h"

#include <fstream>
#include <iostream>
#include <string>
#include <unistd.h>
#include <filesystem>

double lastTime,lt,fps;
int nbFrames = 0;
char title_string[50];
void framebuffer_size_callback(GLFWwindow* window, int width, int height);
void processInput(GLFWwindow *window,double *mouse, bool * shouldDraw, bool * press);
void DisplayFramebufferTexture(unsigned int textureID, Shader *program,unsigned int VAO,glm::vec2 R);

float vertices[] = {
    1.0f,  1.0f, 0.0f,   1.0f, 0.0f, 0.0f,   1.0f, 1.0f, 
    1.0f, -1.0f, 0.0f,   0.0f, 1.0f, 0.0f,   1.0f, 0.0f, 
    -1.0f, -1.0f, 0.0f,   0.0f, 0.0f, 1.0f,   0.0f, 0.0f, 
    -1.0f,  1.0f, 0.0f,   1.0f, 1.0f, 0.0f,   0.0f, 1.0f  
};

float TVvertices[] = {
    1.0f,  1.0f,   1.0f, 1.0f, 
    1.0f, -1.0f,   1.0f, 0.0f, 
    -1.0f, -1.0f,   0.0f, 0.0f, 
    -1.0f,  1.0f,   0.0f, 1.0f  
};

unsigned int indices[] = {
    0,1,3,
    1,2,3
};

int main()
{
    std::filesystem::path path = std::filesystem::current_path();
    int max_up = 5;
    while (max_up-- > 0 && !std::filesystem::exists(path / "Shaders")) {
        path = path.parent_path();
    }
    if (!std::filesystem::exists(path / "Shaders")) {
        std::cerr << "Pasta 'Shaders' não encontrada! Caminho base: " << path << std::endl;
        return -1;
    }

    const std::string pathString = path.string();

    glfwInit();
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);

#ifdef __APPLE__
    glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);
#endif

    const unsigned int SCR_WIDTH = 1280;
    const unsigned int SCR_HEIGHT = 720;

    GLFWwindow* window = glfwCreateWindow(SCR_WIDTH, SCR_HEIGHT, "Introduction", NULL, NULL);
    if (window == NULL)
    {
        std::cout << "Failed to create GLFW window" << std::endl;
        glfwTerminate();
        return -1;
    }
    glfwMakeContextCurrent(window);
    glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);

    if (!gladLoadGLLoader((GLADloadproc)glfwGetProcAddress))
    {
        std::cout << "Failed to initialize GLAD" << std::endl;
        return -1;
    }

    std::filesystem::path vertexShaderPath = std::filesystem::absolute(path / "Shaders" / "VertexShader.vs");
    std::filesystem::path fragmentShaderPath = std::filesystem::absolute(path / "Shaders" / "Image.fs.frag");
    std::filesystem::path fragmentShaderAPath = std::filesystem::absolute(path / "Shaders" / "BufferA.fs");
    std::filesystem::path fragmentShaderBPath = std::filesystem::absolute(path / "Shaders" / "BufferB.fs");
    std::filesystem::path fragmentShaderCPath = std::filesystem::absolute(path / "Shaders" / "BufferC.fs");
    std::filesystem::path fragmentShaderDPath = std::filesystem::absolute(path / "Shaders" / "BufferD.fs");
    std::filesystem::path TVvertexShaderPath = std::filesystem::absolute(path / "Shaders" / "TextureViewer.vs.vert");
    std::filesystem::path TVShaderPath = std::filesystem::absolute(path / "Shaders" / "TextureViewer.fs.frag");
    std::filesystem::path commonPath = std::filesystem::absolute(path / "Shaders" / "common.incl");

    Shader Imageprogram(commonPath.string().c_str(), vertexShaderPath.string().c_str(), fragmentShaderPath.string().c_str());
    Shader BufferAprogram(commonPath.string().c_str(), vertexShaderPath.string().c_str(), fragmentShaderAPath.string().c_str());
  
    Shader TVShaderprogram(
    commonPath.string().c_str(),
    TVvertexShaderPath.string().c_str(),
    TVShaderPath.string().c_str()
);

    unsigned int VBO, VAO, EBO;
    glGenVertexArrays(1, &VAO);
    glGenBuffers(1, &VBO);
    glGenBuffers(1, &EBO);
    glBindVertexArray(VAO);

    glBindBuffer(GL_ARRAY_BUFFER,VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STREAM_DRAW);

    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices),indices, GL_STREAM_DRAW);

    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 8*sizeof(float),(void *)0 );
    glEnableVertexAttribArray(0);
    glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, 8*sizeof(float),(void *) (3*sizeof(float)) );
    glEnableVertexAttribArray(1);
    glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, 8*sizeof(float),(void *) (6*sizeof(float)) );
    glEnableVertexAttribArray(2);



    unsigned int TV_VBO, TV_VAO, TV_EBO;
    glGenVertexArrays(1, &TV_VAO);
    glGenBuffers(1, &TV_VBO);
    glGenBuffers(1, &TV_EBO);
    glBindVertexArray(TV_VAO);

    glBindBuffer(GL_ARRAY_BUFFER,TV_VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(TVvertices), TVvertices, GL_STREAM_DRAW);

    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, TV_EBO);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices),indices, GL_STREAM_DRAW);

    glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 4*sizeof(float),(void *)0 );
    glEnableVertexAttribArray(0);
    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 4*sizeof(float),(void *) (2*sizeof(float)) );
    glEnableVertexAttribArray(1);


    std::string texPath = pathString + "/textures/grass-texture.jpg";
    int texWidth, texHeight, texChannels;
    stbi_set_flip_vertically_on_load(true); 
    unsigned char* texData = stbi_load(texPath.c_str(), &texWidth, &texHeight, &texChannels, 0);

    std::string pathTexPath = pathString + "/textures/path-texture.jpg";
    int pathTexWidth, pathTexHeight, pathTexChannels;
    unsigned char* pathTexData = stbi_load(pathTexPath.c_str(), &pathTexWidth, &pathTexHeight, &pathTexChannels, 0);
    unsigned int pathTextureID;
    glGenTextures(1, &pathTextureID);
    glBindTexture(GL_TEXTURE_2D, pathTextureID);
    GLenum pathFormat = GL_RGB;
    if (pathTexChannels == 1) pathFormat = GL_RED;
    else if (pathTexChannels == 3) pathFormat = GL_RGB;
    else if (pathTexChannels == 4) pathFormat = GL_RGBA;
    if (pathTexData) {
        glTexImage2D(GL_TEXTURE_2D, 0, pathFormat, pathTexWidth, pathTexHeight, 0, pathFormat, GL_UNSIGNED_BYTE, pathTexData);
    } else {
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, SCR_WIDTH, SCR_HEIGHT, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
    }
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glGenerateMipmap(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, 0);
    if (pathTexData) stbi_image_free(pathTexData);

    std::string mainPathTexPath = pathString + "/textures/main-path-texture.jpg";
    int mainPathTexWidth, mainPathTexHeight, mainPathTexChannels;
    unsigned char* mainPathTexData = stbi_load(mainPathTexPath.c_str(), &mainPathTexWidth, &mainPathTexHeight, &mainPathTexChannels, 0);
    unsigned int mainPathTextureID;
    glGenTextures(1, &mainPathTextureID);
    glBindTexture(GL_TEXTURE_2D, mainPathTextureID);
    GLenum mainPathFormat = GL_RGB;
    if (mainPathTexChannels == 1) mainPathFormat = GL_RED;
    else if (mainPathTexChannels == 3) mainPathFormat = GL_RGB;
    else if (mainPathTexChannels == 4) mainPathFormat = GL_RGBA;
    if (mainPathTexData) {
        glTexImage2D(GL_TEXTURE_2D, 0, mainPathFormat, mainPathTexWidth, mainPathTexHeight, 0, mainPathFormat, GL_UNSIGNED_BYTE, mainPathTexData);
    } else {
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, SCR_WIDTH, SCR_HEIGHT, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
    }
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glGenerateMipmap(GL_TEXTURE_2D);
    glBindTexture(GL_TEXTURE_2D, 0);
    if (mainPathTexData) stbi_image_free(mainPathTexData);

    unsigned int FBO_0;
    glGenFramebuffers(1, &FBO_0);

    glBindFramebuffer(GL_FRAMEBUFFER,FBO_0);

    unsigned int iChannel_0;

    glGenTextures(1, &iChannel_0);
    glBindTexture(GL_TEXTURE_2D, iChannel_0);

    GLenum format = GL_RGB;
    if (texChannels == 1) format = GL_RED;
    else if (texChannels == 3) format = GL_RGB;
    else if (texChannels == 4) format = GL_RGBA;

    if (texData) {
        glTexImage2D(GL_TEXTURE_2D, 0, format, texWidth, texHeight, 0, format, GL_UNSIGNED_BYTE, texData);
    } else {
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, SCR_WIDTH, SCR_HEIGHT, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
    }

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    glGenerateMipmap(GL_TEXTURE_2D);

    glBindTexture(GL_TEXTURE_2D, 0);

    if (texData) stbi_image_free(texData);

    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, iChannel_0, 0 );

        unsigned int rbo_0;
        glGenRenderbuffers(1, &rbo_0);
        glBindRenderbuffer(GL_RENDERBUFFER, rbo_0);
        glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, SCR_WIDTH, SCR_HEIGHT); 
        glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, rbo_0); 
        

    if(glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
        std::cout << "ERROR::FRAMEBUFFER:: Framebuffer is not complete!" << std::endl;
    


    unsigned int FBO_1;
    glGenFramebuffers(1, &FBO_1);
    
    glBindFramebuffer(GL_FRAMEBUFFER,FBO_1);


    unsigned int iChannel_1;

    glGenTextures(1, &iChannel_1);
    glBindTexture(GL_TEXTURE_2D, iChannel_1);

    
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, SCR_WIDTH, SCR_HEIGHT, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    glGenerateMipmap(GL_TEXTURE_2D);

    glBindTexture(GL_TEXTURE_2D, 0);

    

    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, iChannel_1, 0);

    
        unsigned int rbo_1;
        glGenRenderbuffers(1, &rbo_1);
        glBindRenderbuffer(GL_RENDERBUFFER, rbo_1);
        glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, SCR_WIDTH, SCR_HEIGHT); 
        glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, rbo_1); 
        


    if(glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
         std::cout << "ERROR::FRAMEBUFFER:: Framebuffer is not complete!" << std::endl;
    


    unsigned int FBO_2;
    glGenFramebuffers(1, &FBO_2);
    
    glBindFramebuffer(GL_FRAMEBUFFER,FBO_2);

    unsigned int iChannel_2;

    glGenTextures(1, &iChannel_2);
    glBindTexture(GL_TEXTURE_2D, iChannel_2);

    
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, SCR_WIDTH, SCR_HEIGHT, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    glGenerateMipmap(GL_TEXTURE_2D);

    glBindTexture(GL_TEXTURE_2D, 0);

    

    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, iChannel_2, 0 );

    
        unsigned int rbo_2;
        glGenRenderbuffers(1, &rbo_2);
        glBindRenderbuffer(GL_RENDERBUFFER, rbo_2);
        glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, SCR_WIDTH, SCR_HEIGHT); 
        glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, rbo_2); 
        


    if(glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
         std::cout << "ERROR::FRAMEBUFFER:: Framebuffer is not complete!" << std::endl;
    


    unsigned int FBO_3;
    glGenFramebuffers(1, &FBO_3);
    
    glBindFramebuffer(GL_FRAMEBUFFER,FBO_3);

    unsigned int iChannel_3;

    glGenTextures(1, &iChannel_3);
    glBindTexture(GL_TEXTURE_2D, iChannel_3);

    
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32F, SCR_WIDTH, SCR_HEIGHT, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    glGenerateMipmap(GL_TEXTURE_2D);

    glBindTexture(GL_TEXTURE_2D, 0);

    

    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, iChannel_3, 0 );

    
        unsigned int rbo_3;
        glGenRenderbuffers(1, &rbo_3);
        glBindRenderbuffer(GL_RENDERBUFFER, rbo_3);
        glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, SCR_WIDTH, SCR_HEIGHT); 
        glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, rbo_3); 
        


    if(glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
         std::cout << "ERROR::FRAMEBUFFER:: Framebuffer is not complete!" << std::endl;
    

   
    
double * mouse = (double *) malloc(sizeof(double)*4);

    
    
    glm::vec2 resolution(SCR_WIDTH,SCR_HEIGHT);
    
    Imageprogram.use();
    Imageprogram.setVec2("iResolution",resolution);

    bool shouldDraw = false;
        bool press = false;
        float currentTime;
        int frame=0;
    
    while(!glfwWindowShouldClose(window)){
        currentTime = static_cast<float>(glfwGetTime());
        

          processInput(window,mouse, &shouldDraw, & press);


           

        if(shouldDraw)
        {
          
          glBindFramebuffer(GL_FRAMEBUFFER, FBO_1);
          glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
          glClear(GL_COLOR_BUFFER_BIT);
          BufferAprogram.use();
          BufferAprogram.setVec2("iResolution",resolution) ;
          BufferAprogram.setVec4("iMouse", mouse);
          BufferAprogram.setFloat("iTime",currentTime);
          BufferAprogram.setInt("iFrame",frame);
          BufferAprogram.setSampler("iChannel0",0); 
          BufferAprogram.setSampler("iChannel1",1); 
          BufferAprogram.setSampler("iChannel4",4); 
          glBindVertexArray(VAO);
          glActiveTexture(GL_TEXTURE0);
          glBindTexture(GL_TEXTURE_2D, iChannel_0); 
          glActiveTexture(GL_TEXTURE1);
          glBindTexture(GL_TEXTURE_2D, pathTextureID); 
          glActiveTexture(GL_TEXTURE4);
          glBindTexture(GL_TEXTURE_2D, mainPathTextureID); 
          glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT,0);

          
          glBindFramebuffer(GL_FRAMEBUFFER, 0);
          glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
          glClear(GL_COLOR_BUFFER_BIT);
          Imageprogram.use();
          Imageprogram.setVec2("iResolution",resolution);
          Imageprogram.setFloat("iTime",currentTime);
          Imageprogram.setInt("iFrame",frame);
          Imageprogram.setVec4("iMouse", mouse);
          Imageprogram.setSampler("iChannel0",0); 
          glBindVertexArray(VAO);
          glActiveTexture(GL_TEXTURE0);
          glBindTexture(GL_TEXTURE_2D, iChannel_1); 
          glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT,0);

          double CurrentTime = glfwGetTime();
          double past = CurrentTime - lt;
          if(past>0.1) {
            fps = (float)nbFrames/past;
            sprintf(title_string, "Introduction - FPS = %.i ", (int)fps);
            glfwSetWindowTitle(window, title_string);
            lt+=past;
            nbFrames = 0;
          }
          nbFrames++;
          frame++;
        }
        glfwSwapBuffers(window);
        glfwPollEvents();


    }

    glDeleteVertexArrays(1, &VAO);
    glDeleteBuffers(1, &VBO);
    glDeleteBuffers(1, &EBO);
    glDeleteFramebuffers(1, &FBO_0);
    glDeleteFramebuffers(1, &FBO_1);
    glDeleteFramebuffers(1, &FBO_2);
    glDeleteFramebuffers(1, &FBO_3);
    glDeleteRenderbuffers(1, &rbo_0);
    glDeleteRenderbuffers(1, &rbo_1);
    glDeleteRenderbuffers(1, &rbo_2);
    glDeleteRenderbuffers(1, &rbo_3);

    glfwTerminate();
    return 0;
}

void processInput(GLFWwindow *window,double *mouse, bool * shouldDraw, bool * press)
{
    
    if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS) glfwSetWindowShouldClose(window, true);

    
    
    glfwGetCursorPos(window,&mouse[0], &mouse[1]);
    mouse[2] = 0.0;
    mouse[3] = 0.0;
    if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_LEFT) == GLFW_PRESS) mouse[2] = 1.0;
    if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_RIGHT) == GLFW_PRESS) mouse[3] = 1.0;

    
    int h,w;
    glfwGetFramebufferSize(window, &w, &h);
    
    
    mouse[1]=h-mouse[1];
    

    if(glfwGetKey(window, 32) == GLFW_PRESS && !*press) {
        *shouldDraw = !*shouldDraw;
        lt = glfwGetTime();
        * press = true;
    }
    if(glfwGetKey(window, 32) == GLFW_RELEASE) * press = false;
}

void DisplayFramebufferTexture(unsigned int textureID,Shader *program, unsigned int VAO,glm::vec2 R)
{

       program->use();


           program->setSampler("fboAttachment",0);
           program->setVec2("iResolution",R);
           glBindVertexArray(VAO);

           glActiveTexture(GL_TEXTURE0);
           glBindTexture(GL_TEXTURE_2D, textureID);

           glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT,0);
           glBindVertexArray(0);
       glUseProgram(0);
}


void framebuffer_size_callback(GLFWwindow* window, int width, int height)
{
    glViewport(0, 0, width, height);
    printf("%i:%i\n", width,height);
}

