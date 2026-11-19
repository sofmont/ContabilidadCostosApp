using System;
using System.Linq;
using System.Windows.Forms;
using CostosOP.Modelos;
using CostosOP.UI;

namespace CostosOP.Modulos
{
    /// <summary>
    /// MÓDULO 7 — Resultados y Costos: tablero que arroja el costo total y unitario de cada orden.
    /// CT = MPD + MOD + GIF      CU = CT ÷ unidades
    /// </summary>
    public class UcResultados : ModuloBase
    {
        private readonly TarjetaKpi kpiTotal, kpiMPD, kpiMOD, kpiGIF;
        private readonly GraficaCostos grafica;
        private readonly DataGridView grid;
        private readonly ComboBox cbFiltro;

        public UcResultados()
            : base("Resultados y Costos", "Costo total y unitario por orden · CT = MPD + MOD + GIF · CU = CT ÷ unidades")
        {
            kpiTotal = AgregarKpi("Costo total de producción", Tema.Azul);
            kpiMPD = AgregarKpi("Materia prima directa", Tema.Azul);
            kpiMOD = AgregarKpi("Mano de obra directa", Tema.Verde);
            kpiGIF = AgregarKpi("Gastos indirectos", Tema.Ambar);

            grafica = new GraficaCostos();
            var tarjetaGrafica = Tema.CrearTarjeta("Composición del costo por orden", grafica);
            tarjetaGrafica.Dock = DockStyle.Top;
            tarjetaGrafica.Height = Tema.E(270);

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Tema.Superficie,
                Padding = new Padding(Tema.E(14), Tema.E(8), Tema.E(14), Tema.E(8)), WrapContents = true
            };
            barra.Controls.Add(new Label
            {
                Text = "Mostrar:", AutoSize = true, ForeColor = Tema.TextoTenue,
                Font = Tema.FuenteNegrita, Margin = new Padding(0, Tema.E(6), Tema.E(10), 0)
            });
            cbFiltro = Tema.CrearCombo();
            cbFiltro.Width = Tema.E(240);
            cbFiltro.Items.Add(new ItemCombo("T", "Todas (excepto canceladas)"));
            cbFiltro.Items.Add(new ItemCombo("A", "En proceso / abiertas"));
            cbFiltro.Items.Add(new ItemCombo("F", "Terminadas"));
            cbFiltro.SelectedIndex = 0;
            cbFiltro.SelectedIndexChanged += (s, e) => Refrescar();
            barra.Controls.Add(cbFiltro);

            grid = Tema.CrearGrid();
            grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
                string col = grid.Columns[e.ColumnIndex].Name;
                if (col == "CostoTotal" || col == "CostoUnitario") { e.CellStyle.Font = Tema.FuenteNegrita; e.CellStyle.ForeColor = Tema.Verde; }
            };
            var tarjetaGrid = Tema.CrearTarjeta("Hoja de resultados por orden", grid);

            Tema.Apilar(Cuerpo, tarjetaGrafica, Tema.CrearEspaciador(DockStyle.Top, 12), barra, tarjetaGrid);
        }

        public override void Refrescar()
        {
            string filtro = Tema.ValorCombo(cbFiltro) ?? "T";
            var ordenes = Repo.Datos.Ordenes
                .Where(o => o.Estado != EstadoOrden.Cancelada)
                .Where(o => filtro == "T" || (filtro == "A" ? o.EstaActiva : o.Estado == EstadoOrden.Terminada))
                .OrderBy(o => o.Folio).ToList();

            decimal mpd = ordenes.Sum(o => o.MPD), mod = ordenes.Sum(o => o.MOD), gif = ordenes.Sum(o => o.GIF);
            decimal total = mpd + mod + gif;
            kpiTotal.Valor = total.ToString("C2");
            kpiTotal.Detalle = ordenes.Count + " órdenes · " + ordenes.Sum(o => o.Cantidad).ToString("N0") + " unidades";
            kpiMPD.Valor = mpd.ToString("C2");
            kpiMPD.Detalle = Porcentaje(mpd, total) + " del costo total";
            kpiMOD.Valor = mod.ToString("C2");
            kpiMOD.Detalle = Porcentaje(mod, total) + " del costo total";
            kpiGIF.Valor = gif.ToString("C2");
            kpiGIF.Detalle = Porcentaje(gif, total) + " del costo total";

            grafica.Establecer(ordenes.Skip(Math.Max(0, ordenes.Count - 12)).Select(o => new GraficaCostos.Serie
            {
                Etiqueta = o.Folio, MPD = o.MPD, MOD = o.MOD, GIF = o.GIF
            }));

            grid.DataSource = ordenes.Select(o => new
            {
                Folio = o.Folio,
                Producto = o.Producto,
                Estado = Textos.Estado(o.Estado),
                Unidades = o.Cantidad,
                MPD = o.MPD,
                MOD = o.MOD,
                GIF = o.GIF,
                CostoTotal = o.CostoTotal,
                CostoUnitario = o.CostoUnitario
            }).ToList();
        }

        private static string Porcentaje(decimal parte, decimal total)
        {
            return total > 0 ? (parte / total).ToString("P1") : "0 %";
        }
    }
}
