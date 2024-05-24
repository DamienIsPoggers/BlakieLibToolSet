using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlakieLibSharp;
using Raylib_cs;

namespace PrmAnEditor
{
    internal static class SprAnManager
    {
        public static SprAn sprAn = new SprAn();
        public static DPSpr dpspr = null;

        static Shader palShader;
        static string palShaderVert = @"
        #version 330

        in vec3 vertexPosition;
        in vec2 vertexTexCoord;
        in vec3 vertexNormal;
        in vec4 vertexColor;

        uniform mat4 mvp;

        out vec2 fragTexCoord;

        void main()
        {
            fragTexCoord = vertexTexCoord;

            gl_Position = mvp*vec4(vertexPosition, 1.0);
        }
        ";
        static string palShaderFrag = @"
        #version 330

        in vec2 fragTexCoord;

        uniform sampler2D sprite;
        uniform sampler2D palette;

        out vec4 finalColor;

        void main()
        {
            float colIndex = texture(sprite, fragTexCoord).r;

            finalColor = texture(palette, vec2(colIndex, 0.0f));
        }
        ";

        public static void LoadShader()
        {
            palShader = Raylib.LoadShaderFromMemory(palShaderVert, palShaderFrag);
        }

        public static void UnloadShader()
        {
            Raylib.UnloadShader(palShader);
        }

        public static void New()
        {
            sprAn = new SprAn();
        }

        public static void Load(string fileName)
        {
            BinaryReader file = new BinaryReader(File.OpenRead(fileName));
            sprAn = new SprAn(file);
            file.Close();
        }

        public static void Save(string fileName)
        {
            sprAn.Save(fileName);
        }

        public static void LoadDPSpr(string fileName, bool useBasePal)
        {
            CloseDPSpr();
            BinaryReader file = new BinaryReader(File.OpenRead(fileName));
            dpspr = new DPSpr(file, useBasePal);
            file.Close();
            foreach(DPSpr.Sprite sprite in dpspr.sprites.Values)
                unsafe
                {
                    uint id = Rlgl.LoadTexture(sprite.imageDataPtr, sprite.width, sprite.height,
                        sprite.indexed ? PixelFormat.UncompressedGrayscale : PixelFormat.UncompressedR8G8B8A8, 1);
                    //Console.WriteLine(id);
                    Rlgl.TextureParameters(id, Rlgl.TEXTURE_MIN_FILTER, Rlgl.TEXTURE_FILTER_NEAREST);
                    Rlgl.TextureParameters(id, Rlgl.TEXTURE_MAG_FILTER, Rlgl.TEXTURE_FILTER_NEAREST);
                    Rlgl.TextureParameters(id, Rlgl.TEXTURE_WRAP_S, Rlgl.TEXTURE_WRAP_MIRROR_REPEAT);
                    Rlgl.TextureParameters(id, Rlgl.TEXTURE_WRAP_T, Rlgl.TEXTURE_WRAP_MIRROR_REPEAT);
                    dpspr.SpriteIsOnGPU(sprite.name, id);
                }
        }

        public static void CloseDPSpr()
        {
            if (dpspr == null)
                return;

            foreach (DPSpr.Sprite sprite in dpspr.sprites.Values)
            {
                Rlgl.UnloadTexture(sprite.glTexId);
                Console.WriteLine("Unloaded Texture \"" + sprite.name + "\" ID: [" + sprite.glTexId + "]");
            }
            dpspr = null;
            GC.Collect();
        }
    }
}
