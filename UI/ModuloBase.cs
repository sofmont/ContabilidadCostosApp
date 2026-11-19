using System;
using System.Drawing;
using System.Windows.Forms;
using CostosOP.Datos;

namespace CostosOP.UI
{
    /// <summary>
    /// Clase base de todos los módulos: encabezado, barra de KPI y cuerpo.
    /// Cada módulo es un UserControl independiente que el formulario principal
    /// crea una sola vez y reutiliza (no se pierden capturas al cambiar de módulo).
    /// </summary>
    public class ModuloBase : UserControl
    {
        /// <summary>Mensajes para la barra de estado del formulario principal (texto, esError).</summary>
        public static event Action<string, bool> MensajeEstado;

        protected readonly Repositorio Repo = Repositorio.Instancia;
        protected readonly Panel Encabezado;
        protected readonly FlowLayoutPanel BarraKpi;
        protected readonly Panel Cuerpo;
        private readonly Label lblTitulo;
        private readonly Label lblSubtitulo;

        // Constructor vacío requerido por el diseñador de Visual Studio.
        public ModuloBase() : this("Módulo", string.Empty) { }

        public ModuloBase(string titulo, string subtitulo)
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            Font = Tema.Fuente;
            Padding = new Padding(Tema.E(22), Tema.E(10), Tema.E(22), Tema.E(18));
            DoubleBuffered = true;

            // Encabezado: título y subtítulo apilados; la altura se calcula sola según la fuente.
            Encabezado = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Tema.Fondo,
                Padding = new Padding(0, 0, 0, Tema.E(10))
            };
            lblTitulo = new Label
            {
                Text = titulo, Font = Tema.FuenteTitulo, ForeColor = Tema.Texto,
                AutoSize = true, Margin = new Padding(0, Tema.E(4), 0, 0)
            };
            lblSubtitulo = new Label
            {
                Text = subtitulo, Font = Tema.Fuente, ForeColor = Tema.TextoTenue,
                AutoSize = true, Margin = new Padding(Tema.E(2), Tema.E(2), 0, 0)
            };
            Encabezado.Controls.Add(lblTitulo);
            Encabezado.Controls.Add(lblSubtitulo);

            // Fila de KPI: crece en alto según las tarjetas y las pasa a otra línea si no caben.
            BarraKpi = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, WrapContents = true, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Tema.Fondo,
                Padding = new Padding(0, 0, 0, Tema.E(2))
            };

            Cuerpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo, Padding = new Padding(0, Tema.E(6), 0, 0) };

            Tema.Apilar(this, Encabezado, BarraKpi, Cuerpo);
        }

        protected TarjetaKpi AgregarKpi(string titulo, Color acento)
        {
            var k = new TarjetaKpi(titulo, acento);
            BarraKpi.Controls.Add(k);
            return k;
        }

        /// <summary>Recarga KPI, tablas y combos desde el repositorio. Se llama al mostrar el módulo.</summary>
        public virtual void Refrescar() { }

        /// <summary>
        /// Ejecuta una operación de negocio con manejo uniforme de errores.
        /// La función devuelve el mensaje de éxito que se muestra en la barra de estado.
        /// </summary>
        protected bool Ejecutar(Func<string> operacion)
        {
            try
            {
                string mensaje = operacion();
                Refrescar();
                if (!string.IsNullOrEmpty(mensaje)) Informar(mensaje, false);
                return true;
            }
            catch (ReglaNegocioException ex)
            {
                Informar(ex.Message.Replace("\n", " "), true);
                MessageBox.Show(this, ex.Message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            catch (Exception ex)
            {
                Informar("Error: " + ex.Message, true);
                MessageBox.Show(this, "Ocurrió un error inesperado:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        protected bool Confirmar(string pregunta)
        {
            return MessageBox.Show(this, pregunta, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                   == DialogResult.Yes;
        }

        public static void Informar(string mensaje, bool esError)
        {
            var h = MensajeEstado;
            if (h != null) h(mensaje, esError);
        }
    }
}
