using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class UserControlOrdenesProduccion : UserControl
    {
        private DataGridView dgvOrdenes;

        public UserControlOrdenesProduccion()
        {
            this.BackColor = Color.FromArgb(20, 26, 35);
            this.Dock = DockStyle.Fill;

            Label lblTitulo = new Label { Text = "Administración de Órdenes de Producción (O.P.)", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            Button btnNuevaOrden = new Button { Text = "➕ Crear Nueva Orden", Location = new Point(20, 65), Width = 200, Height = 38, BackColor = Color.FromArgb(0, 122, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnNuevaOrden.FlatAppearance.BorderSize = 0;
            btnNuevaOrden.Click += (s, e) => MessageBox.Show("Formulario de alta de Orden aperturado.", "Órdenes", MessageBoxButtons.OK, MessageBoxIcon.Information);

            dgvOrdenes = new DataGridView { Location = new Point(20, 115), Size = new Size(1230, 600), BackgroundColor = Color.FromArgb(30, 41, 55), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };

            dgvOrdenes.Columns.Add("OP", "N° Orden");
            dgvOrdenes.Columns.Add("Fecha", "Inicio");
            dgvOrdenes.Columns.Add("Producto", "Producto");
            dgvOrdenes.Columns.Add("Solicitado", "Cant. Solic.");
            dgvOrdenes.Columns.Add("Estado", "Estado");
            dgvOrdenes.Columns.Add("MPD", "MPD ($)");
            dgvOrdenes.Columns.Add("MOD", "MOD ($)");
            dgvOrdenes.Columns.Add("GIF", "GIF ($)");
            dgvOrdenes.Columns.Add("CostoTotal", "Costo Total");
            dgvOrdenes.Columns.Add("CostoUnitario", "Costo Unit.");

            // Ejemplo de la Orden 001 del documento
            dgvOrdenes.Rows.Add("OP-001", "01/11/2026", "Producto A", "100", "Terminada", "$5,000.00", "$3,000.00", "$2,000.00", "$10,000.00", "$100.00");
            dgvOrdenes.Rows.Add("OP-002", "03/11/2026", "Producto B", "50", "En proceso", "$2,500.00", "$1,500.00", "$0.00", "$4,000.00", "$0.00");

            this.Controls.AddRange(new Control[] { lblTitulo, btnNuevaOrden, dgvOrdenes });
        }
    }
}
