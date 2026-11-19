using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CostosOP.Modelos;
using CostosOP.UI;

namespace CostosOP.Modulos
{
    /// <summary>
    /// MÓDULO 4 — Órdenes de Producción: alta con folio automático, seguimiento de estado
    /// y hoja de costos con acumulación independiente de MPD, MOD y GIF.
    /// </summary>
    public class UcOrdenes : ModuloBase
    {
        private readonly TarjetaKpi kpiActivas, kpiTerminadas, kpiEnProceso, kpiTerminado;
        private readonly DataGridView gridOrdenes, gridCargos;
        private readonly Label lblCargos;
        private ComboBox cbProducto;
        private TextBox txtCliente;
        private NumericUpDown numCantidad;
        private DateTimePicker dtpFecha;
        private Label lblResumen;
        private Button btnTerminar, btnCancelar;
        private string folioSeleccionado;

        public UcOrdenes()
            : base("Órdenes de Producción", "Folios, estados y acumulación de los tres elementos del costo: MPD + MOD + GIF")
        {
            kpiActivas = AgregarKpi("Órdenes activas", Tema.Azul);
            kpiTerminadas = AgregarKpi("Órdenes terminadas", Tema.Verde);
            kpiEnProceso = AgregarKpi("Costo en proceso", Tema.Ambar);
            kpiTerminado = AgregarKpi("Costo terminado", Tema.Morado);

            var lateral = Tema.CrearPanelLateral();
            lateral.Controls.Add(ConstruirFormulario());

            gridOrdenes = Tema.CrearGrid();
            gridOrdenes.SelectionChanged += (s, e) =>
            {
                string folio = Tema.ValorCelda(gridOrdenes, "Folio");
                if (!string.IsNullOrEmpty(folio) && folio != folioSeleccionado)
                {
                    folioSeleccionado = folio;
                    MostrarDetalle();
                }
            };
            gridOrdenes.CellFormatting += FormatoEstado;

            gridCargos = Tema.CrearGrid();
            gridCargos.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || gridCargos.Columns[e.ColumnIndex].Name != "Elemento") return;
                string v = Convert.ToString(e.Value);
                e.CellStyle.ForeColor = v == "MPD" ? Tema.Azul : v == "MOD" ? Tema.Verde : Tema.Ambar;
                e.CellStyle.Font = Tema.FuenteNegrita;
            };

            var tarjetaOrdenes = Tema.CrearTarjeta("Órdenes de producción", gridOrdenes);
            var tarjetaCargos = Tema.CrearTarjeta("Hoja de costos", gridCargos, out lblCargos);
            Tema.Apilar(Cuerpo, lateral, Tema.CrearEspaciador(), Tema.DosFilas(tarjetaOrdenes, tarjetaCargos, 55f));
        }

        private PanelFormulario ConstruirFormulario()
        {
            var f = new PanelFormulario();
            f.Seccion("Nueva orden", "El folio se asigna automáticamente al guardar.");
            cbProducto = f.Campo("Producto a fabricar", Tema.CrearCombo(true));
            txtCliente = f.Campo("Cliente", Tema.CrearTexto());
            numCantidad = f.Campo("Unidades a producir", Tema.CrearNumero(0, 10000000m));
            dtpFecha = f.Campo("Fecha de alta", Tema.CrearFecha());
            var btnCrear = f.Boton("Crear orden", Tema.Azul);
            btnCrear.Click += (s, e) => Ejecutar(() =>
            {
                string folio = Repo.CrearOrden(cbProducto.Text, txtCliente.Text, (int)numCantidad.Value, dtpFecha.Value.Date);
                folioSeleccionado = folio;
                cbProducto.Text = string.Empty; txtCliente.Clear(); numCantidad.Value = 0;
                return "Orden " + folio + " creada. Ya puede recibir materia prima, nómina y GIF.";
            });

            f.Separador();
            f.Seccion("Orden seleccionada", null);
            lblResumen = new Label
            {
                AutoSize = true, MinimumSize = new Size(PanelFormulario.AnchoControl, 0),
                MaximumSize = new Size(PanelFormulario.AnchoControl, 0),
                BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, Font = new Font("Consolas", 9.5f),
                Padding = new Padding(Tema.E(10), Tema.E(8), Tema.E(8), Tema.E(8)), Margin = new Padding(0, Tema.E(4), 0, Tema.E(4))
            };
            f.Controls.Add(lblResumen);

            btnTerminar = f.Boton("Terminar y enviar a almacén PT", Tema.Verde);
            btnTerminar.Click += (s, e) => TerminarSeleccionada();
            btnCancelar = f.Boton("Cancelar orden (sin cargos)", Tema.Rojo);
            btnCancelar.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(folioSeleccionado)) return;
                if (!Confirmar("¿Cancelar la orden " + folioSeleccionado + "?")) return;
                Ejecutar(() => { Repo.CancelarOrden(folioSeleccionado); return "Orden " + folioSeleccionado + " cancelada."; });
            };
            return f;
        }

        private void TerminarSeleccionada()
        {
            if (string.IsNullOrEmpty(folioSeleccionado)) return;
            decimal pendiente = Repo.GifPendiente();
            if (pendiente > 0 && !Confirmar(string.Format(
                    "Existen GIF pendientes de prorratear por {0:C2}.\nSi termina la orden ahora, no recibirá esa parte.\n\n¿Terminar de todos modos?", pendiente)))
                return;
            string folio = folioSeleccionado;
            Ejecutar(() =>
            {
                Repo.TerminarOrden(folio, DateTime.Today);
                var o = Repo.BuscarOrden(folio);
                return string.Format("{0} terminada: CT {1:C2}, CU {2:C4}. Transferida a artículos terminados.", folio, o.CostoTotal, o.CostoUnitario);
            });
        }

        private void FormatoEstado(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0 || gridOrdenes.Columns[e.ColumnIndex].Name != "Estado") return;
            switch (Convert.ToString(e.Value))
            {
                case "Abierta": e.CellStyle.ForeColor = Tema.TextoTenue; break;
                case "En proceso": e.CellStyle.ForeColor = Tema.Ambar; break;
                case "Terminada": e.CellStyle.ForeColor = Tema.Verde; break;
                default: e.CellStyle.ForeColor = Tema.Rojo; break;
            }
            e.CellStyle.Font = Tema.FuenteNegrita;
        }

        public override void Refrescar()
        {
            var ordenes = Repo.Datos.Ordenes.OrderByDescending(o => o.Folio).ToList();
            var activas = ordenes.Where(o => o.EstaActiva).ToList();
            var terminadas = ordenes.Where(o => o.Estado == EstadoOrden.Terminada).ToList();

            kpiActivas.Valor = activas.Count.ToString("N0");
            kpiActivas.Detalle = activas.Count(o => o.Estado == EstadoOrden.EnProceso) + " en proceso";
            kpiTerminadas.Valor = terminadas.Count.ToString("N0");
            kpiTerminadas.Detalle = terminadas.Sum(o => o.Cantidad).ToString("N0") + " unidades";
            kpiEnProceso.Valor = activas.Sum(o => o.CostoTotal).ToString("C2");
            kpiEnProceso.Detalle = "Inventario de producción en proceso";
            kpiTerminado.Valor = terminadas.Sum(o => o.CostoTotal).ToString("C2");
            kpiTerminado.Detalle = "Transferido a almacén PT";

            // Autocompletado de productos ya fabricados (evita recapturar).
            string texto = cbProducto.Text;
            cbProducto.Items.Clear();
            foreach (var p in Repo.Datos.Ordenes.Select(o => o.Producto).Distinct().OrderBy(p => p)) cbProducto.Items.Add(p);
            cbProducto.Text = texto;

            string conservar = folioSeleccionado;
            gridOrdenes.DataSource = ordenes.Select(o => new
            {
                Folio = o.Folio,
                Producto = o.Producto,
                Cliente = o.Cliente,
                Unidades = o.Cantidad,
                Estado = Textos.Estado(o.Estado),
                MPD = o.MPD,
                MOD = o.MOD,
                GIF = o.GIF,
                CostoTotal = o.CostoTotal,
                CostoUnitario = o.CostoUnitario
            }).ToList();

            folioSeleccionado = conservar;
            if (Repo.BuscarOrden(folioSeleccionado) == null)
                folioSeleccionado = ordenes.Count > 0 ? ordenes[0].Folio : null;
            Tema.SeleccionarFila(gridOrdenes, "Folio", folioSeleccionado);
            MostrarDetalle();
        }

        private void MostrarDetalle()
        {
            var o = Repo.BuscarOrden(folioSeleccionado);
            if (o == null)
            {
                lblResumen.Text = "Sin orden seleccionada.";
                lblCargos.Text = "Hoja de costos";
                gridCargos.DataSource = null;
                btnTerminar.Enabled = btnCancelar.Enabled = false;
                return;
            }

            lblResumen.Text = string.Format(
                "{0}  [{1}]\n{2}\nUnidades: {3:N0}\n\nMPD  {4,16:C2}\nMOD  {5,16:C2}\nGIF  {6,16:C2}\nCT   {7,16:C2}\nCU   {8,16:C4}",
                o.Folio, Textos.Estado(o.Estado), Recortar(o.Producto, 34), o.Cantidad,
                o.MPD, o.MOD, o.GIF, o.CostoTotal, o.CostoUnitario);
            btnTerminar.Enabled = o.EstaActiva;
            btnCancelar.Enabled = o.EstaActiva;

            lblCargos.Text = "Hoja de costos — " + o.Folio + " · " + o.Producto;
            gridCargos.DataSource = Repo.CargosDeOrden(o.Folio).Select(c => new
            {
                Fecha = c.Fecha,
                Elemento = c.Elemento.ToString(),
                Concepto = c.Concepto,
                Referencia = c.Referencia,
                Cantidad = c.Cantidad,
                Importe = c.Importe
            }).ToList();
        }

        private static string Recortar(string s, int max)
        {
            return string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }
    }
}
