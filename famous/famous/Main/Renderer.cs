using System.IO.Enumeration;
using System.Numerics;
using ClickableTransparentOverlay;
using ImGuiNET;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using famous.Icons;

namespace famous.Main;

public class Renderer : Overlay
{
    private readonly string[] _comboItems = { "Global", "Rifles", "Pistols", "Snipers" };
    private int _comboBox;
    private float aimFov = 0f;

    private bool aimEnabled = false;
    private bool espEnabled = false;
    private bool showBox = false;
    private bool showWindow = true;

    private string _selectedTab = "Aim";

    protected override unsafe Task PostInitialized()
    {
        ReplaceFont(config =>
        {
            var io = ImGui.GetIO();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            string mainFontPath = Path.Combine(basePath, "Fonts", "Inter-Medium.ttf");
            string iconFontPath = Path.Combine(basePath, "Fonts", "lucide.ttf");

            ImFontPtr baseFont = default;

            if (File.Exists(mainFontPath))
            {
                baseFont = io.Fonts.AddFontFromFileTTF(mainFontPath, 20, config, io.Fonts.GetGlyphRangesDefault());
                Console.WriteLine("Loaded main font.");
            }

            if ((IntPtr)baseFont.NativePtr == IntPtr.Zero)
            {
                Console.WriteLine("Main font failed. Using default.");
                baseFont = io.Fonts.AddFontDefault();
            }

            if (File.Exists(iconFontPath))
            {
                ushort[] glyphRanges = { 0xE000, 0xF8FF, 0 };
                config->MergeMode = 1;
                config->OversampleH = 1;
                config->OversampleV = 1;
                config->PixelSnapH = 1;

                fixed (ushort* p = glyphRanges)
                {
                    var iconFont = io.Fonts.AddFontFromFileTTF(iconFontPath, 20, config, new IntPtr(p));
                    Console.WriteLine((IntPtr)iconFont.NativePtr != IntPtr.Zero ? "Icon font loaded." : "Failed to load icon font.");
                }
            }
        });

        return base.PostInitialized();
    }

    private bool VerticalCenteredIconWithText(string icon, string text, bool selected = false, float iconScale = 1.1f)
    {
        Vector2 cursorStart = ImGui.GetCursorScreenPos();
        float itemWidth = 180f;
        float itemHeight = 24f;

        if (selected)
        {
            ImGui.GetWindowDrawList().AddRectFilled(
                cursorStart,
                new Vector2(cursorStart.X + itemWidth, cursorStart.Y + itemHeight),
                ImGui.GetColorU32(ImGuiCol.Header),
                4f);
        }

        ImGui.InvisibleButton($"##{text}_tab", new Vector2(itemWidth, itemHeight));
        bool isClicked = ImGui.IsItemClicked();

        ImGui.SetCursorScreenPos(new Vector2(cursorStart.X + 10, cursorStart.Y + (itemHeight - 18 * iconScale) / 2));
        ImGui.SetWindowFontScale(iconScale);
        ImGui.Text(icon);
        ImGui.SetWindowFontScale(1f);

        ImGui.SameLine();
        ImGui.SetCursorScreenPos(new Vector2(cursorStart.X + 34, cursorStart.Y + (itemHeight - ImGui.CalcTextSize(text).Y) / 2));
        ImGui.Text(text);

        return isClicked;
    }

    private void Text(string text, float opacity = 1)
    {
        var col = ImGui.GetColorU32(ImGuiCol.Text);
        var color = ImGui.ColorConvertU32ToFloat4(col);
        color.W = opacity;
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text(text);
        ImGui.PopStyleColor();
    }

    private void RenderTabContent(string tab)
    {
        switch (tab)
        {
            case "Aim":
                ImGui.BeginChild("Left", new Vector2(ImGui.GetContentRegionAvail().X * 0.5f, 0));
                ImGui.Text("Aimbot:");
                ImGui.Separator();
                ImGui.Checkbox("Enable", ref aimEnabled);
                ImGui.Combo("Weapon Config", ref _comboBox, _comboItems, _comboItems.Length);
                ImGui.SliderFloat("FOV", ref aimFov, 1f, 180f);
                ImGui.EndChild();

                ImGui.SameLine();

                ImGui.BeginChild("Right");
                ImGui.Text("Triggerbot:");
                ImGui.Separator();
                ImGui.EndChild();
                break;

            case "Player":
                ImGui.Text("Player Visuals");
                ImGui.Checkbox("Show Box", ref showBox);
                break;

            case "World":
                ImGui.Text("World Visuals");
                ImGui.Checkbox("Enable ESP", ref espEnabled);
                break;

            case "Themes":
                ImGui.Text("Theme Settings");
                break;

            case "Config":
                ImGui.Text("Configuration Options");
                break;

            case "Overlay":
                ImGui.Text("Overlay Control");
                break;
        }
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    protected override void Render()
    {
        if (GetAsyncKeyState(0x2D) < 0) // INSERT key
        {
            showWindow = !showWindow;
            Thread.Sleep(200);
        }

        if (!showWindow) return;

        ImGui.SetNextWindowSize(new Vector2(1000, 500));
        ImGui.Begin("Overlay UI", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize);

        float windowWidth = ImGui.GetWindowSize().X;
        float textWidth = ImGui.CalcTextSize("Your Title Here").X;
        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
        ImGui.Text("Your Title Here");
        ImGui.Dummy(new Vector2(0, 10));

        ImGui.BeginChild("Tabs", new Vector2(200, 0), ImGuiChildFlags.Border);
        Text("Combat", 5f);
        if (VerticalCenteredIconWithText(Lucide.Crosshair, "Aim", _selectedTab == "Aim")) _selectedTab = "Aim";
        ImGui.Dummy(new Vector2(0, 10));

        Text("Visuals");
        if (VerticalCenteredIconWithText(Lucide.User, "Player", _selectedTab == "Player")) _selectedTab = "Player";
        if (VerticalCenteredIconWithText(Lucide.Globe, "World", _selectedTab == "World")) _selectedTab = "World";
        ImGui.Dummy(new Vector2(0, 10));

        Text("Misc");
        if (VerticalCenteredIconWithText("🎨", "Themes", _selectedTab == "Themes")) _selectedTab = "Themes";
        if (VerticalCenteredIconWithText(Lucide.File, "Config", _selectedTab == "Config")) _selectedTab = "Config";
        if (VerticalCenteredIconWithText(Lucide.Computer, "Overlay", _selectedTab == "Overlay")) _selectedTab = "Overlay";
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("TabContent", new Vector2(0, 0), ImGuiChildFlags.Border);
        RenderTabContent(_selectedTab);
        ImGui.EndChild();

        ImGui.End();
    }
}
