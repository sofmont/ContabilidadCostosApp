using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CostosOP.Modelos;
using CostosOP.UI;

namespace CostosOP.Modulos
{
    /// <summary>
    /// MÓDULO 1 — Materia Prima: alta de material con inventario inicial,
    /// entradas por compra y salidas a producción (cargadas como MPD a la orden).
    /// </summary>
    public class UcMateriaPrima : ModuloBase
    {
        private readonly TarjetaKpi kpiMateriales, kpiValor, kpiBajoMinimo, kpiConsumido;
        private readonly DataGridView grid;
        private readonly Panel host;
        private readonly PanelFormulario frmAlta, frmCompra, frmSalida;
        private readonly List<Button> segmentos = new List<Button>();

        // Alta
        private TextBox txtCodigo, txtNombre;
        private ComboBox cbUnidad;
        private NumericUpDown numMinimo, numCantIni, numCostoIni;
        private DateTimePicker dtpAlta;
        // Compra
        private ComboBox cbMatCompra;
        private NumericUpDown numCantCompra, numCostoCompra;
        private TextBox txtFactura;
        private DateTimePicker dtpCompra;
        private Label lblImporteCompra;
        // Salida
        private ComboBox cbMatSalida, cbOrdenSalida;
        private NumericUpDown numCantSalida;
        private DateTimePicker dtpSalida;
        private Label lblDisponible, lblImporteSalida;

        public UcMateriaPrima()
            : base("Materia Prima", "Captura de materiales, inventario inicial, compras y salidas a producción")
        {
            kpiMateriales = AgregarKpi("Materiales registrados", Tema.Azul);
            kpiValor = AgregarKpi("Valor del inventario MP", Tema.Verde);
            kpiBajoMinimo = AgregarKpi("Bajo stock mínimo", Tema.Ambar);
            kpiConsumido = AgregarKpi("MP enviada a producción", Tema.Morado);

            frmAlta = ConstruirAlta();
            frmCompra = ConstruirCompra();
            frmSalida = ConstruirSalida();

            // Panel lateral con selector segmentado de operación
            var lateral = Tema.CrearPanelLateral();
            var barraSeg = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Tema.Superficie,
                Padding = new Padding(Tema.E(18), Tema.E(12), Tema.E(10), Tema.E(4)), WrapContents = false
            };
            string[] nombres = { "Alta", "Compra", "Salida a OP" };
            for (int i = 0; i < nombres.Length; i++)
            {
                int indice = i;
                var b = Tema.CrearBotonSegmento(nombres[i], Tema.E(92));
                b.Click += (s, e) => MostrarOperacion(indice);
                segmentos.Add(b);
                barraSeg.Controls.Add(b);
            }
            host = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Superficie };
            Tema.Apilar(lateral, barraSeg, host);

            grid = Tema.CrearGrid();
            grid.CellClick += Grid_CellClick;
            grid.CellFormatting += Grid_CellFormatting;
            var tarjeta = Tema.CrearTarjeta("Catálogo y existencias de materia prima  (clic en un renglón para usarlo)", grid);

            Tema.Apilar(Cuerpo, lateral, Tema.CrearEspaciador(), tarjeta);
            MostrarOperacion(0);
        }

        // ------------------------------------------------------------------ Construcción

        private PanelFormulario ConstruirAlta()
        {
            var f = new PanelFormulario();
            f.Seccion("Alta de material", "Registra el material en el catálogo y, si existe, su inventario inicial (abre la tarjeta de almacén).");
            txtCodigo = f.Campo("Código", Tema.CrearTexto());
            txtNombre = f.Campo("Nombre o descripción", Tema.CrearTexto());
            cbUnidad = f.Campo("Unidad de medida", Tema.CrearCombo(true));
            cbUnidad.Items.AddRange(new object[] { "pza", "kg", "g", "lt", "m", "m²", "caja", "cono", "rollo" });
            cbUnidad.SelectedIndex = 0;
            numMinimo = f.Campo("Stock mínimo", Tema.CrearNumero(2));
            f.Seccion("Inventario inicial (opcional)", null);
            numCantIni = f.Campo("Cantidad inicial", Tema.CrearNumero(2));
            numCostoIni = f.Campo("Costo unitario inicial", Tema.CrearNumero(4));
            dtpAlta = f.Campo("Fecha", Tema.CrearFecha());
            var btn = f.Boton("Guardar material", Tema.Azul);
            btn.Click += (s, e) => Ejecutar(() =>
            {
                var m = Repo.AltaMaterial(txtCodigo.Text, txtNombre.Text, cbUnidad.Text,
                    numMinimo.Value, numCantIni.Value, numCostoIni.Value, dtpAlta.Value.Date);
                txtCodigo.Text = Repo.SugerirCodigoMaterial();
                txtNombre.Clear();
                numMinimo.Value = 0; numCantIni.Value = 0; numCostoIni.Value = 0;
                txtNombre.Focus();
                return "Material " + m.Codigo + " registrado.";
            });
            return f;
        }

        private PanelFormulario ConstruirCompra()
        {
            var f = new PanelFormulario();
            f.Seccion("Entrada por compra", "Cada compra se suma a la tarjeta de almacén y recalcula el costo promedio.");
            cbMatCompra = f.Campo("Material", Tema.CrearCombo());
            numCantCompra = f.Campo("Cantidad comprada", Tema.CrearNumero(2));
            numCostoCompra = f.Campo("Costo unitario de compra", Tema.CrearNumero(4));
            txtFactura = f.Campo("Factura / referencia", Tema.CrearTexto());
            dtpCompra = f.Campo("Fecha", Tema.CrearFecha());
            lblImporteCompra = f.Indicador("Importe: $0.00", Tema.Verde);
            numCantCompra.ValueChanged += (s, e) => ActualizarImporteCompra();
            numCostoCompra.ValueChanged += (s, e) => ActualizarImporteCompra();
            cbMatCompra.SelectedIndexChanged += (s, e) =>
            {
                // Sugiere el último costo conocido para agilizar la captura.
                var m = Repo.BuscarMaterial(Tema.ValorCombo(cbMatCompra));
                if (m != null && m.CostoPromedio > 0 && numCostoCompra.Value == 0) numCostoCompra.Value = m.CostoPromedio;
            };
            var btn = f.Boton("Registrar compra", Tema.Verde);
            btn.Click += (s, e) => Ejecutar(() =>
            {
                string codigo = Tema.ValorCombo(cbMatCompra);
                Repo.RegistrarCompra(codigo, numCantCompra.Value, numCostoCompra.Value, txtFactura.Text, dtpCompra.Value.Date);
                numCantCompra.Value = 0; txtFactura.Clear();
                return "Compra de " + codigo + " registrada en la tarjeta de almacén.";
            });
            return f;
        }

        private PanelFormulario ConstruirSalida()
        {
            var f = new PanelFormulario();
            f.Seccion("Salida a producción", "Envía material a una orden. Se valúa a costo promedio y se carga como MPD a la orden.");
            cbMatSalida = f.Campo("Material", Tema.CrearCombo());
            lblDisponible = f.Indicador("Disponible: —", Tema.Texto);
            cbOrdenSalida = f.Campo("Orden de producción", Tema.CrearCombo());
            numCantSalida = f.Campo("Cantidad a enviar", Tema.CrearNumero(2));
            dtpSalida = f.Campo("Fecha", Tema.CrearFecha());
            lblImporteSalida = f.Indicador("Importe a cargar (MPD): $0.00", Tema.Azul);
            cbMatSalida.SelectedIndexChanged += (s, e) => ActualizarDisponible();
            numCantSalida.ValueChanged += (s, e) => ActualizarDisponible();
            var btn = f.Boton("Enviar a producción", Tema.Azul);
            btn.Click += (s, e) => Ejecutar(() =>
            {
                string folio = Tema.ValorCombo(cbOrdenSalida);
                decimal importe = Repo.SalidaAProduccion(Tema.ValorCombo(cbMatSalida), folio, numCantSalida.Value, dtpSalida.Value.Date);
                numCantSalida.Value = 0;
                return string.Format("Salida aplicada a {0}: {1:C2} de MPD.", folio, importe);
            });
            return f;
        }

        // ------------------------------------------------------------------ Comportamiento

        private void MostrarOperacion(int indice)
        {
            PanelFormulario[] formularios = { frmAlta, frmCompra, frmSalida };
            host.Controls.Clear();
            host.Controls.Add(formularios[indice]);
            for (int i = 0; i < segmentos.Count; i++) Tema.MarcarSegmento(segmentos[i], i == indice);
        }

        private void ActualizarImporteCompra()
        {
            lblImporteCompra.Text = "Importe: " + (numCantCompra.Value * numCostoCompra.Value).ToString("C2");
        }

        private void ActualizarDisponible()
        {
            var m = Repo.BuscarMaterial(Tema.ValorCombo(cbMatSalida));
            if (m == null)
            {
                lblDisponible.Text = "Disponible: —";
                lblImporteSalida.Text = "Importe a cargar (MPD): $0.00";
                return;
            }
            lblDisponible.Text = string.Format("Disponible: {0:N2} {1}  ·  CP {2:C4}", m.Existencia, m.Unidad, m.CostoPromedio);
            bool excede = numCantSalida.Value > m.Existencia;
            lblDisponible.ForeColor = excede ? Tema.Rojo : (m.BajoMinimo ? Tema.Ambar : Tema.Texto);
            lblImporteSalida.Text = excede
                ? "La cantidad excede la existencia"
                : "Importe a cargar (MPD): " + (numCantSalida.Value * m.CostoPromedio).ToString("C2");
            lblImporteSalida.ForeColor = excede ? Tema.Rojo : Tema.Azul;
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string codigo = Convert.ToString(grid.Rows[e.RowIndex].Cells["Código"].Value);
            Tema.SeleccionarValor(cbMatCompra, codigo);
            Tema.SeleccionarValor(cbMatSalida, codigo);
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name == "Estado")
                e.CellStyle.ForeColor = Convert.ToString(e.Value) == "Bajo mínimo" ? Tema.Ambar : Tema.Verde;
        }

        public override void Refrescar()
        {
            var mats = Repo.Datos.Materiales.OrderBy(m => m.Codigo).ToList();
            int bajos = mats.Count(m => m.BajoMinimo);

            kpiMateriales.Valor = mats.Count.ToString("N0");
            kpiMateriales.Detalle = "En catálogo";
            kpiValor.Valor = mats.Sum(m => m.SaldoImporte).ToString("C2");
            kpiValor.Detalle = "Valuado a costo promedio";
            kpiBajoMinimo.Valor = bajos.ToString("N0");
            kpiBajoMinimo.Detalle = bajos > 0 ? "Requieren reabastecimiento" : "Existencias suficientes";
            kpiConsumido.Valor = Repo.Datos.MovimientosMP
                .Where(x => x.Tipo == TipoMovimiento.SalidaProduccion).Sum(x => x.SalidaImporte).ToString("C2");
            kpiConsumido.Detalle = "Cargado como MPD";

            grid.DataSource = mats.Select(m => new
            {
                Código = m.Codigo,
                Nombre = m.Nombre,
                Unidad = m.Unidad,
                Existencia = m.Existencia,
                StockMínimo = m.StockMinimo,
                CostoPromedio = m.CostoPromedio,
                SaldoImporte = m.SaldoImporte,
                Estado = m.BajoMinimo ? "Bajo mínimo" : "Suficiente"
            }).ToList();

            var items = mats.Select(m => new ItemCombo(m.Codigo, m.Codigo + " · " + m.Nombre)).ToList();
            Tema.LlenarCombo(cbMatCompra, items);
            Tema.LlenarCombo(cbMatSalida, items);
            Tema.LlenarCombo(cbOrdenSalida, Repo.OrdenesActivas()
                .Select(o => new ItemCombo(o.Folio, o.Folio + " · " + o.Producto)));

            if (string.IsNullOrWhiteSpace(txtCodigo.Text)) txtCodigo.Text = Repo.SugerirCodigoMaterial();
            ActualizarImporteCompra();
            ActualizarDisponible();
        }
    }
}
