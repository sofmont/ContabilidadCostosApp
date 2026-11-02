using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class UserControlTarjetasAlmacen : UserControl
    {
        public UserControlTarjetasAlmacen()
        {
            this.BackColor = Color.FromArgb(20, 26, 35);
            this.Dock = DockStyle.Fill;

            Label lblTitulo = new Label { Text = "Tarjetas de Almacén (Valuación de Inventario Kardex)", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            Label lblFiltro = new Label { Text = "Seleccionar Materia Prima:", Location = new Point(20, 65), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true };
            ComboBox cbMateriales = new ComboBox { Location = new Point(20, 90), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White };
            cbMateriales.Items.AddRange(new string[] { "MP-001 | Lámina de Acero", "MP-002 | Madera Pino" });
            cbMateriales.SelectedIndex = 0;

            DataGridView dgvKardex = new DataGridView { Location = new Point(20, 135), Size = new Size(1230, 580), BackgroundColor = Color.FromArgb(30, 41, 55), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };

            // Columnas del Kardex según Pizarrón / Documento
            dgvKardex.Columns.Add("Fecha", "Fecha");
            dgvKardex.Columns.Add("Concepto", "Concepto / OP");
            dgvKardex.Columns.Add("Entrada", "Entrada (Cant)");
            dgvKardex.Columns.Add("Salida", "Salida (Cant)");
            dgvKardex.Columns.Add("Existencia", "Existencia");
            dgvKardex.Columns.Add("CostoDeEntrada", "Costo Entrada");
            dgvKardex.Columns.Add("CostoDeSalida", "Costo Salida");
            dgvKardex.Columns.Add("SaldoMonetario", "Saldo Monetario");

            dgvKardex.Rows.Add("01/11/2026", "Inventario Inicial", "100", "0", "100", "$50.00", "$0.00", "$5,000.00");
            dgvKardex.Rows.Add("02/11/2026", "Compra Factura #402", "50", "0", "150", "$50.00", "$0.00", "$7,500.00");
            dgvKardex.Rows.Add("03/11/2026", "Requisición OP-001", "0", "30", "120", "$0.00", "$50.00", "$6,000.00");
            dgvKardex.Rows.Add("04/11/2026", "Requisición OP-002", "0", "20", "100", "$0.00", "$50.00", "$5,000.00");

            this.Controls.AddRange(new Control[] { lblTitulo, lblFiltro, cbMateriales, dgvKardex });
        }
    }
}
