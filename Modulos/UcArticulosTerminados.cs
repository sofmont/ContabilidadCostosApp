using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CostosOP.Modelos;
using CostosOP.UI;

namespace CostosOP.Modulos
{
    /// <summary>
    /// MÓDULO 3 — Artículos Terminados: recibe las órdenes terminadas (transferencia del costo total)
    /// y controla sus existencias y salidas por venta a costo promedio.
    /// </summary>
    public class UcArticulosTerminados : ModuloBase
    {
        private readonly TarjetaKpi kpiProductos, kpiUnidades, kpiValor, kpiVendido;
        private readonly DataGridView gridInventario, gridMovimientos;
        private ComboBox cbOrden, cbProducto;
        private Label lblOrden, lblDisponible;
        private NumericUpDown numCantidad;
        private TextBox txtReferencia;
        private DateTimePicker dtpTermino, dtpVenta;

        public UcArticulosTerminados()
            : base("Artículos Terminados", "Transferencia del costo de órdenes terminadas y control del almacén de producto terminado")
        {
            kpiProductos = AgregarKpi("Productos en almacén", Tema.Azul);
            kpiUnidades = AgregarKpi("Unidades disponibles", Tema.Morado);
            kpiValor = AgregarKpi("Valor inventario PT", Tema.Verde);
            kpiVendido = AgregarKpi("Costo de lo vendido", Tema.Ambar);

            var lateral = Tema.CrearPanelLateral();
            lateral.Controls.Add(ConstruirFormulario());

            gridInventario = Tema.CrearGrid();
            gridInventario.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                Tema.SeleccionarValor(cbProducto, Convert.ToString(gridInventario.Rows[e.RowIndex].Cells["Producto"].Value));
            };
            gridMovimientos = Tema.CrearGrid();
            Tema.OcultarCerosMovimientos(gridMovimientos);
            var arriba = Tema.CrearTarjeta("Existencias de artículos terminados", gridInventario);
            var abajo = Tema.CrearTarjeta("Tarjeta de almacén de artículos terminados", gridMovimientos);
            Tema.Apilar(Cuerpo, lateral, Tema.CrearEspaciador(), Tema.DosFilas(arriba, abajo, 42f));
        }

        private PanelFormulario ConstruirFormulario()
        {
            var f = new PanelFormulario();
            f.Seccion("Recibir orden terminada", "Cierra la orden y transfiere su costo total (MPD + MOD + GIF) al almacén.");
            cbOrden = f.Campo("Orden en proceso", Tema.CrearCombo());
            lblOrden = f.Indicador("—", Tema.Texto);
            dtpTermino = f.Campo("Fecha de terminación", Tema.CrearFecha());
            cbOrden.SelectedIndexChanged += (s, e) => MostrarOrden();
            var btnTerminar = f.Boton("Terminar y transferir costo", Tema.Verde);
            btnTerminar.Click += (s, e) =>
            {
                string folio = Tema.ValorCombo(cbOrden);
                if (folio == null) { Informar("No hay órdenes en proceso.", true); return; }
                decimal pendiente = Repo.GifPendiente();
                if (pendiente > 0 && !Confirmar(string.Format(
                        "Hay {0:C2} de GIF sin prorratear. ¿Terminar la orden {1} de todos modos?", pendiente, folio)))
                    return;
                Ejecutar(() =>
                {
                    Repo.TerminarOrden(folio, dtpTermino.Value.Date);
                    var o = Repo.BuscarOrden(folio);
                    return string.Format("{0} recibida en almacén: {1:N0} u. a CU {2:C4}.", folio, o.Cantidad, o.CostoUnitario);
                });
            };

            f.Separador();
            f.Seccion("Salida por venta", "Descarga unidades del almacén valuadas a costo promedio.");
            cbProducto = f.Campo("Artículo", Tema.CrearCombo());
            lblDisponible = f.Indicador("Disponible: —", Tema.Texto);
            numCantidad = f.Campo("Unidades vendidas", Tema.CrearNumero(0));
            txtReferencia = f.Campo("Factura / referencia", Tema.CrearTexto());
            dtpVenta = f.Campo("Fecha", Tema.CrearFecha());
            cbProducto.SelectedIndexChanged += (s, e) => MostrarDisponible();
            numCantidad.ValueChanged += (s, e) => MostrarDisponible();
            var btnVender = f.Boton("Registrar salida", Tema.Azul);
            btnVender.Click += (s, e) => Ejecutar(() =>
            {
                decimal costo = Repo.VenderProductoTerminado(Tema.ValorCombo(cbProducto), numCantidad.Value, txtReferencia.Text, dtpVenta.Value.Date);
                numCantidad.Value = 0; txtReferencia.Clear();
                return string.Format("Salida registrada. Costo de lo vendido: {0:C2}.", costo);
            });
            return f;
        }

        private void MostrarOrden()
        {
            var o = Repo.BuscarOrden(Tema.ValorCombo(cbOrden));
            if (o == null) { lblOrden.Text = "No hay órdenes en proceso."; lblOrden.ForeColor = Tema.TextoTenue; return; }
            lblOrden.Text = string.Format("{0} · {1:N0} u.\nCT {2:C2}   CU {3:C4}", o.Producto, o.Cantidad, o.CostoTotal, o.CostoUnitario);
            lblOrden.ForeColor = o.MPD > 0 ? Tema.Texto : Tema.Ambar;
        }

        private void MostrarDisponible()
        {
            var p = Repo.BuscarProducto(Tema.ValorCombo(cbProducto));
            if (p == null) { lblDisponible.Text = "Disponible: —"; return; }
            bool excede = numCantidad.Value > p.Existencia;
            lblDisponible.Text = excede
                ? string.Format("Excede la existencia ({0:N0} u.)", p.Existencia)
                : string.Format("Disponible: {0:N0} u. · CP {1:C4}\nCosto de la salida: {2:C2}", p.Existencia, p.CostoPromedio, numCantidad.Value * p.CostoPromedio);
            lblDisponible.ForeColor = excede ? Tema.Rojo : Tema.Texto;
        }

        public override void Refrescar()
        {
            var productos = Repo.Datos.Productos.OrderBy(p => p.Producto).ToList();
            var movs = Repo.Datos.MovimientosPT;

            kpiProductos.Valor = productos.Count(p => p.Existencia > 0).ToString("N0");
            kpiProductos.Detalle = productos.Count + " artículos registrados";
            kpiUnidades.Valor = productos.Sum(p => p.Existencia).ToString("N0");
            kpiUnidades.Detalle = "En almacén de PT";
            kpiValor.Valor = productos.Sum(p => p.SaldoImporte).ToString("C2");
            kpiValor.Detalle = "Valuado a costo promedio";
            kpiVendido.Valor = movs.Sum(m => m.SalidaImporte).ToString("C2");
            kpiVendido.Detalle = movs.Sum(m => m.SalidaCantidad).ToString("N0") + " unidades vendidas";

            Tema.LlenarCombo(cbOrden, Repo.OrdenesActivas().Where(o => o.CostoTotal > 0)
                .Select(o => new ItemCombo(o.Folio, o.Folio + " · " + o.Producto)));
            Tema.LlenarCombo(cbProducto, productos.Where(p => p.Existencia > 0)
                .Select(p => new ItemCombo(p.Producto, p.Producto)));

            gridInventario.DataSource = productos.Select(p => new
            {
                Producto = p.Producto,
                Existencia = p.Existencia,
                CostoPromedio = p.CostoPromedio,
                SaldoImporte = p.SaldoImporte
            }).ToList();

            gridMovimientos.DataSource = movs.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).Select(m => new
            {
                Fecha = m.Fecha,
                Producto = m.Producto,
                Concepto = m.Concepto,
                Referencia = m.Referencia,
                EntradaCantidad = m.EntradaCantidad,
                EntradaImporte = m.EntradaImporte,
                SalidaCantidad = m.SalidaCantidad,
                SalidaImporte = m.SalidaImporte,
                SaldoCantidad = m.SaldoCantidad,
                SaldoImporte = m.SaldoImporte
            }).ToList();

            MostrarOrden();
            MostrarDisponible();
        }
    }
}
