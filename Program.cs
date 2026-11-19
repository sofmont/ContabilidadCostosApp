using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace CostosOP
{
    /// <summary>
    /// Punto de entrada de la aplicación.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Cultura mexicana: formato de moneda $1,234.56 y fechas dd/MM/yyyy.
            var cultura = new CultureInfo("es-MX");
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;
            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;

#if NETCOREAPP
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Red de seguridad: ningún error no controlado cierra la aplicación sin aviso.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
                MessageBox.Show("Ocurrió un error inesperado:\n\n" + e.Exception.Message,
                    "Sistema de Costos", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Application.Run(new FrmPrincipal());
        }
    }
}
