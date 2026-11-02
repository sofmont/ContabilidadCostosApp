using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ContabilidadCostosApp
{
    public partial class FormPrincipal : Form
    {
        private readonly Color ColorFondo = Color.FromArgb(20, 26, 35);
        private readonly Color ColorBarraSuperior = Color.FromArgb(13, 17, 23);
        private readonly Color ColorBotonNavegacion = Color.FromArgb(30, 41, 55);
        private readonly Color ColorBotonActivo = Color.FromArgb(0, 122, 255);

        private Panel panelBarraNavegacion;
        private Panel panelContenedor;

        public FormPrincipal()
        {
            this.Text = "Sistema de Contabilidad de Costos por Órdenes de Producción";
            this.Size = new Size(1300, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorFondo;

            // Barra Superior de Navegación
            panelBarraNavegacion = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ColorBarraSuperior,
                Padding = new Padding(5)
            };

            panelContenedor = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorFondo
            };

            // Creación de los 8 Botones de Navegación según el documento
            CrearBotonNav("MATERIA PRIMA", 0, (s, e) => CargarUserControl(new UserControlCapturas()));
            CrearBotonNav("ALMACÉN", 1, (s, e) => CargarUserControl(new UserControlTarjetasAlmacen()));
            CrearBotonNav("ARTÍCULOS", 2, (s, e) => CargarUserControl(new UserControlArticulosTerminados()));
            CrearBotonNav("ÓRDENES", 3, (s, e) => CargarUserControl(new UserControlOrdenesProduccion()));
            CrearBotonNav("MANO DE OBRA", 4, (s, e) => CargarUserControl(new UserControlManoObra()));
            CrearBotonNav("GIF & PRORRATEO", 5, (s, e) => CargarUserControl(new UserControlGastosIndirectos()));
            CrearBotonNav("RESULTADOS", 6, (s, e) => CargarUserControl(new UserControlResultados()));
            CrearBotonNav("REPORTES", 7, (s, e) => CargarUserControl(new UserControlReportes()));

            this.Controls.Add(panelContenedor);
            this.Controls.Add(panelBarraNavegacion);

            // Cargar Materia Prima por defecto
            CargarUserControl(new UserControlCapturas());
        }

        private void CrearBotonNav(string texto, int indice, EventHandler accionClick)
        {
            Button btn = new Button
            {
                Text = texto,
                Width = 148,
                Height = 42,
                Left = 8 + (indice * 153),
                Top = 9,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorBotonNavegacion,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += accionClick;

            btn.Click += (s, e) =>
            {
                foreach (Control ctrl in panelBarraNavegacion.Controls)
                {
                    if (ctrl is Button b) b.BackColor = ColorBotonNavegacion;
                }
                btn.BackColor = ColorBotonActivo;
            };

            if (indice == 0) btn.BackColor = ColorBotonActivo;

            panelBarraNavegacion.Controls.Add(btn);
        }

        private void CargarUserControl(UserControl uc)
        {
            panelContenedor.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            panelContenedor.Controls.Add(uc);
            uc.BringToFront();
        }
    }
}
