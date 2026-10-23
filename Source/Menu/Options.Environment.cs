using System;
using System.Drawing;
using System.Windows.Forms;
using ORTS.Settings;

namespace Menu
{
    public partial class OptionsForm
    {
        ComboBox environmentQuality, environmentCloudQuality, environmentReflectionQuality, environmentBloomQuality, environmentShadowQuality;
        NumericUpDown environmentIntensity, environmentGroundFog, environmentFogHeight, environmentCloudResolution;
        CheckBox environmentSunGlare, environmentColorGrading;
        Label environmentNotice;
        int? environmentPreset;
        bool loadingEnvironment = true;
        bool updatingEnvironmentShadows, environmentShadowsChanged;

        void InitializeEnvironmentOptions()
        {
            var page = new TabPage(catalog.GetString("Environment")) { AutoScroll = true };
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            var tips = new ToolTip { AutoPopDelay = 15000 };
            page.Disposed += (sender, args) => tips.Dispose();
            var row = 0;
            void Add(string title, Control control, string help = null)
            {
                var label = new Label { AutoSize = true, Text = catalog.GetString(title), Margin = new Padding(0, 6, 8, 6) };
                layout.Controls.Add(label, 0, row); layout.Controls.Add(control, 1, row++);
                if (help != null) { tips.SetToolTip(label, catalog.GetString(help)); tips.SetToolTip(control, catalog.GetString(help)); }
            }
            ComboBox Levels(int selected)
            {
                var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
                combo.Items.AddRange(new object[] { catalog.GetString("Off"), catalog.GetString("Low"), catalog.GetString("Medium"), catalog.GetString("High"), catalog.GetString("Ultra") });
                combo.SelectedIndex = Math.Clamp(selected, 0, 4);
                return combo;
            }
            NumericUpDown Number(int value, int min, int max, int increment) => new NumericUpDown
            { Minimum = min, Maximum = max, Increment = increment, Width = 90, Value = Math.Clamp(value, min, max) };

            var presets = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 125 };
            presets.Items.AddRange(new object[] { catalog.GetString("Low"), catalog.GetString("Medium"), catalog.GetString("High"), catalog.GetString("Ultra") });
            presets.SelectedIndex = 1;
            var apply = new Button { Text = catalog.GetString("Apply preset"), AutoSize = true };
            var presetPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            presetPanel.Controls.Add(presets); presetPanel.Controls.Add(apply);
            Add("Starting preset", presetPanel, "Presets are starting points. Change individual effects afterwards. Screen resolution, FPS target and VSync stay under your control.");

            environmentQuality = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            environmentQuality.Items.AddRange(new object[] { catalog.GetString("Original"), catalog.GetString("Modern") });
            environmentQuality.SelectedIndex = Settings.EnvironmentQuality > 0 ? 1 : 0;
            Add("Renderer", environmentQuality, "Modern effects require DirectX 10 or newer. Original uses the legacy renderer.");
            environmentIntensity = Number(Settings.EnvironmentIntensity, 0, 100, 5);
            Add("Effect strength (%)", environmentIntensity);
            environmentCloudQuality = Levels(GraphicsQuality.Resolve(Settings.EnvironmentCloudQuality, Settings.EnvironmentVolumetricClouds, Settings.EnvironmentQuality));
            Add("Volumetric clouds", environmentCloudQuality, "Off uses the cheaper 2D cloud layer. Higher levels improve volume sampling and enable cloud shadows from Medium upwards. Clouds follow the current weather.");
            environmentCloudResolution = Number(Settings.EnvironmentCloudResolution, 25, 100, 5);
            Add("Cloud resolution (%)", environmentCloudResolution, "Quality selects a suggested resolution; you may override it. Higher values increase GPU cost. High is capped at 1280x720; Ultra at 1536x864.");
            environmentReflectionQuality = Levels(GraphicsQuality.Resolve(Settings.EnvironmentReflectionQuality, Settings.EnvironmentWaterReflections, Settings.EnvironmentQuality));
            Add("Water reflections", environmentReflectionQuality, "Off uses a sky approximation. Low to Ultra increase reflection resolution and refresh frequency. Scene reflections can be expensive on busy routes.");
            environmentBloomQuality = Levels(GraphicsQuality.ResolveBloom(Settings.EnvironmentBloomQuality, Settings.EnvironmentBloom, Settings.EnvironmentQuality));
            Add("Bloom", environmentBloomQuality, "Off removes highlight glow. Higher quality increases the resolution of the glow buffers, at a higher GPU and memory cost.");
            var shadowLevel = !Settings.DynamicShadows ? 0 : Settings.ShadowMapCount == 1 && Settings.ShadowMapResolution == 1024 ? 1
                : Settings.ShadowMapCount == 2 && Settings.ShadowMapResolution == 1024 ? 2
                : Settings.ShadowMapCount == 2 && Settings.ShadowMapResolution == 2048 ? 3
                : Settings.ShadowMapCount == 4 && Settings.ShadowMapResolution == 2048 ? 4 : 5;
            environmentShadowQuality = Levels(Math.Min(shadowLevel,4));
            environmentShadowQuality.Items.Add(catalog.GetString("Custom"));
            environmentShadowQuality.SelectedIndex = shadowLevel;
            Add("Shadows", environmentShadowQuality, "Low: one 1024-pixel cascade. Medium: two 1024-pixel cascades. High: two 2048-pixel cascades. Ultra: four 2048-pixel cascades. Custom preserves your existing shadow settings.");
            environmentColorGrading = new CheckBox { AutoSize = true, Text = catalog.GetString("Filmic scene colors"), Checked = Settings.EnvironmentColorGrading };
            Add("Color grading", environmentColorGrading);
            environmentSunGlare = new CheckBox { AutoSize = true, Text = catalog.GetString("Sun halo"), Checked = Settings.EnvironmentSunGlare };
            Add("Sunlight", environmentSunGlare);
            environmentGroundFog = Number(Settings.EnvironmentGroundFog, 0, 100, 5);
            Add("Ground fog (%)", environmentGroundFog, "Set to zero to disable the additional ground fog. Route/weather visibility still applies.");
            environmentFogHeight = Number(Settings.EnvironmentFogHeight, 5, 500, 5);
            Add("Fog layer height (m)", environmentFogHeight);
            environmentNotice = new Label { AutoSize = true, MaximumSize = new Size(650, 0), Margin = new Padding(0, 14, 0, 0) };
            layout.Controls.Add(environmentNotice, 0, row++); layout.SetColumnSpan(environmentNotice, 2);
            var description = new Label
            {
                AutoSize = true, MaximumSize = new Size(650, 0), Margin = new Padding(0, 10, 0, 10),
                Text = catalog.GetString("Mix settings freely: for example, High clouds with reflections Off. Ultra increases GPU and memory use and does not guarantee a particular FPS. Shadows, viewing distance, antialiasing and display settings remain available on the Graphics tab. Presets also set shadows/viewing distance; they do not change screen resolution or FPS target. Click OK and start a new activity to apply changes. Cancel discards changes."),
            };
            layout.Controls.Add(description, 0, row); layout.SetColumnSpan(description, 2);
            apply.Click += (sender, args) => ApplyEnvironmentPreset(presets.SelectedIndex + 1);
            environmentQuality.SelectedIndexChanged += (sender, args) =>
            {
                if (!loadingEnvironment && environmentQuality.SelectedIndex == 1) checkUseMSTSEnv.Checked = false;
                UpdateEnvironmentAvailability();
            };
            environmentCloudQuality.SelectedIndexChanged += (sender, args) =>
            {
                if (!loadingEnvironment && environmentCloudQuality.SelectedIndex > 0)
                    environmentCloudResolution.Value = GraphicsQuality.Clouds(environmentCloudQuality.SelectedIndex).ResolutionPercent;
                UpdateEnvironmentAvailability();
            };
            environmentIntensity.ValueChanged += (sender, args) => UpdateEnvironmentAvailability();
            environmentGroundFog.ValueChanged += (sender, args) => UpdateEnvironmentAvailability();
            checkUseMSTSEnv.CheckedChanged += (sender, args) => UpdateEnvironmentAvailability();
            environmentShadowQuality.SelectedIndexChanged += (sender, args) =>
            {
                if (loadingEnvironment || updatingEnvironmentShadows || environmentShadowQuality.SelectedIndex == 5) return;
                updatingEnvironmentShadows = true;
                environmentShadowsChanged = true;
                checkDynamicShadows.Checked = environmentShadowQuality.SelectedIndex > 0;
                checkShadowAllShapes.Checked = environmentShadowQuality.SelectedIndex >= 3;
                updatingEnvironmentShadows = false;
            };
            checkDynamicShadows.CheckedChanged += (sender, args) =>
            {
                if (updatingEnvironmentShadows) return;
                updatingEnvironmentShadows = true;
                environmentShadowQuality.SelectedIndex = checkDynamicShadows.Checked
                    ? !environmentShadowsChanged && Settings.DynamicShadows ? shadowLevel : 5 : 0;
                updatingEnvironmentShadows = false;
            };
            page.Controls.Add(layout); tabOptions.TabPages.Add(page);
            loadingEnvironment = false;
            UpdateEnvironmentAvailability();
        }

        void UpdateEnvironmentAvailability()
        {
            var modern = environmentQuality.SelectedIndex == 1 && !checkUseMSTSEnv.Checked;
            environmentIntensity.Enabled = modern;
            var active = modern && environmentIntensity.Value > 0;
            environmentCloudQuality.Enabled = environmentReflectionQuality.Enabled = environmentBloomQuality.Enabled = active;
            environmentCloudResolution.Enabled = active && environmentCloudQuality.SelectedIndex > 0;
            environmentColorGrading.Enabled = environmentSunGlare.Enabled = environmentGroundFog.Enabled = active;
            environmentFogHeight.Enabled = active && environmentGroundFog.Value > 0;
            environmentNotice.Text = catalog.GetString(checkUseMSTSEnv.Checked
                ? "MSTS environment is enabled on the Graphics tab; modern effects are bypassed. Choose Modern or apply a preset to enable them."
                : !modern ? "Original renderer selected. Independent modern effects are bypassed."
                : !active ? "Effect strength is zero. Modern effects are bypassed."
                : "Independent quality settings are active. Changes apply to the next activity; no specific FPS is guaranteed.");
        }

        void ApplyEnvironmentPreset(int level)
        {
            environmentPreset = level;
            environmentQuality.SelectedIndex = 1; checkUseMSTSEnv.Checked = false;
            environmentIntensity.Value = 100;
            environmentCloudQuality.SelectedIndex = level;
            environmentCloudResolution.Value = GraphicsQuality.Clouds(level).ResolutionPercent;
            environmentReflectionQuality.SelectedIndex = level == 1 ? 0 : level == 2 ? 1 : level;
            environmentBloomQuality.SelectedIndex = level;
            environmentColorGrading.Checked = environmentSunGlare.Checked = true;
            environmentGroundFog.Value = 35; environmentFogHeight.Value = 40;
            checkModelInstancing.Checked = true;
            environmentShadowQuality.SelectedIndex = level;
            updatingEnvironmentShadows = true;
            checkDynamicShadows.Checked = true; checkShadowAllShapes.Checked = level >= 3;
            updatingEnvironmentShadows = false;
            environmentShadowsChanged = true;
            numericViewingDistance.Value = level == 1 ? 1000 : level == 2 ? 2000 : level == 3 ? 2500 : 4000;
            UpdateEnvironmentAvailability();
        }

        void SaveEnvironmentOptions()
        {
            Settings.EnvironmentQuality = environmentQuality.SelectedIndex == 0 ? 0 : 2;
            Settings.EnvironmentIntensity = (int)environmentIntensity.Value;
            Settings.EnvironmentSunGlare = environmentSunGlare.Checked;
            Settings.EnvironmentGroundFog = (int)environmentGroundFog.Value;
            Settings.EnvironmentFogHeight = (int)environmentFogHeight.Value;
            Settings.EnvironmentColorGrading = environmentColorGrading.Checked;
            Settings.EnvironmentCloudQuality = environmentCloudQuality.SelectedIndex;
            Settings.EnvironmentReflectionQuality = environmentReflectionQuality.SelectedIndex;
            Settings.EnvironmentBloomQuality = environmentBloomQuality.SelectedIndex;
            Settings.EnvironmentVolumetricClouds = Settings.EnvironmentCloudQuality > 0;
            Settings.EnvironmentWaterReflections = Settings.EnvironmentReflectionQuality > 0;
            Settings.EnvironmentBloom = Settings.EnvironmentBloomQuality > 0;
            Settings.EnvironmentCloudResolution = (int)environmentCloudResolution.Value;
            if (environmentShadowsChanged && environmentShadowQuality.SelectedIndex < 5)
            {
                var level = environmentShadowQuality.SelectedIndex;
                Settings.ShadowMapCount = level == 0 ? 0 : level == 1 ? 1 : level == 4 ? 4 : 2;
                Settings.ShadowMapResolution = level <= 2 ? 1024 : 2048;
                Settings.ShadowMapDistance = 0; Settings.ShadowMapBlur = true;
            }
            if (environmentPreset.HasValue) Settings.DirectXFeatureLevel = "";
        }
    }
}
