using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CostosOP.Datos;
using CostosOP.Modelos;
using CostosOP.UI;

namespace CostosOP.Modulos
{
    /// <summary>
    /// MÓDULO 8 — Reportes: centro de auditoría y reportes generales, exportables a Excel (.xlsx).
    /// </summary>
    public class UcReportes : ModuloBase
    {
        private static readonly string[] NombresReporte =
        {
            "Estado de costo de producción y ventas",
            "Hoja de costos por orden",
            "Concentrado de órdenes",
            "Kardex consolidado de materia prima",
            "Movimientos de artículos terminados",
            "Historial de prorrateos de GIF",
            "Bitácora de auditoría"
        };

        private readonly TarjetaKpi kpiOrdenes, kpiMovimientos, kpiBitacora, kpiArchivo;
        private readonly List<Button> botones = new List<Button>();
        private readonly DataGridView grid;
        private readonly Label lblTitulo;
        private ComboBox cbFolio;
        private int reporteActual;
        private DataTable tabla;

        public UcReportes()
            : base("Reportes y Auditoría", "Estados de costos, hojas de costos, kardex y bitácora · Exportación a Excel (.xlsx)")
        {
            kpiOrdenes = AgregarKpi("Órdenes registradas", Tema.Azul);
            kpiMovimientos = AgregarKpi("Movimientos de almacén", Tema.Verde);
            kpiBitacora = AgregarKpi("Eventos auditados", Tema.Morado);
            kpiArchivo = AgregarKpi("Respaldo", Tema.Ambar);

            var lateral = Tema.CrearPanelLateral();
            lateral.Controls.Add(ConstruirFormulario());

            grid = Tema.CrearGrid();
            grid.DataBindingComplete += (s, e) =>
            {
                if (grid.Columns.Contains("Resaltar")) grid.Columns["Resaltar"].Visible = false;
                if (grid.Columns.Contains("Concepto")) grid.Columns["Concepto"].FillWeight = 260;
                if (grid.Columns.Contains("Detalle")) grid.Columns["Detalle"].FillWeight = 300;
            };
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || tabla == null || !tabla.Columns.Contains("Resaltar")) return;
                var fila = grid.Rows[e.RowIndex].DataBoundItem as DataRowView;
                if (fila != null && fila["Resaltar"] is bool && (bool)fila["Resaltar"])
                {
                    e.CellStyle.Font = Tema.FuenteNegrita;
                    e.CellStyle.ForeColor = Tema.Azul;
                }
            };
            var tarjeta = Tema.CrearTarjeta("Reporte", grid, out lblTitulo);
            Tema.Apilar(Cuerpo, lateral, Tema.CrearEspaciador(), tarjeta);
        }

        private PanelFormulario ConstruirFormulario()
        {
            var f = new PanelFormulario();
            f.Seccion("Tipo de reporte", "El reporte se genera al seleccionarlo.");
            for (int i = 0; i < NombresReporte.Length; i++)
            {
                int indice = i;
                var b = Tema.CrearBotonSegmento(NombresReporte[i], PanelFormulario.AnchoControl);
                b.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                b.Padding = new Padding(Tema.E(8), 0, 0, 0);
                b.Margin = new Padding(0, 0, 0, Tema.E(4));
                b.Click += (s, e) => { reporteActual = indice; Generar(); };
                botones.Add(b);
                f.Controls.Add(b);
            }
            cbFolio = f.Campo("Orden para la hoja de costos", Tema.CrearCombo());
            cbFolio.SelectedIndexChanged += (s, e) => { if (reporteActual == 1) Generar(); };

            f.Separador();
            f.Seccion("Exportar a Excel (.xlsx)", null);
            var btnExportar = f.Boton("Exportar este reporte", Tema.Verde);
            btnExportar.Click += (s, e) => ExportarActual();
            var btnModulos = f.Boton("Elegir módulos a exportar…", Tema.Azul);
            btnModulos.Click += (s, e) => FrmExportar.Mostrar(this);
            f.Nota("Puedes incluir Materia Prima, Tarjetas de Almacén, Órdenes, Mano de Obra, GIF, Artículos Terminados, Resultados y Reportes en un solo archivo, o solo los que marques.");

            f.Separador();
            f.Seccion("Datos del sistema", "Se guardan automáticamente en: " + Repo.RutaArchivo);
            var btnDemo = f.Boton("Cargar datos de demostración", Tema.Tarjeta);
            btnDemo.Click += (s, e) =>
            {
                if (!Confirmar("Se reemplazarán TODOS los datos actuales por datos de ejemplo. ¿Continuar?")) return;
                Ejecutar(() => { Repo.CargarDemo(); return "Datos de demostración cargados."; });
            };
            var btnBlanco = f.Boton("Iniciar sistema en blanco", Tema.Rojo);
            btnBlanco.Click += (s, e) =>
            {
                if (!Confirmar("Se eliminarán TODOS los datos (materiales, órdenes, costos y bitácora). ¿Continuar?")) return;
                Ejecutar(() => { Repo.IniciarEnBlanco(); return "Sistema reiniciado sin datos."; });
            };
            return f;
        }

        public override void Refrescar()
        {
            kpiOrdenes.Valor = Repo.Datos.Ordenes.Count.ToString("N0");
            kpiOrdenes.Detalle = Repo.Datos.Ordenes.Count(o => o.Estado == EstadoOrden.Terminada) + " terminadas";
            kpiMovimientos.Valor = (Repo.Datos.MovimientosMP.Count + Repo.Datos.MovimientosPT.Count).ToString("N0");
            kpiMovimientos.Detalle = "MP y artículos terminados";
            kpiBitacora.Valor = Repo.Datos.Bitacora.Count.ToString("N0");
            kpiBitacora.Detalle = "Operaciones registradas";
            bool existe = File.Exists(Repo.RutaArchivo);
            kpiArchivo.Valor = existe ? "Guardado" : "Pendiente";
            kpiArchivo.Detalle = existe ? File.GetLastWriteTime(Repo.RutaArchivo).ToString("dd/MM/yyyy HH:mm") : "Aún no se ha guardado";

            Tema.LlenarCombo(cbFolio, Repo.Datos.Ordenes.OrderByDescending(o => o.Folio)
                .Select(o => new ItemCombo(o.Folio, o.Folio + " · " + o.Producto)));
            Generar();
        }

        // ------------------------------------------------------------------ Generación

        private void Generar()
        {
            for (int i = 0; i < botones.Count; i++) Tema.MarcarSegmento(botones[i], i == reporteActual);
            switch (reporteActual)
            {
                case 0: tabla = GeneradorTablas.EstadoDeCosto(); break;
                case 1: tabla = GeneradorTablas.HojaDeCostos(Tema.ValorCombo(cbFolio)); break;
                case 2: tabla = GeneradorTablas.ConcentradoOrdenes(); break;
                case 3: tabla = GeneradorTablas.KardexConsolidado(); break;
                case 4: tabla = GeneradorTablas.MovimientosPT(); break;
                case 5: tabla = GeneradorTablas.Prorrateos(); break;
                default: tabla = GeneradorTablas.Bitacora(); break;
            }
            lblTitulo.Text = tabla.TableName;
            grid.DataSource = tabla;
        }

        // ------------------------------------------------------------------ Exportación

        /// <summary>Exporta a Excel únicamente el reporte que se está viendo.</summary>
        private void ExportarActual()
        {
            if (tabla == null || tabla.Rows.Count == 0)
            {
                MessageBox.Show(this, "El reporte no tiene datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog
            {
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = NombresReporte[reporteActual].Replace(' ', '_') + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    ExportadorExcel.Guardar(dlg.FileName, new List<DataTable> { tabla });
                    Informar("Reporte exportado a Excel: " + dlg.FileName, false);
                    if (Confirmar("Reporte exportado correctamente.\n¿Deseas abrirlo ahora?"))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
                catch (IOException ex)
                {
                    MessageBox.Show(this, "No se pudo escribir el archivo. Si está abierto en Excel, ciérralo.\n\n" + ex.Message,
                        "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "No se pudo exportar:\n" + ex.Message, "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
