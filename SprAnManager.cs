using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using BlakieLibSharp;
using Raylib_cs;
using ImGuiNET;
using static BlakieLibSharp.SprAn;
using static BlakieLibSharp.RectCollider;

namespace PrmAnEditor
{
    internal static class SprAnManager
    {
        public static SprAn sprAn = new SprAn();
        public static DPSpr dpspr = null;

        static Shader palShader;
        static Texture2D palTexture = new Texture2D();
        static int palLoc;
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

        static readonly Color[] ColliderTypeColors = {
            new Color(255, 255, 60, 90),
            new Color(90, 255, 60, 90),
            new Color(255, 60, 60, 90),
            new Color(60, 75, 255, 90),
            new Color(175, 60, 255, 90),
            new Color(175, 100, 255, 90),
            new Color(175, 140, 255, 90),
            new Color(145, 20, 20, 90),
            new Color(79, 255, 226, 90)
        };

        public static void LoadShader()
        {
            palShader = Raylib.LoadShaderFromMemory(palShaderVert, palShaderFrag);
            palLoc = Raylib.GetShaderLocation(palShader, "palette");
        }

        public static void UnloadShader()
        {
            Raylib.UnloadShader(palShader);
            Raylib.UnloadTexture(palTexture);
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

        public static void LoadPal(string path)
        {
            palTexture = Raylib.LoadTexture(path);
            Raylib.SetTextureFilter(palTexture, TextureFilter.Point);
        }

        static int curState = 0;
        static int curFrame = 0;
        static int curUv = 0;
        static int curCol = 0;
        static bool copyFrame = true;
        static bool animating = false;
        static int animTimer = 0;
        static SprAnFrame copiedFrame = null;
        static SprAnState copiedState = null;

        public static void DrawUI()
        {
            string[] stateNames = sprAn.GetAllStateNames();
            if(stateNames.Length <= 0)
            {
                ImGui.Text("No States Loaded");
                if (ImGui.Button("Add State"))
                    AddState();
                return;
            }

            ImGui.SetNextItemWidth(150.0f);
            if(ImGui.BeginCombo("Current State", stateNames[curState]))
            {
                int stateCount = stateNames.Length;
                for(int i = 0; i < stateCount; i++)
                {
                    bool isSelected = curState == i;
                    if (ImGui.Selectable(stateNames[i], isSelected))
                    {
                        curState = i;
                        curFrame = 0;
                        curUv = 0;
                        curCol = 0;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            SprAnState state = sprAn.GetState(stateNames[curState]);
            string name = state.name;
            ImGui.SetNextItemWidth(125.0f);
            ImGui.InputText("State Name", ref name, 32);
            if(name != state.name)
            {
                sprAn.ReKeyState(state.name, name);
                state.name = name;
            }

            ImGui.SetNextItemWidth(120.0f);
            if (ImGui.Button("Add State"))
                AddState();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120.0f);
            if(ImGui.Button("Remove State"))
            {
                RemoveState();
                return;
            }

            if(state.frameCount <= 0)
            {
                ImGui.Text("State has no frames");
                ImGui.SetNextItemWidth(120.0f);
                if (ImGui.Button("Add Frame"))
                    AddFrame(state, false);
                return;
            }

            ImGui.SliderInt("Current Frame", ref curFrame, 0, state.frameCount - 1);

            if (Raylib.IsKeyPressed(KeyboardKey.Left) && curFrame > 0)
                curFrame--;
            else if(Raylib.IsKeyPressed(KeyboardKey.Right) && curFrame < state.frameCount - 1)
                curFrame++;
            
            SprAnFrame frame = state.frames[curFrame];
            int length = frame.frameLength;
            ImGui.InputInt("Frame Length", ref length);
            frame.frameLength = (ushort)length;
            ImGui.Checkbox("Animating", ref animating);

            if(ImGui.TreeNode("Uv Data"))
            {
                if (frame.uvCount == 0)
                    ImGui.Text("Frame has no uvs");
                ImGui.SetNextItemWidth(125.0f);
                if (ImGui.Button("Add Uv"))
                    AddUv(frame);
                if(frame.uvCount > 0)
                {
                    ImGui.SetNextItemWidth(125.0f);
                    if (ImGui.Button("Remove Uv"))
                    {
                        RemoveUv(frame);
                        goto EndUv;
                    }

                    ImGui.SliderInt("Current Uv", ref curUv, 0, frame.uvCount - 1);

                    ref FrameUv uv = ref frame.uvs[curUv];
                    ImGui.InputText("Sprite", ref uv.textureName, 32);

                    ImGui.DragFloat2("Position", ref uv.position, 0.01f);
                    ImGui.DragFloat3("Rotation", ref uv.rotation, 0.5f);
                    ImGui.DragFloat2("Scale", ref uv.scale, 0.05f);

                    Vector2 uvDat = new Vector2(uv.uv.X, uv.uv.Y);
                    ImGui.DragFloat2("UV Pos", ref uvDat);
                    uv.uv.X = uvDat.X; uv.uv.Y = uvDat.Y;
                    uvDat = new Vector2(uv.uv.Z,  uv.uv.W);
                    ImGui.DragFloat2("UV Size", ref uvDat);
                    uv.uv.Z = uvDat.X; uv.uv.W = uvDat.Y;
                }

            EndUv:
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Collision Data"))
            {
                if (frame.colliderCount == 0)
                    ImGui.Text("Frame has no colliders");
                if (frame.colliderCount < 20)
                {
                    ImGui.SetNextItemWidth(125.0f);
                    if (ImGui.Button("Add Collider"))
                        AddCol(frame);
                }
                if (frame.colliderCount > 0)
                {
                    ImGui.SetNextItemWidth(125.0f);
                    if (ImGui.Button("Remove Collider"))
                    {
                        RemoveCol(frame);
                        goto EndCol;
                    }


                    ImGui.SliderInt("Current Collider", ref curCol, 0, frame.colliderCount - 1);
                    ref RectCollider col = ref frame.colliders[curCol];

                    if (ImGui.BeginCombo("Collider Type", Enum.GetNames(typeof(ColliderType))[(int)col.colliderType]))
                    {
                        const int colliderTypeCount = (int)ColliderType.ColliderTypeCount;
                        for (int i = 0; i < colliderTypeCount; i++)
                        {
                            bool is_selected = (int)col.colliderType == i;
                            if (ImGui.Selectable(Enum.GetNames(typeof(ColliderType))[i], is_selected))
                                col.colliderType = (ColliderType)i;

                            if (is_selected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }

                    Vector2 boxDat = new Vector2(col.x, col.y);
                    ImGui.DragFloat2("Collider Pos", ref boxDat, 0.01f);
                    col.x = boxDat.X; col.y = boxDat.Y;
                    boxDat = new Vector2(col.width, col.height);
                    ImGui.DragFloat2("Collider Size", ref boxDat, 0.01f);
                    col.width = boxDat.X; col.height = boxDat.Y;
                }

            EndCol:
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Tools"))
            {
                ImGui.Checkbox("Copy frame on add", ref copyFrame);
                ImGui.SetNextItemWidth(120.0f);
                if (ImGui.Button("Add Frame"))
                    AddFrame(state, copyFrame);
                ImGui.SetNextItemWidth(120.0f);
                ImGui.SameLine();
                if (ImGui.Button("Remove Frame"))
                    RemoveFrame(state);

                ImGui.SetNextItemWidth(120.0f);
                if (ImGui.Button("Copy Frame"))
                    copiedFrame = frame.Copy();
                ImGui.SetNextItemWidth(120.0f);
                ImGui.SameLine();
                if (ImGui.Button("Paste Frame"))
                    state.frames[curFrame] = copiedFrame.Copy();

                ImGui.TreePop();
            }
        }

        public static void DrawSprites()
        {
            string[] stateNames = sprAn.GetAllStateNames();
            if (stateNames.Length == 0)
                return;
            if (sprAn.GetState(stateNames[curState]).frameCount <= 0)
                return;
            SprAnFrame frame = sprAn.GetFrame(stateNames[curState], curFrame);
            if (frame == null)
                return;

            if (animating)
            {
                animTimer++;
                if (animTimer >= frame.frameLength)
                {
                    curFrame++;
                    if (curFrame >= sprAn.GetState(stateNames[curState]).frameCount)
                        curFrame = 0;
                    frame = sprAn.GetFrame(stateNames[curState], curFrame);
                    animTimer = 0;
                }
            }
            else
                animTimer = 0;
            foreach (FrameUv uv in frame.uvs)
                DrawSprite(uv);

            foreach(RectCollider col in frame.colliders)
            {
                Color color = ColliderTypeColors[(int)col.colliderType];
                Raylib.DrawCube(new Vector3(col.x + col.width / 2.0f, col.y + col.height / 2.0f, 0.001f), col.width, col.height, 0.0f, color);
            }
        }

        static void DrawSprite(FrameUv frame)
        {
            if (dpspr == null)
                return;
            if (!dpspr.HasSprite(frame.textureName))
                return;

            DPSpr.Sprite sprite = dpspr.GetSprite(frame.textureName);
            if (sprite.glTexId == 0)
                return;

            Rlgl.SetTexture(sprite.glTexId);
            Rlgl.DisableBackfaceCulling();
            if (sprite.indexed && palTexture.Id != 0)
            {
                Raylib.BeginShaderMode(palShader);
                Raylib.SetShaderValueTexture(palShader, palLoc, palTexture);
            }

            Rlgl.PushMatrix();
            {
                Rlgl.Translatef(frame.position.X, frame.position.Y, 0.0f);
                Rlgl.Rotatef(frame.rotation.Y, 0.0f, 1.0f, 0.0f);
                Rlgl.Rotatef(frame.rotation.X, 1.0f, 0.0f, 0.0f);
                Rlgl.Rotatef(frame.rotation.Z, 0.0f, 0.0f, 1.0f);
                Rlgl.Scalef(frame.scale.X, frame.scale.Y, 1.0f);

                Rlgl.Begin(DrawMode.Quads);

                Rlgl.Color4ub(255, 255, 255, 255);
                Rlgl.Normal3f(0.0f, 0.0f, 1.0f);

                if (frame.uv.Z == 0.0f || frame.uv.W == 0.0f) //width or height of 0, draw full texture instead
                {
                    Rlgl.TexCoord2f(0.0f, 0.0f);
                    Rlgl.Vertex3f(0.0f, 0.0f, 0.0f);

                    Rlgl.TexCoord2f(0.0f, 1.0f);
                    Rlgl.Vertex3f(0.0f, -sprite.height * 0.01f, 0.0f);

                    Rlgl.TexCoord2f(1.0f, 1.0f);
                    Rlgl.Vertex3f(sprite.width * 0.01f, -sprite.height * 0.01f, 0.0f);

                    Rlgl.TexCoord2f(1.0f, 0.0f);
                    Rlgl.Vertex3f(sprite.width * 0.01f, -0.0f, 0.0f);
                }
                else
                {
                    Rlgl.TexCoord2f(frame.uv.X / sprite.width, frame.uv.Y / sprite.height);
                    Rlgl.Vertex3f(0.0f, 0.0f, 0.0f);

                    Rlgl.TexCoord2f(frame.uv.X / sprite.width, (frame.uv.Y + frame.uv.W) / sprite.height);
                    Rlgl.Vertex3f(0.0f, -frame.uv.W * 0.01f, 0.0f);

                    Rlgl.TexCoord2f((frame.uv.X + frame.uv.Z) / sprite.width, (frame.uv.Y + frame.uv.W) / sprite.height);
                    Rlgl.Vertex3f(frame.uv.Z * 0.01f, -frame.uv.W * 0.01f, 0.0f);

                    Rlgl.TexCoord2f((frame.uv.X + frame.uv.Z) / sprite.width, frame.uv.Y / sprite.height);
                    Rlgl.Vertex3f(frame.uv.Z * 0.01f, 0.0f, 0.0f);
                }

                Rlgl.End();
            }
            Rlgl.PopMatrix();
            Rlgl.SetTexture(0);

            Raylib.EndShaderMode();
        }

        public static void AddState()
        {
            SprAnState state = new SprAnState();
            state.name = "New State " + sprAn.GetAllStateNames().Length;
            state.frames = new SprAnFrame[0];
            sprAn.AddState(state);
            curState++;
            curFrame = 0;
            curUv = 0;
            curCol = 0;
        }

        public static void RemoveState()
        {
            sprAn.RemoveState(sprAn.GetAllStateNames()[curState]);
            curState--;
            if (curState < 0)
                curState = 0;
        }

        public static void AddFrame(SprAnState state, bool copyFrame)
        {
            SprAnFrame[] frames = state.frames;
            state.frames = new SprAnFrame[frames.Length + 1];
            Array.Copy(frames, state.frames, frames.Length);
            state.frameCount++;
            state.frames[frames.Length] = copyFrame ? frames[frames.Length - 1].Copy() : new SprAnFrame();
            curCol = 0;
            curUv = 0;
            curFrame++;
            if (curFrame >= state.frames.Length)
                curFrame--;
        }

        public static void RemoveFrame(SprAnState state)
        {
            SprAnFrame[] frames = state.frames;
            state.frames = new SprAnFrame[frames.Length - 1];
            if (curFrame == 0)
                Array.Copy(frames, 1, state.frames, 0, frames.Length - 1);
            else if (curFrame == frames.Length - 1)
            {
                Array.Copy(frames, 0, state.frames, 0, frames.Length - 1);
                curFrame--;
            }
            else
            {
                Array.Copy(frames, 0, state.frames, 0, curFrame);
                Array.Copy(frames, curFrame + 1, state.frames, curFrame, frames.Length - curFrame - 1);
                curFrame--;
            }
            state.frameCount--;
            curUv = 0;
            curCol = 0;
        }

        public static void AddUv(SprAnFrame frame)
        {
            FrameUv[] uvs = frame.uvs;
            frame.uvs = new FrameUv[uvs.Length + 1];
            Array.Copy(uvs, frame.uvs, uvs.Length);
            frame.uvs[uvs.Length] = new FrameUv();
            curUv = uvs.Length;
            frame.uvCount++;
        }

        public static void RemoveUv(SprAnFrame frame)
        {
            FrameUv[] uvs = frame.uvs;
            frame.uvs = new FrameUv[uvs.Length - 1];
            int i = curUv;
            while (i < frame.uvCount - 1)
            {
                frame.uvs[i] = uvs[i + 1];
                i++;
            }
            frame.uvCount--;
            curUv--;
            if (curUv < 0)
                curUv = 0;
        }

        public static void AddCol(SprAnFrame frame)
        {
            RectCollider[] cols = frame.colliders;
            frame.colliders = new RectCollider[cols.Length + 1];
            Array.Copy(cols, frame.colliders, cols.Length);
            frame.colliders[cols.Length] = new RectCollider();
            curCol = cols.Length;
            frame.colliderCount++;
        }

        public static void RemoveCol(SprAnFrame frame)
        {
            RectCollider[] cols = frame.colliders;
            frame.colliders = new RectCollider[cols.Length - 1];
            int i = curCol;
            while (i < frame.colliderCount - 1)
            {
                frame.colliders[i] = cols[i + 1];
                i++;
            }
            frame.colliderCount--;
            curCol--;
            if (curCol < 0)
                curCol = 0;
        }
    }
}
