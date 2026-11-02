using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class UserControlCapturas : UserControl
    {
        private TextBox txtNombre, txtSKU, txtCosto, txtCantidad;
        private ComboBox cbUnidad, cbTipoMovimiento;
        private DataGridView dgvMateriales;

        public UserControlCapturas()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.BackColor = Color.FromArgb(20, 26, 35);
            this.Dock = DockStyle.Fill;

            Label lblTitulo = new Label { Text = "Mapeo y Movimiento de Materia Prima", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            Label lblSKU = CrearEtiqueta("Código / SKU:", 20, 65);
            txtSKU = CrearTextBox(20, 90, 250);

            Label lblNombre = CrearEtiqueta("Nombre de Material:", 20, 130);
            txtNombre = CrearTextBox(20, 155, 250);

            Label lblUnidad = CrearEtiqueta("Unidad de Medida:", 20, 195);
            cbUnidad = new ComboBox { Location = new Point(20, 220), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White };
            cbUnidad.Items.AddRange(new string[] { "Kg", "Piezas", "Metros", "Litros" });
            cbUnidad.SelectedIndex = 0;

            Label lblTipo = CrearEtiqueta("Tipo de Movimiento:", 20, 260);
            cbTipoMovimiento = new ComboBox { Location = new Point(20, 285), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White };
            cbTipoMovimiento.Items.AddRange(new string[] { "Inventario Inicial", "Compra (Entrada)", "Baja a Producción (Salida)" });
            cbTipoMovimiento.SelectedIndex = 0;

            Label lblCantidad = CrearEtiqueta("Cantidad:", 20, 325);
            txtCantidad = CrearTextBox(20, 350, 250);

            Label lblCosto = CrearEtiqueta("Costo Unitario ($):", 20, 390);
            txtCosto = CrearTextBox(20, 415, 250);

            Button btnGuardar = new Button { Text = "Registrar Movimiento", Location = new Point(20, 470), Width = 250, Height = 40, BackColor = Color.FromArgb(0, 122, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            dgvMateriales = new DataGridView { Location = new Point(300, 65), Size = new Size(950, 650), BackgroundColor = Color.FromArgb(30, 41, 55), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };
            dgvMateriales.Columns.Add("SKU", "Código");
            dgvMateriales.Columns.Add("Nombre", "Material");
            dgvMateriales.Columns.Add("Unidad", "Unidad");
            dgvMateriales.Columns.Add("Movimiento", "Movimiento");
            dgvMateriales.Columns.Add("Cantidad", "Cantidad");
            dgvMateriales.Columns.Add("CostoU", "Costo Unit.");
            dgvMateriales.Columns.Add("CostoT", "Costo Total");

            // Datos de prueba iniciales
            dgvMateriales.Rows.Add("MP-001", "Lámina de Acero", "Piezas", "Inventario Inicial", "100", "$50.00", "$5,000.00");
            dgvMateriales.Rows.Add("MP-002", "Madera Pino", "Metros", "Compra (Entrada)", "50", "$120.00", "$6,000.00");

            this.Controls.AddRange(new Control[] { lblTitulo, lblSKU, txtSKU, lblNombre, txtNombre, lblUnidad, cbUnidad, lblTipo, cbTipoMovimiento, lblCantidad, txtCantidad, lblCosto, txtCosto, btnGuardar, dgvMateriales });
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSKU.Text) || string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtCantidad.Text) || string.IsNullOrWhiteSpace(txtCosto.Text))
            {
                MessageBox.Show("Complete todos los campos para registrar la materia prima.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (decimal.TryParse(txtCantidad.Text, out decimal cant) && decimal.TryParse(txtCosto.Text, out decimal costoU))
            {
                decimal total = cant * costoU;
                dgvMateriales.Rows.Add(txtSKU.Text.Trim(), txtNombre.Text.Trim(), cbUnidad.SelectedItem.ToString(), cbTipoMovimiento.SelectedItem.ToString(), cant.ToString(), $"${costoU:N2}", $"${total:N2}");

                txtSKU.Clear(); txtNombre.Clear(); txtCantidad.Clear(); txtCosto.Clear(); txtSKU.Focus();
                MessageBox.Show("Movimiento de Materia Prima guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Ingrese valores numéricos válidos en Cantidad y Costo.", "Error de formato", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Label CrearEtiqueta(string t, int x, int y) => new Label { Text = t, Location = new Point(x, y), ForeColor = Color.FromArgb(156, 163, 175), AutoSize = true };
        private TextBox CrearTextBox(int x, int y, int w) => new TextBox { Location = new Point(x, y), Width = w, BackColor = Color.FromArgb(30, 41, 55), ForeColor = Color.White, Font = new Font("Segoe UI", 10F) };
    }
}
