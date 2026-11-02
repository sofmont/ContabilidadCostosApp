using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class UserControlManoObra : UserControl
    {
        private TextBox txtOperario;
        private TextBox txtHoras;
        private TextBox txtCostoHora;
        private ComboBox cbOrdenes;
        private DataGridView dgvMOD;

        public UserControlManoObra()
        {
            this.BackColor = Color.FromArgb(20, 26, 35);
            this.Dock = DockStyle.Fill;

            Label lblTitulo = new Label { Text = "Registro de Mano de Obra Directa (MOD)", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            Label lblOrden = CrearEtiqueta("Seleccionar Orden:", 20, 70);
            cbOrdenes = new ComboBox { Location = new Point(20, 95), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White };
            cbOrdenes.Items.AddRange(new string[] { "OP-001 (Cliente Samis)", "OP-002 (Annanti Asnase)" });
            cbOrdenes.SelectedIndex = 0;

            Label lblOperario = CrearEtiqueta("Nombre del Operario:", 20, 140);
            txtOperario = CrearTextBox(20, 165, 250);

            Label lblHoras = CrearEtiqueta("Horas Trabajadas:", 20, 210);
            txtHoras = CrearTextBox(20, 235, 250);

            Label lblCostoHora = CrearEtiqueta("Costo por Hora ($):", 20, 280);
            txtCostoHora = CrearTextBox(20, 305, 250);

            Button btnRegistrar = new Button { Text = "Registrar Nómina OP", Location = new Point(20, 360), Width = 250, Height = 40, BackColor = Color.FromArgb(0, 122, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRegistrar.FlatAppearance.BorderSize = 0;
            btnRegistrar.Click += BtnRegistrar_Click;

            dgvMOD = new DataGridView { Location = new Point(300, 70), Size = new Size(930, 630), BackgroundColor = Color.FromArgb(30, 41, 55), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvMOD.Columns.Add("Orden", "N° Orden");
            dgvMOD.Columns.Add("Operario", "Operario");
            dgvMOD.Columns.Add("Horas", "Horas Trab.");
            dgvMOD.Columns.Add("Tarifa", "Costo x Hora");
            dgvMOD.Columns.Add("Total", "Importe Total MOD");

            dgvMOD.Rows.Add("OP-001", "Juan Pérez", "10", "$100.00", "$1,000.00");
            dgvMOD.Rows.Add("OP-002", "María López", "15", "$100.00", "$1,500.00");

            this.Controls.AddRange(new Control[] { lblTitulo, lblOrden, cbOrdenes, lblOperario, txtOperario, lblHoras, txtHoras, lblCostoHora, txtCostoHora, btnRegistrar, dgvMOD });
        }

        private void BtnRegistrar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOperario.Text) || string.IsNullOrWhiteSpace(txtHoras.Text) || string.IsNullOrWhiteSpace(txtCostoHora.Text))
            {
                MessageBox.Show("Complete todos los campos de mano de obra.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (decimal.TryParse(txtHoras.Text, out decimal horas) && decimal.TryParse(txtCostoHora.Text, out decimal tarifa))
            {
                decimal total = horas * tarifa;
                dgvMOD.Rows.Add(cbOrdenes.SelectedItem.ToString().Split(' ')[0], txtOperario.Text.Trim(), horas.ToString(), $"${tarifa:N2}", $"${total:N2}");
                txtOperario.Clear(); txtHoras.Clear(); txtCostoHora.Clear();
                MessageBox.Show("Mano de obra acumulada exitosamente en la orden.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private Label CrearEtiqueta(string t, int x, int y) => new Label { Text = t, Location = new Point(x, y), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true };
        private TextBox CrearTextBox(int x, int y, int w) => new TextBox { Location = new Point(x, y), Width = w, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White };
    }
}
