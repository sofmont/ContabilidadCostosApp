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
    /// MÓDULO 2 — Tarjetas de Almacén (Kardex): control físico y monetario por material,
    /// con entradas, salidas y saldos a costo promedio ponderado.
    /// </summary>
    public class UcKardex : ModuloBase
    {
        private const string Todos = "*";
        private readonly TarjetaKpi kpiExistencia, kpiPromedio, kpiSaldo, kpiMovimientos;
        private readonly ComboBox cbMaterial;
        private readonly DataGridView grid;
        private readonly Label lblTitulo;

        public UcKardex()
            : base("Tarjetas de Almacén — Kardex", "Control físico y monetario de existencias · Método: costo promedio ponderado")
        {
            kpiExistencia = AgregarKpi("Existencia", Tema.Azul);
            kpiPromedio = AgregarKpi("Costo promedio", Tema.Morado);
            kpiSaldo = AgregarKpi("Saldo valuado", Tema.Verde);
            kpiMovimientos = AgregarKpi("Movimientos", Tema.Ambar);

            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Tema.Superficie,
                Padding = new Padding(Tema.E(14), Tema.E(10), Tema.E(14), Tema.E(10)), WrapContents = true
            };
            barra.Controls.Add(new Label
            {
                Text = "Tarjeta de:", AutoSize = true, ForeColor = Tema.TextoTenue,
                Font = Tema.FuenteNegrita, Margin = new Padding(0, Tema.E(6), Tema.E(10), 0)
            });
            cbMaterial = Tema.CrearCombo();
            cbMaterial.Width = Tema.E(360);
            cbMaterial.SelectedIndexChanged += (s, e) => MostrarKardex();
            barra.Controls.Add(cbMaterial);
            barra.Controls.Add(new Label
            {
                Text = "Las entradas provienen del inventario inicial y compras; las salidas, de los envíos a órdenes.",
                AutoSize = true, ForeColor = Tema.TextoTenue, Font = Tema.FuentePequena, Margin = new Padding(Tema.E(16), Tema.E(8), 0, 0)
            });

            grid = Tema.CrearGrid();
            Tema.OcultarCerosMovimientos(grid);
            grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
                string col = grid.Columns[e.ColumnIndex].Name;
                if (col.StartsWith("Entrada")) e.CellStyle.ForeColor = Tema.Verde;
                else if (col.StartsWith("Salida")) e.CellStyle.ForeColor = Color.FromArgb(0xF8, 0x71, 0x71);
                else if (col.StartsWith("Saldo")) e.CellStyle.Font = Tema.FuenteNegrita;
            };
            var tarjeta = Tema.CrearTarjeta("Movimientos", grid, out lblTitulo);

            Tema.Apilar(Cuerpo, barra, Tema.CrearEspaciador(DockStyle.Top, 12), tarjeta);
        }

        public override void Refrescar()
        {
            var items = new List<ItemCombo> { new ItemCombo(Todos, "(Todos los materiales)") };
            items.AddRange(Repo.Datos.Materiales.OrderBy(m => m.Codigo)
                .Select(m => new ItemCombo(m.Codigo, m.Codigo + " · " + m.Nombre)));
            Tema.LlenarCombo(cbMaterial, items);
            MostrarKardex();
        }

        private void MostrarKardex()
        {
            string codigo = Tema.ValorCombo(cbMaterial) ?? Todos;
            var nombres = Repo.Datos.Materiales.ToDictionary(m => m.Codigo, m => m.Nombre);
            var movs = Repo.Datos.MovimientosMP
                .Where(m => codigo == Todos || m.CodigoMaterial == codigo)
                .OrderBy(m => m.Fecha).ThenBy(m => m.Id).ToList();

            grid.DataSource = movs.Select(m => new
            {
                Fecha = m.Fecha,
                Material = codigo == Todos
                    ? m.CodigoMaterial + " " + (nombres.ContainsKey(m.CodigoMaterial) ? nombres[m.CodigoMaterial] : string.Empty)
                    : m.CodigoMaterial,
                Concepto = Textos.Movimiento(m.Tipo),
                Referencia = m.Referencia,
                EntradaCantidad = m.EntradaCantidad,
                EntradaCU = m.EntradaCostoUnitario,
                EntradaImporte = m.EntradaImporte,
                SalidaCantidad = m.SalidaCantidad,
                SalidaCU = m.SalidaCostoUnitario,
                SalidaImporte = m.SalidaImporte,
                SaldoCantidad = m.SaldoCantidad,
                CostoPromedio = m.CostoPromedio,
                SaldoImporte = m.SaldoImporte
            }).ToList();

            kpiMovimientos.Valor = movs.Count.ToString("N0");
            kpiMovimientos.Detalle = movs.Count(m => m.Tipo == TipoMovimiento.SalidaProduccion) + " salidas a producción";

            var mat = Repo.BuscarMaterial(codigo == Todos ? null : codigo);
            if (mat == null)
            {
                lblTitulo.Text = "Movimientos de todos los materiales";
                kpiExistencia.Valor = Repo.Datos.Materiales.Count.ToString("N0") + " mat.";
                kpiExistencia.Detalle = "Seleccione un material para ver su tarjeta";
                kpiExistencia.Acento = Tema.Azul;
                kpiPromedio.Valor = "—";
                kpiPromedio.Detalle = "Aplica por material";
                kpiSaldo.Valor = Repo.Datos.Materiales.Sum(m => m.SaldoImporte).ToString("C2");
                kpiSaldo.Detalle = "Inventario total de MP";
            }
            else
            {
                lblTitulo.Text = "Tarjeta de almacén: " + mat.Codigo + " · " + mat.Nombre + " (" + mat.Unidad + ")";
                kpiExistencia.Valor = mat.Existencia.ToString("N2") + " " + mat.Unidad;
                kpiExistencia.Detalle = "Mínimo: " + mat.StockMinimo.ToString("N2");
                kpiExistencia.Acento = mat.BajoMinimo ? Tema.Ambar : Tema.Azul;
                kpiPromedio.Valor = mat.CostoPromedio.ToString("C4");
                kpiPromedio.Detalle = "Por " + mat.Unidad;
                kpiSaldo.Valor = mat.SaldoImporte.ToString("C2");
                kpiSaldo.Detalle = "Existencia × costo promedio";
            }
        }
    }
}
