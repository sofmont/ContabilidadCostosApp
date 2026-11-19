using System;
using System.Linq;
using System.Windows.Forms;
using CostosOP.Datos;
using CostosOP.Modelos;
using CostosOP.UI;

namespace CostosOP.Modulos
{
    /// <summary>
    /// MÓDULO 6 — Gastos Indirectos de Fabricación: captura de gastos reales y motor de prorrateo.
    /// Tasa de aplicación = Σ GIF pendientes ÷ Σ base (horas MOD, costo MOD o costo MPD) de órdenes activas.
    /// Cada resultado se envía a su orden como cargo de GIF.
    /// </summary>
    public class UcGastosIndirectos : ModuloBase
    {
        private readonly TarjetaKpi kpiRegistrados, kpiPendientes, kpiAplicados, kpiTasa;
        private readonly DataGridView gridGastos, gridPrevio;
        private readonly Label lblPrevio;
        private TextBox txtConcepto;
        private ComboBox cbCategoria, cbBase;
        private NumericUpDown numImporte;
        private DateTimePicker dtpFecha;
        private Label lblTasa;

        public UcGastosIndirectos()
            : base("Gastos Indirectos de Fabricación — GIF", "Registro de gastos y prorrateo automático a las órdenes mediante tasa de aplicación")
        {
            kpiRegistrados = AgregarKpi("GIF registrados", Tema.Azul);
            kpiPendientes = AgregarKpi("Pendientes de prorratear", Tema.Ambar);
            kpiAplicados = AgregarKpi("GIF aplicados a órdenes", Tema.Verde);
            kpiTasa = AgregarKpi("Tasa calculada", Tema.Morado);

            var lateral = Tema.CrearPanelLateral();
            lateral.Controls.Add(ConstruirFormulario());

            gridGastos = Tema.CrearGrid();
            gridGastos.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || gridGastos.Columns[e.ColumnIndex].Name != "Estado") return;
                e.CellStyle.ForeColor = Convert.ToString(e.Value) == "Pendiente" ? Tema.Ambar : Tema.Verde;
            };
            gridPrevio = Tema.CrearGrid();
            var arriba = Tema.CrearTarjeta("Gastos indirectos reales", gridGastos);
            var abajo = Tema.CrearTarjeta("Vista previa del prorrateo", gridPrevio, out lblPrevio);
            Tema.Apilar(Cuerpo, lateral, Tema.CrearEspaciador(), Tema.DosFilas(arriba, abajo, 55f));
        }

        private PanelFormulario ConstruirFormulario()
        {
            var f = new PanelFormulario();
            f.Seccion("1. Agregar gasto indirecto", "Captura cada gasto indirecto en cuanto exista (MPI, sueldos de supervisores, luz, etc.).");
            txtConcepto = f.Campo("Concepto", Tema.CrearTexto());
            cbCategoria = f.Campo("Categoría", Tema.CrearCombo());
            cbCategoria.Items.AddRange(new object[]
            {
                "Materia prima indirecta", "Mano de obra indirecta", "Sueldos de supervisores",
                "Energía eléctrica (luz)", "Agua y gas", "Depreciación", "Mantenimiento",
                "Renta de nave industrial", "Seguros", "Otros"
            });
            cbCategoria.SelectedIndex = 0;
            numImporte = f.Campo("Importe", Tema.CrearNumero(2));
            dtpFecha = f.Campo("Fecha", Tema.CrearFecha());
            var btnAgregar = f.Boton("Agregar gasto", Tema.Azul);
            btnAgregar.Click += (s, e) => Ejecutar(() =>
            {
                Repo.RegistrarGasto(txtConcepto.Text, cbCategoria.Text, numImporte.Value, dtpFecha.Value.Date);
                string msg = string.Format("Gasto «{0}» por {1:C2} agregado.", txtConcepto.Text.Trim(), numImporte.Value);
                txtConcepto.Clear(); numImporte.Value = 0; txtConcepto.Focus();
                return msg;
            });
            var btnEliminar = f.Boton("Eliminar gasto seleccionado", Tema.Tarjeta);
            btnEliminar.Click += (s, e) =>
            {
                string no = Tema.ValorCelda(gridGastos, "No");
                int id;
                if (!int.TryParse(no, out id)) return;
                if (!Confirmar("¿Eliminar el gasto seleccionado?")) return;
                Ejecutar(() => { Repo.EliminarGasto(id); return "Gasto eliminado."; });
            };

            f.Separador();
            f.Seccion("2. Prorratear", "Totaliza los GIF pendientes, los divide entre la base y envía cada resultado a su orden.");
            cbBase = f.Campo("Base de aplicación", Tema.CrearCombo());
            foreach (BaseProrrateo b in Enum.GetValues(typeof(BaseProrrateo)))
                cbBase.Items.Add(new ItemCombo(b.ToString(), Textos.Base(b)));
            cbBase.SelectedIndex = 0;
            cbBase.SelectedIndexChanged += (s, e) => Previsualizar();
            lblTasa = f.Indicador("Tasa: —", Tema.Morado);
            var btnAplicar = f.Boton("Aplicar prorrateo a órdenes", Tema.Verde);
            btnAplicar.Click += (s, e) =>
            {
                var previo = Repo.CalcularProrrateo(BaseSeleccionada());
                if (previo.TotalGIF <= 0 || previo.Detalle.Count == 0)
                {
                    Ejecutar(() => { Repo.AplicarProrrateo(BaseSeleccionada(), DateTime.Today); return null; });
                    return;
                }
                if (!Confirmar(string.Format("Se aplicarán {0:C2} de GIF a {1} órdenes con tasa {2:N6}.\n¿Continuar?",
                        previo.TotalGIF, previo.Detalle.Count, previo.Tasa))) return;
                Ejecutar(() =>
                {
                    var r = Repo.AplicarProrrateo(BaseSeleccionada(), DateTime.Today);
                    return string.Format("Prorrateo aplicado: {0:C2} distribuidos en {1} órdenes (tasa {2:N6}).", r.TotalGIF, r.Detalle.Count, r.Tasa);
                });
            };
            return f;
        }

        private BaseProrrateo BaseSeleccionada()
        {
            string v = Tema.ValorCombo(cbBase);
            return v == null ? BaseProrrateo.HorasMOD : (BaseProrrateo)Enum.Parse(typeof(BaseProrrateo), v);
        }

        private void Previsualizar()
        {
            var b = BaseSeleccionada();
            ResultadoProrrateo r = Repo.CalcularProrrateo(b);
            string unidad = b == BaseProrrateo.HorasMOD ? "por hora de MOD" : "por cada $1 de base";
            lblTasa.Text = r.TotalBase > 0
                ? string.Format("Tasa = {0:C2} ÷ {1:N2}\n= {2:N6} {3}", r.TotalGIF, r.TotalBase, r.Tasa, unidad)
                : "Tasa: sin órdenes activas con base > 0";
            kpiTasa.Valor = r.TotalBase > 0 ? r.Tasa.ToString("N4") : "—";
            kpiTasa.Detalle = b == BaseProrrateo.HorasMOD ? "$ por hora de MOD" : "$ por peso de base";

            lblPrevio.Text = "Vista previa del prorrateo — " + Textos.Base(b) +
                (r.TotalGIF > 0 ? string.Format("  ·  GIF a distribuir: {0:C2}", r.TotalGIF) : "  ·  sin GIF pendientes");
            gridPrevio.DataSource = r.Detalle.Select(d => new
            {
                Folio = d.Folio,
                Producto = d.Producto,
                ValorBase = d.ValorBase,
                Tasa = r.Tasa,
                ImporteGIF = d.Importe
            }).ToList();
        }

        public override void Refrescar()
        {
            var gastos = Repo.Datos.Gastos;
            decimal pendiente = Repo.GifPendiente();
            kpiRegistrados.Valor = gastos.Sum(g => g.Importe).ToString("C2");
            kpiRegistrados.Detalle = gastos.Count + " gastos reales";
            kpiPendientes.Valor = pendiente.ToString("C2");
            kpiPendientes.Detalle = gastos.Count(g => !g.Prorrateado) + " sin aplicar";
            kpiAplicados.Valor = Repo.Datos.Ordenes.Where(o => o.Estado != EstadoOrden.Cancelada).Sum(o => o.GIF).ToString("C2");
            kpiAplicados.Detalle = Repo.Datos.Prorrateos.Count + " lotes de prorrateo";

            gridGastos.DataSource = gastos.OrderByDescending(g => g.Fecha).ThenByDescending(g => g.Id).Select(g => new
            {
                No = g.Id,
                Fecha = g.Fecha,
                Concepto = g.Concepto,
                Categoría = g.Categoria,
                Importe = g.Importe,
                Estado = g.Prorrateado ? "Prorrateado (lote " + g.Lote + ")" : "Pendiente"
            }).ToList();

            Previsualizar();
        }
    }
}
