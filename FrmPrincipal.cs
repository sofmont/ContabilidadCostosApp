using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CostosOP.Datos;
using CostosOP.Modulos;
using CostosOP.UI;

namespace CostosOP
{
    /// <summary>
    /// Formulario contenedor tipo Dashboard: barra de navegación superior fija,
    /// área de contenido donde se intercambian los módulos y barra de estado.
    /// Los módulos se crean una sola vez (carga diferida) y se reutilizan.
    /// Atajos: Ctrl+1 … Ctrl+8 cambian de módulo · Ctrl+E exporta a Excel · Ctrl+S guarda.
    /// </summary>
    public class FrmPrincipal : Form
    {
        private class DefinicionModulo
        {
            public string Texto;
            public Func<ModuloBase> Fabrica;
            public BotonNav Boton;
            public ModuloBase Instancia;
        }

        private readonly List<DefinicionModulo> modulos = new List<DefinicionModulo>();
        private readonly Panel contenedor;
        private readonly FlowLayoutPanel navegacion;
        private readonly Label lblEstado;
        private readonly Label lblGuardado;
        private readonly ToolTip ayuda = new ToolTip();
        private readonly Repositorio repo = Repositorio.Instancia;
        private DefinicionModulo actual;

        public FrmPrincipal()
        {
            Text = "Sistema de Contabilidad de Costos por Órdenes de Producción";
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            Font = Tema.Fuente;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(Math.Min(Tema.E(1280), Screen.PrimaryScreen.WorkingArea.Width), Math.Min(Tema.E(760), Screen.PrimaryScreen.WorkingArea.Height));
            Size = new Size(1440, 880);
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;
            DoubleBuffered = true;

            // ---------------- Barra superior ----------------
            var barra = new Panel { Dock = DockStyle.Top, Height = Tema.E(64), BackColor = Tema.Superficie };
            var marca = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, Width = Tema.E(220), BackColor = Tema.Superficie, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, Padding = new Padding(Tema.E(16), Tema.E(8), 0, 0)
            };
            marca.Controls.Add(new Label
            {
                Text = "◆ CostosOP", Font = new Font("Segoe UI Semibold", 14f), ForeColor = Tema.Texto,
                AutoSize = true, Margin = new Padding(0)
            });
            marca.Controls.Add(new Label
            {
                Text = "Costos por órdenes de producción", Font = Tema.FuentePequena, ForeColor = Tema.TextoTenue,
                AutoSize = true, Margin = new Padding(Tema.E(2), 0, 0, 0)
            });

            navegacion = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, WrapContents = false, BackColor = Tema.Superficie,
                Padding = new Padding(0), Margin = new Padding(0)
            };

            // Zona derecha: Exportar a Excel + Guardar
            var zonaGuardar = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                BackColor = Tema.Superficie, Padding = new Padding(Tema.E(8), 0, Tema.E(12), 0)
            };
            var btnExcel = Tema.CrearBoton("Exportar a Excel", Tema.Verde);
            btnExcel.Width = TextRenderer.MeasureText(btnExcel.Text, Tema.FuenteNegrita).Width + Tema.E(28);
            var btnGuardar = Tema.CrearBoton("Guardar", Tema.Azul);
            btnGuardar.Width = TextRenderer.MeasureText(btnGuardar.Text, Tema.FuenteNegrita).Width + Tema.E(28);
            int margenVertical = Math.Max(0, (barra.Height - btnGuardar.Height) / 2);
            btnExcel.Margin = new Padding(0, margenVertical, Tema.E(8), 0);
            btnGuardar.Margin = new Padding(0, margenVertical, 0, 0);
            btnExcel.Click += (s, e) => FrmExportar.Mostrar(this);
            btnGuardar.Click += (s, e) => GuardarDatos(true);
            ayuda.SetToolTip(btnExcel, "Elegir qué módulos exportar a un archivo .xlsx (Ctrl+E)");
            ayuda.SetToolTip(btnGuardar, "Guardar ahora (Ctrl+S). Los cambios también se guardan automáticamente.");
            zonaGuardar.Controls.Add(btnExcel);
            zonaGuardar.Controls.Add(btnGuardar);

            Tema.Apilar(barra, marca, zonaGuardar, navegacion);

            var linea = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Tema.Borde };

            // ---------------- Barra de estado ----------------
            var estado = new Panel { Dock = DockStyle.Bottom, Height = Tema.FuentePequena.Height + Tema.E(14), BackColor = Tema.Superficie };
            lblEstado = new Label
            {
                Dock = DockStyle.Fill, ForeColor = Tema.TextoTenue, Font = Tema.FuentePequena,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Tema.E(14), 0, 0, 0),
                Text = "Listo · Ctrl+1…Ctrl+8 cambia de módulo · Ctrl+E exporta a Excel · Ctrl+S guarda"
            };
            lblGuardado = new Label
            {
                Dock = DockStyle.Right, Width = Tema.E(460), ForeColor = Tema.TextoTenue, Font = Tema.FuentePequena,
                TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, Tema.E(14), 0), AutoEllipsis = true
            };
            Tema.Apilar(estado, lblGuardado, lblEstado);

            contenedor = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo };

            Tema.Apilar(this, barra, linea, estado, contenedor);

            // ---------------- Registro de módulos (orden = atajo Ctrl+N) ----------------
            Registrar("Materia Prima", () => new UcMateriaPrima());
            Registrar("Tarjetas de Almacén", () => new UcKardex());
            Registrar("Órdenes", () => new UcOrdenes());
            Registrar("Mano de Obra", () => new UcManoObra());
            Registrar("GIF", () => new UcGastosIndirectos());
            Registrar("Art. Terminados", () => new UcArticulosTerminados());
            Registrar("Resultados", () => new UcResultados());
            Registrar("Reportes", () => new UcReportes());

            // ---------------- Datos y eventos ----------------
            ModuloBase.MensajeEstado += MostrarMensaje;
            repo.DatosCambiados += (s, e) => GuardarDatos(false);
            repo.Cargar();

            Load += (s, e) => Navegar(0);
            FormClosing += (s, e) => GuardarDatos(false);
            FormClosed += (s, e) =>
            {
                ModuloBase.MensajeEstado -= MostrarMensaje;
                foreach (var m in modulos)
                    if (m.Instancia != null && m.Instancia.Parent == null) m.Instancia.Dispose();
            };
        }

        private void Registrar(string texto, Func<ModuloBase> fabrica)
        {
            var def = new DefinicionModulo { Texto = texto, Fabrica = fabrica, Boton = new BotonNav(texto) };
            int indice = modulos.Count;
            def.Boton.Click += (s, e) => Navegar(indice);
            ayuda.SetToolTip(def.Boton, texto + "  (Ctrl+" + (indice + 1) + ")");
            modulos.Add(def);
            navegacion.Controls.Add(def.Boton);
        }

        /// <summary>Muestra el módulo indicado; se crea la primera vez y después se reutiliza.</summary>
        private void Navegar(int indice)
        {
            if (indice < 0 || indice >= modulos.Count) return;
            var def = modulos[indice];
            Cursor = Cursors.WaitCursor;
            try
            {
                if (def.Instancia == null) def.Instancia = def.Fabrica();

                if (actual != def)
                {
                    contenedor.SuspendLayout();
                    contenedor.Controls.Clear();
                    contenedor.Controls.Add(def.Instancia);
                    contenedor.ResumeLayout(true);
                    actual = def;
                }
                def.Instancia.Refrescar();

                foreach (var m in modulos) m.Boton.Activo = (m == def);
                Text = "Sistema de Contabilidad de Costos — " + def.Texto;
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al abrir el módulo: " + ex.Message, true);
                MessageBox.Show(this, "No se pudo abrir el módulo:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void GuardarDatos(bool manual)
        {
            try
            {
                repo.Guardar();
                lblGuardado.ForeColor = Tema.TextoTenue;
                lblGuardado.Text = "Guardado " + DateTime.Now.ToString("HH:mm:ss") + " · " + repo.RutaArchivo;
                if (manual) MostrarMensaje("Datos guardados correctamente.", false);
            }
            catch (Exception ex)
            {
                lblGuardado.ForeColor = Tema.Rojo;
                lblGuardado.Text = "No se pudo guardar: " + ex.Message;
            }
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            lblEstado.ForeColor = esError ? Tema.Ambar : Tema.Verde;
            lblEstado.Text = (esError ? "⚠ " : "✔ ") + mensaje;
        }

        /// <summary>Atajos de teclado globales.</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Modifiers) == Keys.Control)
            {
                Keys tecla = keyData & Keys.KeyCode;
                if (tecla >= Keys.D1 && tecla <= Keys.D8)
                {
                    Navegar(tecla - Keys.D1);
                    return true;
                }
                if (tecla == Keys.S)
                {
                    GuardarDatos(true);
                    return true;
                }
                if (tecla == Keys.E)
                {
                    FrmExportar.Mostrar(this);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
