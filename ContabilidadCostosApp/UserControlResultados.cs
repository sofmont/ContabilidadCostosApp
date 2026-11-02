using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class UserControlResultados : UserControl
    {
        public UserControlResultados()
        {
            this.BackColor = Color.FromArgb(20, 26, 35);
            this.Dock = DockStyle.Fill;

            Label lblTitulo = new Label { Text = "Resultados: Costos Totales y Unitarios por Orden", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            Panel cardTotal = CrearCard("Costo Total Producido", "$10,000.00", 20, 65, Color.FromArgb(0, 122, 255));
            Panel cardUnitario = CrearCard("Costo Unitario (OP-001)", "$100.00", 250, 65, Color.FromArgb(16, 185, 129));
            Panel cardUnidades = CrearCard("Unidades Fabricadas", "100 Unidades", 480, 65, Color.FromArgb(245, 158, 11));

            DataGridView dgvResumen = new DataGridView { Location = new Point(20, 165), Size = new Size(1230, 550), BackgroundColor = Color.FromArgb(30, 41, 55), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };

            dgvResumen.Columns.Add("Orden", "N° Orden");
            dgvResumen.Columns.Add("Producto", "Producto");
            dgvResumen.Columns.Add("MPD", "MP Directa (50%)");
            dgvResumen.Columns.Add("MOD", "MO Directa (30%)");
            dgvResumen.Columns.Add("GIF", "GIF Asignado (20%)");
            dgvResumen.Columns.Add("Total", "Costo Total (CT)");
            dgvResumen.Columns.Add("Unitario", "Costo Unitario (CU)");

            dgvResumen.Rows.Add("OP-001", "Producto A", "$5,000.00", "$3,000.00", "$2,000.00", "$10,000.00", "$100.00");

            this.Controls.AddRange(new Control[] { lblTitulo, cardTotal, cardUnitario, cardUnidades, dgvResumen });
        }

        private Panel CrearCard(string t, string v, int x, int y, Color c)
        {
            Panel p = new Panel { Location = new Point(x, y), Size = new Size(210, 80), BackColor = Color.FromArgb(30, 41, 55) };
            Panel b = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = c };
            Label l1 = new Label { Text = t, Location = new Point(15, 12), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true };
            Label l2 = new Label { Text = v, Location = new Point(15, 35), ForeColor = Color.White, Font = new Font("Segoe UI", 13F, FontStyle.Bold), AutoSize = true };
            p.Controls.AddRange(new Control[] { b, l1, l2 });
            return p;
        }
    }
}