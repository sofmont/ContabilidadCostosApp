using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CostosOP.Datos;

namespace CostosOP.UI
{
    /// <summary>
    /// Ventana para elegir qué módulos se exportan a Excel.
    /// Cada módulo marcado genera una o varias hojas en el mismo libro .xlsx.
    /// La selección se recuerda mientras el programa esté abierto.
    /// </summary>
    public class FrmExportar : Form
    {
        private static HashSet<SeccionExport> ultimaSeleccion;
        private static bool ultimoAbrir = true;

        private readonly Dictionary<SeccionExport, CheckBox> casillas = new Dictionary<SeccionExport, CheckBox>();
        private readonly CheckBox chkAbrir;
        private readonly Button btnExportar;
        private readonly Label lblResumen;

        /// <summary>Muestra la ventana. Si se indica una sección, se abre solo con ella marcada.</summary>
        public static void Mostrar(IWin32Window propietario, SeccionExport? soloEsta = null)
        {
            using (var f = new FrmExportar(soloEsta)) f.ShowDialog(propietario);
        }

        private FrmExportar(SeccionExport? soloEsta)
        {
            Text = "Exportar a Excel";
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            Font = Tema.Fuente;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(Tema.E(22), Tema.E(18), Tema.E(22), Tema.E(18));

            var raiz = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Tema.Fondo, Margin = new Padding(0)
            };
            int ancho = Tema.E(440);

            raiz.Controls.Add(new Label
            {
                Text = "¿Qué quieres incluir en el archivo de Excel?", Font = Tema.FuenteSeccion,
                ForeColor = Tema.Texto, AutoSize = true, Margin = new Padding(0, 0, 0, Tema.E(4))
            });
            raiz.Controls.Add(new Label
            {
                Text = "Marca uno o varios módulos. Todos se guardan en un solo archivo .xlsx, cada uno en sus propias hojas.",
                Font = Tema.FuentePequena, ForeColor = Tema.TextoTenue, AutoSize = true,
                MaximumSize = new Size(ancho, 0), Margin = new Padding(0, 0, 0, Tema.E(10))
            });

            // Atajos de selección
            var atajos = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Tema.Fondo, Margin = new Padding(0, 0, 0, Tema.E(8)) };
            var btnTodo = Tema.CrearBotonSegmento("Seleccionar todo", Tema.E(150));
            var btnNada = Tema.CrearBotonSegmento("Ninguno", Tema.E(100));
            btnTodo.Click += (s, e) => MarcarTodo(true);
            btnNada.Click += (s, e) => MarcarTodo(false);
            atajos.Controls.Add(btnTodo);
            atajos.Controls.Add(btnNada);
            raiz.Controls.Add(atajos);

            // Una casilla por módulo, con descripción y número de hojas
            var lista = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
                BackColor = Tema.Superficie, Padding = new Padding(Tema.E(14), Tema.E(10), Tema.E(14), Tema.E(10)),
                Margin = new Padding(0, 0, 0, Tema.E(10)), MinimumSize = new Size(ancho, 0)
            };
            foreach (SeccionExport s in Enum.GetValues(typeof(SeccionExport)))
            {
                var chk = new CheckBox
                {
                    Text = GeneradorTablas.Nombre(s), Font = Tema.FuenteNegrita, ForeColor = Tema.Texto,
                    AutoSize = true, Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(0, Tema.E(6), 0, 0),
                    Checked = soloEsta.HasValue ? s == soloEsta.Value
                                                : (ultimaSeleccion == null || ultimaSeleccion.Contains(s))
                };
                chk.FlatAppearance.BorderColor = Tema.Borde;
                chk.FlatAppearance.CheckedBackColor = Tema.Azul;
                chk.CheckedChanged += (o, e) => ActualizarResumen();
                casillas[s] = chk;
                lista.Controls.Add(chk);
                lista.Controls.Add(new Label
                {
                    Text = GeneradorTablas.Descripcion(s), Font = Tema.FuentePequena, ForeColor = Tema.TextoTenue,
                    AutoSize = true, Margin = new Padding(Tema.E(20), 0, 0, Tema.E(4))
                });
            }
            raiz.Controls.Add(lista);

            chkAbrir = new CheckBox
            {
                Text = "Abrir el archivo al terminar", ForeColor = Tema.TextoTenue, AutoSize = true,
                FlatStyle = FlatStyle.Flat, Checked = ultimoAbrir, Margin = new Padding(0, 0, 0, Tema.E(6))
            };
            chkAbrir.FlatAppearance.BorderColor = Tema.Borde;
            raiz.Controls.Add(chkAbrir);

            lblResumen = new Label
            {
                AutoSize = true, ForeColor = Tema.Verde, Font = Tema.FuentePequena,
                MaximumSize = new Size(ancho, 0), Margin = new Padding(0, 0, 0, Tema.E(10))
            };
            raiz.Controls.Add(lblResumen);

            // Botones
            var botones = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft, AutoSize = true, WrapContents = false,
                BackColor = Tema.Fondo, MinimumSize = new Size(ancho, 0), Margin = new Padding(0)
            };
            btnExportar = Tema.CrearBoton("Exportar a Excel", Tema.Verde);
            btnExportar.Width = Tema.E(170);
            btnExportar.Margin = new Padding(Tema.E(8), 0, 0, 0);
            btnExportar.Click += (s, e) => Exportar();
            var btnCancelar = Tema.CrearBoton("Cancelar", Tema.Tarjeta);
            btnCancelar.Width = Tema.E(110);
            btnCancelar.Margin = new Padding(0);
            btnCancelar.DialogResult = DialogResult.Cancel;
            botones.Controls.Add(btnExportar);
            botones.Controls.Add(btnCancelar);
            raiz.Controls.Add(botones);

            Controls.Add(raiz);
            AcceptButton = btnExportar;
            CancelButton = btnCancelar;
            ActualizarResumen();
        }

        private List<SeccionExport> Seleccionadas()
        {
            return casillas.Where(kv => kv.Value.Checked).Select(kv => kv.Key).ToList();
        }

        private void MarcarTodo(bool valor)
        {
            foreach (var c in casillas.Values) c.Checked = valor;
        }

        private void ActualizarResumen()
        {
            var sel = Seleccionadas();
            int hojas = sel.Sum(s => s == SeccionExport.TarjetasAlmacen
                ? Math.Max(1, Repositorio.Instancia.Datos.Materiales.Count)
                : GeneradorTablas.Hojas(s).Count);
            btnExportar.Enabled = sel.Count > 0;
            lblResumen.ForeColor = sel.Count > 0 ? Tema.Verde : Tema.Ambar;
            lblResumen.Text = sel.Count == 0
                ? "Selecciona al menos un módulo."
                : string.Format("{0} módulo(s) seleccionado(s) · {1} hoja(s) en el archivo", sel.Count, hojas);
        }

        private void Exportar()
        {
            var sel = Seleccionadas();
            if (sel.Count == 0) return;

            string sugerido = sel.Count == 1
                ? GeneradorTablas.Nombre(sel[0]).Replace(" ", "_").Replace("(", "").Replace(")", "")
                : "Costos_OP";
            using (var dlg = new SaveFileDialog
            {
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = sugerido + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx",
                OverwritePrompt = true
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                Cursor = Cursors.WaitCursor;
                try
                {
                    var hojas = new List<DataTable>();
                    foreach (var s in sel) hojas.AddRange(GeneradorTablas.Hojas(s));
                    ExportadorExcel.Guardar(dlg.FileName, hojas);

                    ultimaSeleccion = new HashSet<SeccionExport>(sel);
                    ultimoAbrir = chkAbrir.Checked;
                    ModuloBase.Informar(string.Format("Excel generado ({0} hojas): {1}", hojas.Count, dlg.FileName), false);

                    if (chkAbrir.Checked) AbrirArchivo(dlg.FileName);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (IOException ex)
                {
                    MessageBox.Show(this, "No se pudo escribir el archivo. Si está abierto en Excel, ciérralo e inténtalo de nuevo.\n\n" + ex.Message,
                        "Exportar a Excel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "No se pudo exportar:\n\n" + ex.Message, "Exportar a Excel", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private void AbrirArchivo(string ruta)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
            }
            catch (Exception)
            {
                ModuloBase.Informar("Archivo guardado en " + ruta + " (no se pudo abrir automáticamente).", false);
            }
        }
    }
}
