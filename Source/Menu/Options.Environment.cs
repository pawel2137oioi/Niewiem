using System;
using System.Drawing;
using System.Windows.Forms;

namespace Menu
{
    public partial class OptionsForm
    {
        ComboBox environmentQuality;
        NumericUpDown environmentIntensity;
        CheckBox environmentSunGlare;
        NumericUpDown environmentGroundFog;
        NumericUpDown environmentFogHeight;
        NumericUpDown environmentCloudResolution;
        CheckBox environmentColorGrading, environmentBloom, environmentWaterReflections, environmentVolumetricClouds;
        bool applyBalancedShadowSettings;

        void InitializeEnvironmentOptions()
        {
            var page = new TabPage(catalog.GetString("Environment")) { AutoScroll = true };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            environmentQuality = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill,
            };
            environmentQuality.Items.AddRange(new object[]
            {
                catalog.GetString("Original"),
                catalog.GetString("Balanced"),
                catalog.GetString("High"),
            });
            environmentQuality.SelectedIndex = Math.Clamp(Settings.EnvironmentQuality, 0, 2);
            environmentIntensity = new NumericUpDown
            {
                Minimum = 0, Maximum = 100, Increment = 5, Width = 90,
                Value = Math.Clamp(Settings.EnvironmentIntensity, 0, 100),
            };
            environmentSunGlare = new CheckBox
            {
                AutoSize = true, Text = catalog.GetString("Sun halo"),
                Checked = Settings.EnvironmentSunGlare,
            };
            layout.Controls.Add(new Label { AutoSize = true, Text = catalog.GetString("Environment quality") }, 0, 0);
            layout.Controls.Add(environmentQuality, 1, 0);
            layout.Controls.Add(new Label { AutoSize = true, Text = catalog.GetString("Effect strength (%)") }, 0, 1);
            layout.Controls.Add(environmentIntensity, 1, 1);
            layout.Controls.Add(environmentSunGlare, 1, 2);
            environmentGroundFog = new NumericUpDown
            {
                Minimum = 0, Maximum = 100, Increment = 5, Width = 90,
                Value = Math.Clamp(Settings.EnvironmentGroundFog, 0, 100),
            };
            environmentFogHeight = new NumericUpDown
            {
                Minimum = 5, Maximum = 500, Increment = 5, Width = 90,
                Value = Math.Clamp(Settings.EnvironmentFogHeight, 5, 500),
            };
            layout.Controls.Add(new Label { AutoSize = true, Text = catalog.GetString("Ground fog (%)") }, 0, 3);
            layout.Controls.Add(environmentGroundFog, 1, 3);
            layout.Controls.Add(new Label { AutoSize = true, Text = catalog.GetString("Fog layer height (m)") }, 0, 4);
            layout.Controls.Add(environmentFogHeight, 1, 4);
            environmentColorGrading = new CheckBox { AutoSize = true, Text = catalog.GetString("Filmic scene colors"), Checked = Settings.EnvironmentColorGrading };
            environmentBloom = new CheckBox { AutoSize = true, Text = catalog.GetString("Soft highlight bloom"), Checked = Settings.EnvironmentBloom };
            environmentWaterReflections = new CheckBox { AutoSize = true, Text = catalog.GetString("Scene reflections in water (High)"), Checked = Settings.EnvironmentWaterReflections };
            layout.Controls.Add(environmentColorGrading, 1, 5);
            layout.Controls.Add(environmentBloom, 1, 6);
            layout.Controls.Add(environmentWaterReflections, 1, 7);
            environmentVolumetricClouds = new CheckBox { AutoSize = true, Text = catalog.GetString("Volumetric clouds (High)"), Checked = Settings.EnvironmentVolumetricClouds };
            layout.Controls.Add(environmentVolumetricClouds, 1, 8);
            environmentCloudResolution = new NumericUpDown
            {
                Minimum = 25, Maximum = 100, Increment = 25, Width = 90,
                Value = Math.Clamp(Settings.EnvironmentCloudResolution, 25, 100),
            };
            layout.Controls.Add(new Label { AutoSize = true, Text = catalog.GetString("Cloud resolution (%)") }, 0, 9);
            layout.Controls.Add(environmentCloudResolution, 1, 9);
            var description = new Label
            {
                AutoSize = true, MaximumSize = new Size(540, 0), Margin = new Padding(0, 16, 0, 0),
                Text = catalog.GetString("The new renderer uses HDR scene targets, filmic colors, bloom, FXAA, shaded vegetation and reflective water. High adds depth-aware volumetric clouds with self-shadowing, moving cloud shadows and half-resolution scene reflections in water. Clouds follow the current weather. HUD colors are preserved; filmic processing is disabled in the 2D cab view. Requires DirectX feature level 10.0 or newer. MSTS environments use the original renderer. Route geometry and textures still affect the result."),
            };
            layout.Controls.Add(description, 0, 10);
            layout.SetColumnSpan(description, 2);
            var preset = new Button
            {
                AutoSize = true, Margin = new Padding(0, 16, 0, 0),
                Text = catalog.GetString("Apply 1080p / 60 FPS target"),
            };
            preset.Click += (sender, args) =>
            {
                environmentQuality.SelectedIndex = 1;
                environmentIntensity.Value = 70;
                environmentSunGlare.Checked = true;
                environmentColorGrading.Checked = environmentBloom.Checked = environmentWaterReflections.Checked = true;
                environmentVolumetricClouds.Checked = true;
                environmentCloudResolution.Value = 25;
                checkUseMSTSEnv.Checked = false;
                environmentGroundFog.Value = 35;
                environmentFogHeight.Value = 40;
                comboWindowSize.Text = "1920x1080";
                checkModelInstancing.Checked = true;
                checkDynamicShadows.Checked = true;
                checkShadowAllShapes.Checked = false;
                trackAntiAliasing.Value = (int)ORTS.Settings.UserSettings.AntiAliasingMethod.MSAA2x;
                numericViewingDistance.Value = 2000;
                checkPerformanceTuner.Checked = true;
                numericPerformanceTunerTarget.Value = 60;
                numericPerformanceTunerTarget.Enabled = labelPerformanceTunerTarget.Enabled = true;
                applyBalancedShadowSettings = true;
            };
            layout.Controls.Add(preset, 0, 11);
            layout.SetColumnSpan(preset, 2);
            var highPreset = new Button { AutoSize = true, Text = catalog.GetString("Apply High visual preset"), Margin = new Padding(0, 8, 0, 0) };
            highPreset.Click += (sender, args) =>
            {
                preset.PerformClick();
                environmentQuality.SelectedIndex = 2;
                environmentIntensity.Value = 100;
                environmentCloudResolution.Value = 50;
                checkPerformanceTuner.Checked = false;
                checkShadowAllShapes.Checked = true;
            };
            layout.Controls.Add(highPreset, 0, 12);
            layout.SetColumnSpan(highPreset, 2);
            environmentQuality.SelectedIndexChanged += (sender, args) =>
            {
                environmentIntensity.Enabled = environmentSunGlare.Enabled = environmentQuality.SelectedIndex > 0;
                environmentGroundFog.Enabled = environmentFogHeight.Enabled = environmentQuality.SelectedIndex > 0;
            };
            environmentIntensity.Enabled = environmentSunGlare.Enabled = environmentQuality.SelectedIndex > 0;
            environmentGroundFog.Enabled = environmentFogHeight.Enabled = environmentQuality.SelectedIndex > 0;
            page.Controls.Add(layout);
            tabOptions.TabPages.Add(page);
        }

        void SaveEnvironmentOptions()
        {
            Settings.EnvironmentQuality = environmentQuality.SelectedIndex;
            Settings.EnvironmentIntensity = (int)environmentIntensity.Value;
            Settings.EnvironmentSunGlare = environmentSunGlare.Checked;
            Settings.EnvironmentGroundFog = (int)environmentGroundFog.Value;
            Settings.EnvironmentFogHeight = (int)environmentFogHeight.Value;
            Settings.EnvironmentColorGrading = environmentColorGrading.Checked;
            Settings.EnvironmentBloom = environmentBloom.Checked;
            Settings.EnvironmentWaterReflections = environmentWaterReflections.Checked;
            Settings.EnvironmentVolumetricClouds = environmentVolumetricClouds.Checked;
            Settings.EnvironmentCloudResolution = (int)environmentCloudResolution.Value;
            if (applyBalancedShadowSettings)
            {
                // Defer mutation until OK; Cancel must preserve the user's settings.
                Settings.ShadowMapCount = 2;
                Settings.ShadowMapResolution = environmentQuality.SelectedIndex >= 2 ? 2048 : 1024;
                Settings.ShadowMapDistance = 0;
                Settings.ShadowMapBlur = true;
                Settings.DirectXFeatureLevel = ""; // Re-detect the available profile when the activity starts.
            }
        }
    }
}
