using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BlakieLibSharp;
using Raylib_cs;
using static System.Windows.Forms.AxHost;

namespace PrmAnEditor
{
    internal static class PrmAnManager
    {
        public static PrmAn prmAn = new PrmAn();

        public static Dictionary<string, PrmAn.Texture> textures = new Dictionary<string, PrmAn.Texture>(); 

        static List<PrmAn.Layer> layersIn2D = new List<PrmAn.Layer>();

        public static void New()
        {
            prmAn = new PrmAn();
            foreach (string tex in textures.Keys)
                RemoveTex(tex);
            textures.Clear();
        }

        public static void Load(string fileName)
        {
            foreach (string tex in textures.Keys)
                RemoveTex(tex);
            BinaryReader file = new BinaryReader(File.OpenRead(fileName));
            prmAn = new PrmAn(file);
            file.Close();
            foreach (PrmAn.Texture texture in prmAn.textures.Values)
            {
                Image img = Raylib.LoadImageFromMemory(texture.name.Substring(texture.name.LastIndexOf('.')), texture.texDat);
                Texture2D tex = Raylib.LoadTextureFromImage(img);
                texture.glTexId = tex.Id;
                textures.Add(texture.name, texture);
            }
        }

        public static void Save(string fileName)
        {
            prmAn.SaveToFile(fileName);
        }

        public static void LoadTex(string fileName)
        {
            byte[] fileDat = File.ReadAllBytes(fileName);
            PrmAn.Texture tex = new PrmAn.Texture();
            int subStrPos = fileName.LastIndexOf('\\');
            tex.name = fileName.Substring(subStrPos >= 0 ? subStrPos + 1 : 0);
            subStrPos = tex.name.LastIndexOf('/');
            tex.name = tex.name.Substring(subStrPos >= 0 ? subStrPos + 1 : 0);
            tex.texDat = fileDat;
            tex.texDatSize = fileDat.Length;
            Image img = Raylib.LoadImageFromMemory(fileName.Substring(fileName.LastIndexOf('.')), fileDat);
            tex.width = img.Width;
            tex.height = img.Height;
            tex.id = textures.Count;
            tex.glTexId = Raylib.LoadTextureFromImage(img).Id;
            textures.Add(tex.name, tex);
            prmAn.textures.Add(tex.id, tex);
        }

        public static void RemoveTex(string texName)
        {
            PrmAn.Texture tex = textures[texName];
            textures.Remove(texName);
            prmAn.textures.Remove(tex.id);
            Rlgl.UnloadTexture(tex.glTexId);
            tex.Dispose();
        }

        public static void AddFrame()
        {
            PrmAn.Frame frame = new PrmAn.Frame();
            frame.name = "Frame_" + prmAn.frames.Count;
            prmAn.frames.Add(frame.name, frame);
        }

        public static void RemoveFrame(string frame)
        {
            prmAn.frames.Remove(frame);
        }

        public static bool ContainsFrame(string frame)
        {
            return prmAn.frames.ContainsKey(frame);
        }

        public static void ReKeyFrame(string oldName, string newName)
        {
            PrmAn.Frame frame;
            prmAn.frames.Remove(oldName, out frame);
            prmAn.frames.Add(newName, frame);
        }

        public static void ReKeyTexture(int oldId, int newId)
        {
            PrmAn.Texture tex;
            prmAn.textures.Remove(oldId, out tex);
            prmAn.textures.Add(newId, tex);
        }

        public static void ReKeyAnim(string oldName, string newName)
        {
            PrmAn.Animation anim;
            prmAn.animations.Remove(oldName, out anim);
            prmAn.animations.Add(newName, anim);
        }

        public static void AddAnimation()
        {
            PrmAn.Animation anim = new PrmAn.Animation();
            anim.name = "Anim_" + prmAn.animations.Count;
            prmAn.animations.Add(anim.name, anim);
        }

        public static void RemoveAnimation(string anim)
        {
            prmAn.animations.Remove(anim);
        }

        public static void Draw(string frameA, string frameB, float blend)
        {
            PrmAn.Frame frame = prmAn.BlendFrames(frameA, frameB, blend);

            foreach(PrmAn.Layer layer in frame.layers)
            {
                if(layer.drawIn2d)
                {
                    layersIn2D.Add(layer);
                    continue;
                }

                Rlgl.DisableBackfaceCulling();
                Rlgl.PushMatrix();
                Rlgl.Translatef(layer.position.X, layer.position.Y, layer.position.Z);
                Rlgl.Rotatef(layer.rotation.Y, 0.0f, 0.1f, 0.0f);
                Rlgl.Rotatef(layer.rotation.X, 0.1f, 0.0f, 0.0f);
                Rlgl.Rotatef(layer.rotation.Z, 0.0f, 0.0f, 0.1f);
                Rlgl.Scalef(layer.scale.X, layer.scale.Y, layer.scale.Z);

                Rlgl.Color4f((layer.colMult[0] + layer.colAdd[0]) / 255.0f, (layer.colMult[1] + layer.colAdd[1]) / 255.0f,
                    (layer.colMult[2] + layer.colAdd[2]) / 255.0f, (layer.colMult[3] + layer.colAdd[3]) / 255.0f);

                if (layer.additive)
                    Raylib.BeginBlendMode(BlendMode.Additive);

                Vector2 texSize;
                if (prmAn.textures.ContainsKey(layer.texId))
                {
                    Rlgl.SetTexture(prmAn.textures[layer.texId].glTexId);
                    texSize = new Vector2(prmAn.textures[layer.texId].width, prmAn.textures[layer.texId].height);
                }
                else
                    texSize = Vector2.Zero;
                
                switch(layer.primitiveType)
                {
                    default:
                    case PrmAn.PrimitiveType.Plane:
                        Rlgl.Begin(DrawMode.Quads);

                        Rlgl.TexCoord2f(layer.uv.X / texSize.X, layer.uv.Y / texSize.Y);
                        Rlgl.Vertex3f(0.0f, 0.0f, 0.0f);

                        Rlgl.TexCoord2f(layer.uv.X / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                        Rlgl.Vertex3f(0.0f, -layer.uv.W * 0.01f, 0.0f);

                        Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                        Rlgl.Vertex3f(layer.uv.Z * 0.01f, -layer.uv.W * 0.01f, 0.0f);

                        Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, layer.uv.Y / texSize.Y);
                        Rlgl.Vertex3f(layer.uv.Z * 0.01f, 0.0f, 0.0f);

                        Rlgl.End();
                        break;
                }

                Rlgl.PopMatrix();
                Raylib.EndBlendMode();
                Rlgl.SetTexture(0);
            }
        }

        public static void Draw2D()
        {
            foreach(PrmAn.Layer layer in layersIn2D)
            {
                Rlgl.DisableBackfaceCulling();
                Rlgl.PushMatrix();
                Rlgl.Translatef(layer.position.X * (Raylib.GetScreenWidth() / 1280.0f), layer.position.Y * (Raylib.GetScreenHeight() / 720.0f), layer.position.Z);
                Rlgl.Rotatef(layer.rotation.Y, 0.0f, 0.1f, 0.0f);
                Rlgl.Rotatef(layer.rotation.X, 0.1f, 0.0f, 0.0f);
                Rlgl.Rotatef(layer.rotation.Z, 0.0f, 0.0f, 0.1f);
                Rlgl.Scalef(layer.scale.X, layer.scale.Y, layer.scale.Z);

                Rlgl.Color4f((layer.colMult[0] + layer.colAdd[0]) / 255.0f, (layer.colMult[1] + layer.colAdd[1]) / 255.0f,
                    (layer.colMult[2] + layer.colAdd[2]) / 255.0f, (layer.colMult[3] + layer.colAdd[3]) / 255.0f);

                if (layer.additive)
                    Raylib.BeginBlendMode(BlendMode.Additive);

                Vector2 texSize;
                if (prmAn.textures.ContainsKey(layer.texId))
                {
                    Rlgl.SetTexture(prmAn.textures[layer.texId].glTexId);
                    texSize = new Vector2(prmAn.textures[layer.texId].width, prmAn.textures[layer.texId].height);
                }
                else
                    texSize = Vector2.Zero;

                switch (layer.primitiveType)
                {
                    default:
                    case PrmAn.PrimitiveType.Plane:
                        Rlgl.Begin(DrawMode.Quads);

                        Rlgl.TexCoord2f(layer.uv.X / texSize.X, layer.uv.Y / texSize.Y);
                        Rlgl.Vertex2f(0.0f, 0.0f);

                        Rlgl.TexCoord2f(layer.uv.X / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                        Rlgl.Vertex2f(0.0f, layer.uv.W * (Raylib.GetScreenHeight() / 720.0f));

                        Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                        Rlgl.Vertex2f(layer.uv.Z * (Raylib.GetScreenWidth() / 1280.0f), layer.uv.W * (Raylib.GetScreenHeight() / 720.0f));

                        Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, layer.uv.Y / texSize.Y);
                        Rlgl.Vertex2f(layer.uv.Z * (Raylib.GetScreenWidth() / 1280.0f), 0.0f);

                        Rlgl.End();
                        break;
                }

                Rlgl.PopMatrix();
                Raylib.EndBlendMode();
                Rlgl.SetTexture(0);
            }
            layersIn2D.Clear();
        }

        public static void Close()
        {
            foreach (PrmAn.Texture tex in textures.Values)
                Rlgl.UnloadTexture(tex.glTexId);
        }

        public static void AddFrameAnim(PrmAn.Animation anim)
        {
            string[] frames = anim.frames;
            int[] times = anim.frameTimes;
            anim.frames = new string[anim.frameCount + 1];
            anim.frameTimes = new int[anim.frameCount + 1];
            Array.Copy(frames, anim.frames, anim.frameCount);
            Array.Copy(times, anim.frameTimes, anim.frameCount);
            anim.frames[anim.frameCount] = "";
            anim.frameTimes[anim.frameCount] = 0;
            anim.frameCount++;
        }

        public static void RemoveFrameAnim(PrmAn.Animation anim, int num)
        {
            string[] frames = anim.frames;
            int[] times = anim.frameTimes;
            anim.frames = new string[anim.frameCount - 1];
            anim.frameTimes = new int[anim.frameCount - 1];
            if (num == 0)
            {
                Array.Copy(frames, 1, anim.frames, 0, frames.Length - 1);
                Array.Copy(times, 1, anim.frameTimes, 0, frames.Length - 1);
            }
            else if (num == anim.frameCount - 1)
            {
                Array.Copy(frames, 0, anim.frames, 0, frames.Length - 1);
                Array.Copy(times, 0, anim.frameTimes, 0, frames.Length - 1);
            }
            else
            {
                Array.Copy(frames, 0, anim.frames, 0, num);
                Array.Copy(frames, num + 1, anim.frames, num, frames.Length - num - 1);
                Array.Copy(times, 0, anim.frameTimes, 0, num);
                Array.Copy(times, num + 1, anim.frameTimes, num, frames.Length - num - 1);
            }
            anim.frameCount--;
        }
    }
}
