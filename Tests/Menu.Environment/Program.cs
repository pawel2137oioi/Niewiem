using System;
using System.IO;
using System.Windows.Forms;
using ORTS.Common;
using ORTS.Settings;

namespace Menu
{
    // Host the production Environment page without updater/network services or
    // the rest of the menu. Settings and INI persistence use production code.
    public partial class OptionsForm : Form
    {
        readonly UserSettings Settings;
        readonly TestCatalog catalog = new TestCatalog();
        readonly TabControl tabOptions = new TabControl { Dock = DockStyle.Fill };
        readonly CheckBox checkUseMSTSEnv = new CheckBox(), checkDynamicShadows = new CheckBox(),
            checkShadowAllShapes = new CheckBox(), checkModelInstancing = new CheckBox();
        readonly NumericUpDown numericViewingDistance = new NumericUpDown { Maximum = 50000 };
        public OptionsForm(UserSettings settings)
        {
            Settings = settings; ClientSize = new System.Drawing.Size(800, 650);
            checkUseMSTSEnv.Checked = settings.UseMSTSEnv;
            checkDynamicShadows.Checked = settings.DynamicShadows;
            checkShadowAllShapes.Checked = settings.ShadowAllShapes;
            numericViewingDistance.Value = settings.ViewingDistance;
            Controls.Add(tabOptions); InitializeEnvironmentOptions();
        }
        public void Preset(int level) => ApplyEnvironmentPreset(level);
        public void SavePage() => SaveEnvironmentOptions();
        public void Mix()
        {
            environmentCloudQuality.SelectedIndex = 3;
            environmentCloudResolution.Value = 45;
            environmentReflectionQuality.SelectedIndex = 0;
            environmentBloomQuality.SelectedIndex = 1;
            environmentShadowQuality.SelectedIndex = 1;
        }
        public void Verify(int level)
        {
            if (environmentCloudQuality.SelectedIndex != level || environmentShadowQuality.SelectedIndex != level)
                throw new Exception("Preset did not update independent options.");
            if (!environmentCloudQuality.Enabled || !environmentReflectionQuality.Enabled)
                throw new Exception("Modern independent settings are disabled.");
        }
        public void VerifyLegacyControls()
        {
            environmentQuality.SelectedIndex = 0;
            if (environmentCloudQuality.Enabled) throw new Exception("Legacy mode shows active modern cloud controls.");
            environmentQuality.SelectedIndex = 1;
            environmentCloudQuality.SelectedIndex = 0;
            if (environmentCloudResolution.Enabled) throw new Exception("Cloud scale remains active with volumes Off.");
        }
        public void VerifyLayout()
        {
            if (!environmentCloudQuality.Visible || environmentCloudQuality.Width < 100 || environmentCloudQuality.Height < 15)
                throw new Exception("Cloud quality control has no usable visible layout.");
            Console.WriteLine($"Environment layout: {environmentCloudQuality.Bounds}; page {tabOptions.SelectedTab.ClientSize}.");
        }
    }
    internal sealed class TestCatalog { public string GetString(string text) => text; }
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try { Run(args); }
            catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
        }
        static void Run(string[] args)
        {
            var ini = Path.Combine(Path.GetTempPath(), "openrails-quality-" + Guid.NewGuid() + ".ini");
            File.WriteAllText(ini,"[ORTS]\r\n"); SettingsBase.OverrideSettingsLocations(ini,null);
            var settings = new UserSettings(Array.Empty<string>());
            settings.EnvironmentQuality = 2; settings.EnvironmentIntensity = 100;
            settings.EnvironmentVolumetricClouds = true; settings.EnvironmentWaterReflections = true;
            settings.EnvironmentCloudQuality = settings.EnvironmentReflectionQuality = settings.EnvironmentBloomQuality = -1;
            settings.WindowSize = "2560x1440"; settings.VerticalSync = true;
            settings.PerformanceTunerTarget = 144; settings.PerformanceTuner = false;
            settings.ShadowMapResolution = 1536; settings.ShadowMapCount = 3;
            settings.ViewingDistance = 3000;
            for (var level=1;level<=4;level++)
            {
                using var form = new OptionsForm(settings);
                form.Preset(level); form.Verify(level);
                if (settings.EnvironmentCloudQuality != -1 || settings.ShadowMapResolution != 1536 || settings.ViewingDistance != 3000)
                    throw new Exception("Preset mutated settings before OK/after Cancel.");
                if (settings.WindowSize != "2560x1440" || !settings.VerticalSync || settings.PerformanceTunerTarget != 144 || settings.PerformanceTuner)
                    throw new Exception("Preset imposed a display mode, FPS target or tuner.");
            }
            using(var form = new OptionsForm(settings))
            {
                form.Preset(2); form.Mix(); form.SavePage();
                if (settings.EnvironmentCloudQuality != 3 || settings.EnvironmentCloudResolution != 45
                    || settings.EnvironmentReflectionQuality != 0 || settings.EnvironmentBloomQuality != 1
                    || settings.ShadowMapResolution != 1024 || settings.ShadowMapCount != 1)
                    throw new Exception("Mixed feature settings were not saved.");
                form.VerifyLegacyControls();
                if(args.Length>0)
                {
                    form.Mix(); form.Show();
                    var until=DateTime.UtcNow.AddSeconds(5);
                    while(DateTime.UtcNow<until) { Application.DoEvents(); System.Threading.Thread.Sleep(20); }
                    form.VerifyLayout();
                    using var bitmap = new System.Drawing.Bitmap(form.Width,form.Height);
                    form.DrawToBitmap(bitmap,new System.Drawing.Rectangle(0,0,form.Width,form.Height));
                    bitmap.Save(args[0]); form.Hide();
                }
            }
            settings.Save();
            var reload = new UserSettings(Array.Empty<string>());
            if(reload.EnvironmentCloudQuality != 3 || reload.EnvironmentCloudResolution != 45
                || reload.EnvironmentReflectionQuality != 0 || reload.EnvironmentBloomQuality != 1
                || reload.EnvironmentWaterReflections || !reload.EnvironmentVolumetricClouds)
                throw new Exception("Independent settings did not survive the production INI round trip.");
            File.Delete(ini);
            Console.WriteLine("PASS: production quality UI, four presets, mixed levels, deferred save/Cancel, display/FPS preservation and INI round trip.");
        }
    }
}
