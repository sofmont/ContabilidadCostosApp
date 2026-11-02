using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class UserControlReportes : UserControl
    {
        public UserControlReportes()
        {
            this.BackColor = Color.FromArgb(20, 26, 35);
            this.Dock = DockStyle.Fill;

            Label lblTitulo = new Label { Text = "Centro de Reportes y Auditoría de Costos", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            ComboBox cbReportes = new ComboBox { Location = new Point(20, 75), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White };
            cbReportes.Items.AddRange(new string[] {
                "1. Reporte de Costo Total por Orden",
                "2. Reporte de Costo Unitario por Producto",
                "3. Reporte de Entradas y Salidas de Almacén",
                "4. Reporte de Prorrateo de GIF",
                "5. Reporte de Producción en Proceso"
            });
            cbReportes.SelectedIndex = 0;

            Button btnGenerar = new Button { Text = "📊 Generar Reporte", Location = new Point(385, 74), Width = 160, Height = 30, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnGenerar.FlatAppearance.BorderSize = 0;

            DataGridView dgvReporte = new DataGridView { Location = new Point(20, 125), Size = new Size(1215, 570), BackgroundColor = Color.FromArgb(30, 41, 55), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvReporte.Columns.Add("Col1", "Número de Orden / SKU");
            dgvReporte.Columns.Add("Col2", "Descripción / Producto");
            dgvReporte.Columns.Add("Col3", "Materia Prima (MPD)");
            dgvReporte.Columns.Add("Col4", "Mano de Obra (MOD)");
            dgvReporte.Columns.Add("Col5", "Gastos Ind. (GIF)");
            dgvReporte.Columns.Add("Col6", "Costo Total (CT)");

            dgvReporte.Rows.Add("OP-001", "Lote Producto A (100 u.)", "$5,000.00", "$3,000.00", "$2,000.00", "$10,000.00");
            dgvReporte.Rows.Add("OP-002", "Lote Producto B (50 u.)", "$2,500.00", "$1,500.00", "$1,000.00", "$5,000.00");

            this.Controls.AddRange(new Control[] { lblTitulo, cbReportes, btnGenerar, dgvReporte });
        }
    }
}
