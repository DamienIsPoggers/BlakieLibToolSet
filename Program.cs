using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;
using System.Numerics;
using BlakieLibSharp;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Forms;

namespace PrmAnEditor
{
    internal class Program
    {
        static string filePath = "";
        static string sprFilePath = "";
        static int texUsing = 0;
        static int frameUsing = 0;
        static int layerUsing = 0;
        static bool renderAnim = false;
        static int animTimer = 0;
        static int animUsing = 0;
        static int animFrameUsing = 0;
        static string multipleSprite = "";
        static bool blend = false;
        static float blendAmount = 0.0f;
        static string blendFrame = "";
        

        [STAThread]
        static void Main(string[] args)
        {
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(1280, 720, "PrmAnEditor");
            Raylib.SetTargetFPS(60);
            rlImGui.Setup(true);
            SprAnManager.LoadShader();

            Camera3D cam = new Camera3D();
            cam.FovY = 45.0f;
            cam.Position = new Vector3(0.0f, 2.0f, 8.0f);
            cam.Target = new Vector3(0.0f, 2.0f, 0.0f);
            cam.Projection = CameraProjection.Perspective;
            cam.Up = new Vector3(0.0f, 1.0f, 0.0f);

            while (!Raylib.WindowShouldClose())
            {
                if (Raylib.IsMouseButtonDown(MouseButton.Right))
                {
                    Vector2 mouseDelta = Raylib.GetMouseDelta();
                    cam.Position.X -= mouseDelta.X * 0.01f;
                    cam.Position.Y += mouseDelta.Y * 0.01f;
                    cam.Target.X -= mouseDelta.X * 0.01f;
                    cam.Target.Y += mouseDelta.Y * 0.01f;
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.RayWhite);
                Raylib.BeginMode3D(cam);

                Raylib.DrawLine3D(new Vector3(-25.0f, 0.0f, 0.0f), new Vector3(25.0f, 0.0f, 0.0f), Color.Gray);
                Raylib.DrawLine3D(new Vector3(0.0f, -25.0f, 0.0f), new Vector3(0.0f, 25.0f, 0.0f), Color.Gray);

                SprAnManager.DrawSprites();

                if (!renderAnim && PrmAnManager.prmAn.frames.Count > 0)
                {
                    if(blend && PrmAnManager.prmAn.frames.ContainsKey(blendFrame))
                        PrmAnManager.Draw(PrmAnManager.prmAn.frames.Keys.ToArray()[frameUsing], blendFrame, blendAmount);
                    else
                        PrmAnManager.Draw(PrmAnManager.prmAn.frames.Keys.ToArray()[frameUsing], "", 0.0f);
                }
                if (PrmAnManager.prmAn.frames.ContainsKey(multipleSprite))
                    PrmAnManager.Draw(multipleSprite, "", 0.0f);

                Raylib.EndMode3D();

                PrmAnManager.Draw2D();

                rlImGui.Begin();
                DrawUI();
                rlImGui.End();
                Raylib.DrawFPS(10, 10);
                Raylib.EndDrawing();
            }

            PrmAnManager.Close();
            SprAnManager.UnloadShader();
            SprAnManager.CloseDPSpr();
            rlImGui.Shutdown();
            Raylib.CloseWindow();
        }

        private static void DrawUI()
        {
            #region main menu
            ImGui.BeginMainMenuBar();

            if(ImGui.BeginMenu("File"))
            {
                if (ImGui.BeginMenu("PrmAn"))
                {
                    if (ImGui.MenuItem("New PrmAn"))
                    {
                        PrmAnManager.New();
                        frameUsing = 0;
                        animUsing = 0;
                        layerUsing = 0;
                    }

                    if (ImGui.MenuItem("Open PrmAn"))
                    {
                        filePath = OpenFile("Open PrmAn file", "PrmAn|*.prman");
                        if (File.Exists(filePath))
                        {
                            PrmAnManager.Load(filePath);
                            frameUsing = 0;
                            animUsing = 0;
                            layerUsing = 0;
                        }
                    }

                    if (ImGui.MenuItem("Save PrmAn"))
                    {
                        if (filePath.Length == 0)
                            filePath = SaveFile("Save PrmAn file", "PrmAn|*.prman");
                        if (filePath.Length > 0)
                            PrmAnManager.Save(filePath);
                    }

                    if (ImGui.MenuItem("Save PrmAn As"))
                    {
                        filePath = SaveFile("Save PrmAn file", "PrmAn|*.prman");
                        if (filePath.Length > 0)
                            PrmAnManager.Save(filePath);
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("SprAn"))
                {
                    if (ImGui.MenuItem("New SprAn"))
                        SprAnManager.New();

                    if (ImGui.MenuItem("Open SprAn"))
                    {
                        sprFilePath = OpenFile("Open SprAn file", "SprAn|*.spran");
                        if (File.Exists(sprFilePath))
                            SprAnManager.Load(sprFilePath);
                    }

                    if(ImGui.MenuItem("Save SprAn"))
                    {
                        if (sprFilePath.Length == 0)
                            sprFilePath = SaveFile("Save SprAn File", "SprAn|*.spran");
                        if (sprFilePath.Length > 0)
                            SprAnManager.Save(sprFilePath);
                    }

                    if(ImGui.MenuItem("Save SprAn As"))
                    {
                        sprFilePath = SaveFile("Save SprAn File", "SprAn|*.spran");
                        if (sprFilePath.Length > 0)
                            SprAnManager.Save(sprFilePath);
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    if(ImGui.MenuItem("Load DPSpr With BasePal"))
                    {
                        string path = OpenFile("Open DPSpr File", "DPSpr|*.dpspr");
                        if (File.Exists(path))
                            SprAnManager.LoadDPSpr(path, true);
                    }

                    if (ImGui.MenuItem("Load DPSpr With Index"))
                    {
                        string path = OpenFile("Open DPSpr File", "DPSpr|*.dpspr");
                        if (File.Exists(path))
                            SprAnManager.LoadDPSpr(path, false);
                    }

                    if(ImGui.MenuItem("Load DPSpr Palette Tex"))
                    {
                        string path = OpenFile("Open Texture for palette", "PNG|*.png");
                        if(File.Exists(path))
                            SprAnManager.LoadPal(path);
                    }

                    if (ImGui.MenuItem("Close DPSpr"))
                        SprAnManager.CloseDPSpr();

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
            #endregion

            #region Prman
            if (ImGui.Begin("PrmAn"))
            {
                if(ImGui.TreeNode("Animations"))
                {
                    ImGui.SetNextItemWidth(150.0f);
                    if (ImGui.Button("Add Animation"))
                        PrmAnManager.AddAnimation();

                    if(PrmAnManager.prmAn.animations.Count <= 0)
                    {
                        ImGui.Text("No Animations in file");
                        ImGui.TreePop();
                        goto EndAnims;
                    }

                    string[] animNames = PrmAnManager.prmAn.animations.Keys.ToArray();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150.0f);
                    if(ImGui.Button("Remove Animation"))
                    {
                        PrmAnManager.RemoveAnimation(animNames[animUsing]);
                        animUsing = animUsing - 1 >= 0 ? animUsing - 1 : 0;
                        if (PrmAnManager.prmAn.animations.Count <= 0)
                        {
                            ImGui.TreePop();
                            goto EndAnims;
                        }
                    }

                    ImGui.Combo("Animation", ref animUsing, animNames, animNames.Length);

                    ImGui.TreePop();
                }
                EndAnims:

                if(ImGui.TreeNode("Frames"))
                {
                    ImGui.SetNextItemWidth(150.0f);
                    if (ImGui.Button("Add Frame"))
                        PrmAnManager.AddFrame();

                    if (PrmAnManager.prmAn.frames.Count <= 0)
                    {
                        ImGui.Text("No Frames in file");
                        ImGui.TreePop();
                        goto EndFrames;
                    }

                    RestartFrames:
                    string[] frameNames = PrmAnManager.prmAn.frames.Keys.ToArray();
                    string[] frameNamesShow = frameNames;
                    for (int i = 0; i < frameNamesShow.Length; i++)
                        frameNamesShow[i] = frameNamesShow[i].Replace("\\", string.Empty); 
                        //if \ is in the name that means the key already exists, this is so it doesnt die, its not actually in the name

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150.0f);
                    if(ImGui.Button("Remove Frame"))
                    {
                        PrmAnManager.RemoveFrame(frameNames[frameUsing]);
                        frameUsing = frameUsing - 1 >= 0 ? frameUsing - 1 : 0;
                        if (PrmAnManager.prmAn.frames.Count <= 0)
                        {
                            ImGui.TreePop();
                            goto EndFrames;
                        }
                        else
                            goto RestartFrames;
                    }

                    ImGui.Combo("Frame Using", ref frameUsing, frameNamesShow, frameNamesShow.Length);
                    PrmAn.Frame frame = PrmAnManager.prmAn.frames[frameNames[frameUsing]];
                    string frameName = frame.name;
                    string frameName2 = "";
                    ImGui.InputText("Frame Name", ref frameName, 255);
                    if(frameName != frame.name)
                    {
                        if (PrmAnManager.ContainsFrame(frameName))
                        {
                            frameName2 = frameName + "\\";
                            if(PrmAnManager.ContainsFrame(frameName2))
                                PrmAnManager.ReKeyFrame(frameName2, frameName2);
                            else
                                PrmAnManager.ReKeyFrame(frame.name, frameName2);
                        }
                        else
                            PrmAnManager.ReKeyFrame(frame.name, frameName);
                        frame.name = frameName;
                    }

                    ImGui.SetNextItemWidth(150.0f);
                    if(ImGui.Button("Add Layer"))
                    {
                        frame.layerCount++;
                        PrmAn.Layer[] oldLayers = frame.layers;
                        frame.layers = new PrmAn.Layer[oldLayers.Length + 1];
                        Array.Copy(oldLayers, frame.layers, oldLayers.Length);
                        frame.layers[oldLayers.Length] = new PrmAn.Layer();
                    }

                    if (frame.layerCount > 0)
                    {
                        if (layerUsing >= frame.layerCount)
                            layerUsing = frame.layerCount - 1 >= 0 ? frame.layerCount - 1 : 0;

                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(150.0f);
                        if(ImGui.Button("Remove Layer"))
                        {
                            frame.layerCount--;
                            PrmAn.Layer[] oldLayers = frame.layers;
                            frame.layers = new PrmAn.Layer[oldLayers.Length - 1];
                            Array.Copy(oldLayers, frame.layers, oldLayers.Length - 1);
                            layerUsing = layerUsing - 1 >= 0 ? layerUsing - 1 : 0;
                            if(frame.layerCount <= 0)
                            {
                                ImGui.TreePop();
                                goto EndFrames;
                            }
                        }

                        ImGui.SliderInt("Layer", ref layerUsing, 0, frame.layerCount - 1);
                        PrmAn.Layer layer = frame.layers[layerUsing];
                        int primType = (int)layer.primitiveType;
                        string[] primNames = Enum.GetNames(typeof(PrmAn.PrimitiveType));
                        ImGui.Combo("Layer Type", ref primType, primNames, primNames.Length);
                        layer.primitiveType = (PrmAn.PrimitiveType)primType;

                        ImGui.DragFloat3("Position", ref layer.position, layer.drawIn2d ? 1.0f : 0.01f);
                        ImGui.DragFloat3("Rotation", ref layer.rotation, 1.0f);
                        ImGui.DragFloat3("Scale", ref layer.scale, 0.1f);

                        switch(layer.primitiveType)
                        {
                            default:
                            case PrmAn.PrimitiveType.Plane:
                                ImGui.DragFloat4("UV", ref layer.uv, 1.0f);
                                break;
                        }

                        Vector4 color = new Vector4(layer.colMult[0] / 255.0f, layer.colMult[1] / 255.0f, layer.colMult[2] / 255.0f, layer.colMult[3] / 255.0f);
                        ImGui.ColorEdit4("Color", ref color);
                        layer.colMult[0] = (byte)(color.X * 255); layer.colMult[1] = (byte)(color.Y * 255);
                        layer.colMult[2] = (byte)(color.Z * 255); layer.colMult[3] = (byte)(color.W * 255);
                        color = new Vector4(layer.colAdd[0] / 255.0f, layer.colAdd[1] / 255.0f, layer.colAdd[2] / 255.0f, layer.colAdd[3] / 255.0f);
                        ImGui.ColorEdit4("Color Add", ref color);
                        layer.colAdd[0] = (byte)(color.X * 255); layer.colAdd[1] = (byte)(color.Y * 255);
                        layer.colAdd[2] = (byte)(color.Z * 255); layer.colAdd[3] = (byte)(color.W * 255);

                        ImGui.InputInt("TexId", ref layer.texId);

                        ImGui.Checkbox("Additive", ref layer.additive);
                        ImGui.Checkbox("Draw In 2D", ref layer.drawIn2d);
                        frame.layers[layerUsing] = layer;
                    }
                    else
                        ImGui.Text("Frame has no layers");

                    if (frameName2.Length > 0)
                        PrmAnManager.prmAn.frames[frameName2] = frame;
                    else
                        PrmAnManager.prmAn.frames[frameName] = frame;

                    ImGui.Separator();
                    ImGui.InputText("Draw Secondary Frame", ref multipleSprite, 255);
                    ImGui.Checkbox("Blend with frame", ref blend);
                    if(blend)
                    {
                        ImGui.Spacing();
                        ImGui.InputText("Blend Frame", ref blendFrame, 255);
                        ImGui.DragFloat("Blend Amount", ref blendAmount, 0.01f, 0.0f, 1.0f);
                    }

                    ImGui.TreePop();
                }
                EndFrames:

                if(ImGui.TreeNode("Textures"))
                {
                    ImGui.SetNextItemWidth(150.0f);
                    if(ImGui.Button("Load Texture"))
                    {
                        string texPath = OpenFile("Load Texture", "PNG|*.png");
                        PrmAnManager.LoadTex(texPath);
                    }

                    if(PrmAnManager.textures.Count <= 0)
                    {
                        ImGui.Text("No Textures Loaded");
                        ImGui.TreePop();
                        goto EndTextures;
                    }
                    string[] textures = PrmAnManager.textures.Keys.ToArray();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150.0f);
                    if(ImGui.Button("Remove Texture"))
                    {
                        PrmAnManager.RemoveTex(textures[texUsing]);
                        texUsing = 0;
                        if (PrmAnManager.textures.Count <= 0)
                        {
                            ImGui.TreePop();
                            goto EndTextures;
                        }
                        textures = PrmAnManager.textures.Keys.ToArray();
                    }

                    ImGui.Combo("Textures", ref texUsing, textures, textures.Length);
                    PrmAn.Texture tex = PrmAnManager.textures[textures[texUsing]];
                    ImGui.Text("Name: " + tex.name);
                    ImGui.Text("Width: " + tex.width);
                    ImGui.Text("Height: " + tex.height);
                    int oldId = tex.id;
                    ImGui.InputInt("Tex Id: ", ref tex.id);
                    if(tex.id != oldId)
                        PrmAnManager.ReKeyTexture(oldId, tex.id);

                    Vector2 texSize = new Vector2(tex.width / 4.0f, tex.height / 4.0f);
                    ImGui.Image(new IntPtr(tex.glTexId), texSize);

                    ImGui.TreePop();
                }
                EndTextures:
                ImGui.End();
            }
            #endregion

            #region spran
            if(ImGui.Begin("SprAn"))
            {
                SprAnManager.DrawUI();
                ImGui.End();
            }
            #endregion
        }

        public static string OpenFile(string title, string filter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Title = title, Filter = filter };
            openFileDialog.ShowDialog();
            return openFileDialog.FileName;
        }

        public static string SaveFile(string title, string filter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog() { Title = title, Filter = filter };
            saveFileDialog.ShowDialog();
            return saveFileDialog.FileName;
        }
    }
}
