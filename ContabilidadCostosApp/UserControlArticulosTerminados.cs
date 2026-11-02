using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class UserControlArticulosTerminados : UserControl
    {
        public UserControlArticulosTerminados()
        {
            this.BackColor = Color.FromArgb(20, 26, 35);
            this.Dock = DockStyle.Fill;

            Label lblTitulo = new Label { Text = "Inventario de Artículos Terminados", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            DataGridView dgvTerminados = new DataGridView { Location = new Point(20, 75), Size = new Size(1230, 640), BackgroundColor = Color.FromArgb(30, 41, 55), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };

            dgvTerminados.Columns.Add("Codigo", "Código");
            dgvTerminados.Columns.Add("Nombre", "Nombre de Producto");
            dgvTerminados.Columns.Add("Orden", "Orden Origen");
            dgvTerminados.Columns.Add("Cantidad", "Unidades Terminadas");
            dgvTerminados.Columns.Add("CostoU", "Costo Unitario");
            dgvTerminados.Columns.Add("CostoT", "Costo Total Transferido");

            dgvTerminados.Rows.Add("ART-001", "Producto A", "OP-001", "100", "$100.00", "$10,000.00");

            this.Controls.AddRange(new Control[] { lblTitulo, dgvTerminados });
        }
    }
}
