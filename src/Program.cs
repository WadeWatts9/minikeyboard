using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("MINI KeyBoard - Cyberpunk Control Studio")]
[assembly: AssemblyDescription("Software de configuración en español para miniteclado macro USB HID (12 teclas + 2 knobs)")]
[assembly: AssemblyCompany("ACDEV Cyberpunk Studio")]
[assembly: AssemblyProduct("MINI KeyBoard Cyberpunk Edition")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]

namespace MiniKeyboardCyber
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length >= 1 && string.Equals(args[0], "--generate-docs-assets", StringComparison.OrdinalIgnoreCase))
            {
                string outDir = args.Length >= 2
                    ? args[1]
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "capturas");

                using (MainForm form = new MainForm())
                {
                    form.GenerateDocumentationAssets(outDir);
                }
                return;
            }

            Application.Run(new MainForm());
        }
    }
}
