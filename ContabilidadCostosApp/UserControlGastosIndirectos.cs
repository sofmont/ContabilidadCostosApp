using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class UserControlGastosIndirectos : UserControl
    {
        private TextBox txtConcepto, txtImporte;
        private ComboBox cbBaseProrrateo;
        private DataGridView dgvGIF;

        public UserControlGastosIndirectos()
        {
            this.BackColor = Color.FromArgb(20, 26, 35);
            this.Dock = DockStyle.Fill;

            Label lblTitulo = new Label { Text = "Gastos Indirectos de Fabricación (GIF) y Prorrateo", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            Label lblConcepto = CrearEtiqueta("Concepto de Gasto:", 20, 65);
            txtConcepto = CrearTextBox(20, 90, 250);

            Label lblImporte = CrearEtiqueta("Importe Total ($):", 20, 130);
            txtImporte = CrearTextBox(20, 155, 250);

            Label lblBase = CrearEtiqueta("Base de Prorrateo:", 20, 195);
            cbBaseProrrateo = new ComboBox { Location = new Point(20, 220), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White };
            cbBaseProrrateo.Items.AddRange(new string[] { "Horas de Mano de Obra", "Costo de Mano de Obra", "Horas Máquina", "Costo de Materia Prima" });
            cbBaseProrrateo.SelectedIndex = 0;

            Button btnRegistrar = new Button { Text = "Registrar Gasto Indirecto", Location = new Point(20, 270), Width = 250, Height = 40, BackColor = Color.FromArgb(0, 122, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRegistrar.FlatAppearance.BorderSize = 0;
            btnRegistrar.Click += BtnRegistrar_Click;

            Button btnProrratear = new Button { Text = "⚡ Ejecutar Prorrateo de GIF", Location = new Point(20, 325), Width = 250, Height = 45, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnProrratear.FlatAppearance.BorderSize = 0;
            btnProrratear.Click += BtnProrratear_Click;

            dgvGIF = new DataGridView { Location = new Point(300, 65), Size = new Size(950, 650), BackgroundColor = Color.FromArgb(30, 41, 55), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };
            dgvGIF.Columns.Add("Folio", "Folio");
            dgvGIF.Columns.Add("Concepto", "Concepto");
            dgvGIF.Columns.Add("Importe", "Importe");
            dgvGIF.Columns.Add("Base", "Base de Reparto");

            // Gastos de prueba según documento
            dgvGIF.Rows.Add("GIF-01", "Electricidad de Planta", "$3,000.00", "Horas de Mano de Obra");
            dgvGIF.Rows.Add("GIF-02", "Sueldo de Supervisor", "$4,000.00", "Horas de Mano de Obra");
            dgvGIF.Rows.Add("GIF-03", "Mantenimiento Maquinaria", "$2,000.00", "Horas de Mano de Obra");
            dgvGIF.Rows.Add("GIF-04", "Materia Prima Indirecta", "$1,000.00", "Horas de Mano de Obra");

            this.Controls.AddRange(new Control[] { lblTitulo, lblConcepto, txtConcepto, lblImporte, txtImporte, lblBase, cbBaseProrrateo, btnRegistrar, btnProrratear, dgvGIF });
        }

        private void BtnRegistrar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtConcepto.Text) || string.IsNullOrWhiteSpace(txtImporte.Text)) return;
            if (decimal.TryParse(txtImporte.Text, out decimal imp))
            {
                dgvGIF.Rows.Add($"GIF-0{dgvGIF.Rows.Count + 1}", txtConcepto.Text.Trim(), $"${imp:N2}", cbBaseProrrateo.SelectedItem.ToString());
                txtConcepto.Clear(); txtImporte.Clear();
            }
        }

        private void BtnProrratear_Click(object sender, EventArgs e)
        {
            // Simulación del cálculo del documento (Tasa de aplicación = Total GIF / Total Base)
            MessageBox.Show("Prorrateo Ejecutado Exitosamente:\n\nTotal GIF: $10,000.00\nBase Total (Horas MOD): 100 hrs\nTasa Aplicada: $100.00 por hora\n\nGIF asignados a Órdenes:\n• OP-001 (40 hrs): $4,000.00\n• OP-002 (30 hrs): $3,000.00\n• OP-003 (30 hrs): $3,000.00", "Prorrateo Completado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Label CrearEtiqueta(string t, int x, int y) => new Label { Text = t, Location = new Point(x, y), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true };
        private TextBox CrearTextBox(int x, int y, int w) => new TextBox { Location = new Point(x, y), Width = w, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White, Font = new Font("Segoe UI", 10F) };
    }
}
