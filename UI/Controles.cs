using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CostosOP.UI
{
    // ==========================================================================
    //  Tarjeta KPI (indicador superior)
    //  Las etiquetas se acomodan una debajo de otra según la altura REAL de la fuente,
    //  así no se enciman aunque Windows esté al 125 % o 150 %.
    // ==========================================================================
    public class TarjetaKpi : Panel
    {
        private readonly Label lblTitulo;
        private readonly Label lblValor;
        private readonly Label lblDetalle;
        private Color acento;
        private bool acomodando;

        public TarjetaKpi(string titulo, Color acento)
        {
            this.acento = acento;
            DoubleBuffered = true;
            BackColor = Tema.Tarjeta;
            Size = new Size(Tema.E(240), Tema.E(90));
            Margin = new Padding(0, 0, Tema.E(12), Tema.E(10));

            lblTitulo = CrearEtiqueta(titulo.ToUpperInvariant(), Tema.FuentePequena, Tema.TextoTenue);
            lblValor = CrearEtiqueta("—", Tema.FuenteKpi, Tema.Texto);
            lblDetalle = CrearEtiqueta(" ", Tema.FuentePequena, Tema.TextoTenue);
            Controls.Add(lblTitulo);
            Controls.Add(lblValor);
            Controls.Add(lblDetalle);
            Acomodar();
        }

        private static Label CrearEtiqueta(string texto, Font fuente, Color color)
        {
            return new Label
            {
                Text = texto, Font = fuente, ForeColor = color, AutoSize = true,
                BackColor = Color.Transparent, Margin = new Padding(0), Padding = new Padding(0)
            };
        }

        public string Valor { get { return lblValor.Text; } set { lblValor.Text = value; Acomodar(); } }

        public string Detalle
        {
            get { return lblDetalle.Text; }
            set { lblDetalle.Text = string.IsNullOrEmpty(value) ? " " : value; Acomodar(); }
        }

        public Color Acento
        {
            get { return acento; }
            set { acento = value; Invalidate(); }
        }

        /// <summary>Apila título, valor y detalle y ajusta el tamaño de la tarjeta a su contenido.</summary>
        private void Acomodar()
        {
            // El constructor de Panel dispara OnLayout al asignar Size, antes de que existan las etiquetas.
            if (acomodando || lblTitulo == null || lblValor == null || lblDetalle == null) return;
            acomodando = true;
            try
            {
                int x = Tema.E(18);
                lblTitulo.Location = new Point(x, Tema.E(10));
                lblValor.Location = new Point(x - Tema.E(2), lblTitulo.Bottom + Tema.E(4));
                lblDetalle.Location = new Point(x, lblValor.Bottom + Tema.E(2));

                int contenido = Math.Max(lblTitulo.PreferredWidth, Math.Max(lblValor.PreferredWidth, lblDetalle.PreferredWidth));
                int ancho = Math.Max(Tema.E(230), contenido + x + Tema.E(18));
                int alto = lblDetalle.Bottom + Tema.E(12);
                if (Width != ancho || Height != alto) Size = new Size(ancho, alto);
            }
            finally
            {
                acomodando = false;
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            Acomodar();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var b = new SolidBrush(acento)) e.Graphics.FillRectangle(b, 0, 0, Tema.E(4), Height);
            using (var p = new Pen(Tema.Borde)) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
    }

    // ==========================================================================
    //  Panel de captura vertical (etiqueta + control)
    // ==========================================================================
    public class PanelFormulario : FlowLayoutPanel
    {
        /// <summary>Ancho de los controles de captura (escalado a la pantalla).</summary>
        public static int AnchoControl { get { return Tema.E(284); } }

        public PanelFormulario()
        {
            FlowDirection = FlowDirection.TopDown;
            WrapContents = false;
            AutoScroll = true;
            Dock = DockStyle.Fill;
            BackColor = Tema.Superficie;
            Padding = new Padding(Tema.E(18), Tema.E(12), Tema.E(10), Tema.E(12));
        }

        public Label Seccion(string titulo, string ayuda)
        {
            var lbl = new Label
            {
                Text = titulo, Font = Tema.FuenteSeccion, ForeColor = Tema.Texto, AutoSize = true,
                MaximumSize = new Size(AnchoControl, 0),
                Margin = new Padding(0, Controls.Count == 0 ? Tema.E(2) : Tema.E(14), 0, Tema.E(2))
            };
            Controls.Add(lbl);
            if (!string.IsNullOrEmpty(ayuda)) Nota(ayuda);
            return lbl;
        }

        public Label Nota(string texto)
        {
            var lbl = new Label
            {
                Text = texto, Font = Tema.FuentePequena, ForeColor = Tema.TextoTenue, AutoSize = true,
                MaximumSize = new Size(AnchoControl, 0), Margin = new Padding(0, 0, 0, Tema.E(8))
            };
            Controls.Add(lbl);
            return lbl;
        }

        public T Campo<T>(string etiqueta, T control) where T : Control
        {
            Controls.Add(new Label
            {
                Text = etiqueta, Font = Tema.FuentePequena, ForeColor = Tema.TextoTenue,
                AutoSize = true, Margin = new Padding(0, Tema.E(4), 0, Tema.E(2))
            });
            control.Width = AnchoControl;
            control.Margin = new Padding(0, 0, 0, Tema.E(6));
            Controls.Add(control);
            return control;
        }

        public Button Boton(string texto, Color color)
        {
            var b = Tema.CrearBoton(texto, color);
            b.Width = AnchoControl;
            b.Margin = new Padding(0, Tema.E(8), 0, Tema.E(4));
            Controls.Add(b);
            return b;
        }

        /// <summary>
        /// Etiqueta destacada para cálculos en vivo (importe, disponible, etc.).
        /// Crece en alto según el número de líneas del texto.
        /// </summary>
        public Label Indicador(string texto, Color color)
        {
            var lbl = new Label
            {
                Text = texto, Font = Tema.FuenteNegrita, ForeColor = color, AutoSize = true,
                MinimumSize = new Size(AnchoControl, Tema.FuenteNegrita.Height + Tema.E(20)),
                MaximumSize = new Size(AnchoControl, 0), BackColor = Tema.Tarjeta,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Tema.E(10), Tema.E(8), Tema.E(6), Tema.E(8)),
                Margin = new Padding(0, Tema.E(6), 0, Tema.E(4))
            };
            Controls.Add(lbl);
            return lbl;
        }

        public void Separador()
        {
            Controls.Add(new Panel { Width = AnchoControl, Height = 1, BackColor = Tema.Borde, Margin = new Padding(0, Tema.E(14), 0, Tema.E(2)) });
        }
    }

    // ==========================================================================
    //  Botón de la barra de navegación superior
    // ==========================================================================
    public class BotonNav : Button
    {
        private bool activo;

        public BotonNav(string texto)
        {
            Text = texto;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Tema.Tarjeta;
            FlatAppearance.MouseDownBackColor = Tema.Tarjeta;
            Font = Tema.FuenteNegrita;
            BackColor = Tema.Superficie;
            ForeColor = Tema.TextoTenue;
            Height = Tema.E(64);
            Width = TextRenderer.MeasureText(texto, Tema.FuenteNegrita).Width + Tema.E(24);
            Margin = new Padding(0);
            TabStop = false;
            Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
        }

        public bool Activo
        {
            get { return activo; }
            set
            {
                activo = value;
                ForeColor = value ? Tema.Texto : Tema.TextoTenue;
                BackColor = value ? Tema.Tarjeta : Tema.Superficie;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            if (activo)
                using (var b = new SolidBrush(Tema.Azul))
                    pevent.Graphics.FillRectangle(b, Tema.E(8), Height - Tema.E(3), Width - Tema.E(16), Tema.E(3));
        }
    }

    // ==========================================================================
    //  Gráfica de barras apiladas MPD / MOD / GIF por orden (GDI+, sin dependencias)
    // ==========================================================================
    public class GraficaCostos : Control
    {
        public class Serie
        {
            public string Etiqueta { get; set; }
            public decimal MPD { get; set; }
            public decimal MOD { get; set; }
            public decimal GIF { get; set; }
            public decimal Total { get { return MPD + MOD + GIF; } }
        }

        private List<Serie> datos = new List<Serie>();

        public GraficaCostos()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Tema.Superficie;
            ForeColor = Tema.Texto;
            Font = Tema.FuentePequena;
        }

        public void Establecer(IEnumerable<Serie> series)
        {
            datos = series == null ? new List<Serie>() : series.ToList();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            Color[] colores = { Tema.Azul, Tema.Verde, Tema.Ambar };
            string[] nombres = { "MPD", "MOD", "GIF" };

            // Leyenda
            float xLeyenda = Width - Tema.E(220);
            for (int i = 0; i < 3; i++)
            {
                using (var b = new SolidBrush(colores[i])) g.FillRectangle(b, xLeyenda + i * Tema.E(70), Tema.E(10), Tema.E(12), Tema.E(12));
                TextRenderer.DrawText(g, nombres[i], Tema.FuentePequena, new Point((int)xLeyenda + i * Tema.E(70) + Tema.E(16), Tema.E(8)), Tema.TextoTenue);
            }

            var area = new RectangleF(Tema.E(78), Tema.E(40), Math.Max(10, Width - Tema.E(98)), Math.Max(10, Height - Tema.E(78)));
            if (datos.Count == 0 || datos.All(d => d.Total <= 0))
            {
                TextRenderer.DrawText(g, "Aún no hay órdenes con costos acumulados.", Tema.Fuente,
                    Rectangle.Round(area), Tema.TextoTenue, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            decimal maximo = EscalaBonita(datos.Max(d => d.Total));

            // Líneas guía
            using (var pen = new Pen(Tema.Borde))
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = area.Bottom - area.Height * i / 4f;
                    g.DrawLine(pen, area.Left, y, area.Right, y);
                    string etiqueta = (maximo * i / 4m).ToString("C0");
                    var tam = TextRenderer.MeasureText(etiqueta, Tema.FuentePequena);
                    TextRenderer.DrawText(g, etiqueta, Tema.FuentePequena,
                        new Point((int)area.Left - tam.Width - 6, (int)y - tam.Height / 2), Tema.TextoTenue);
                }
            }

            float ranura = area.Width / datos.Count;
            float ancho = Math.Min(Tema.E(56), ranura * 0.55f);
            for (int i = 0; i < datos.Count; i++)
            {
                var d = datos[i];
                float x = area.Left + ranura * i + (ranura - ancho) / 2f;
                float yBase = area.Bottom;
                decimal[] valores = { d.MPD, d.MOD, d.GIF };
                for (int k = 0; k < 3; k++)
                {
                    float alto = (float)(valores[k] / maximo) * area.Height;
                    if (alto <= 0) continue;
                    using (var b = new SolidBrush(colores[k])) g.FillRectangle(b, x, yBase - alto, ancho, alto);
                    yBase -= alto;
                }

                string total = d.Total.ToString("C0");
                var tt = TextRenderer.MeasureText(total, Tema.FuentePequena);
                TextRenderer.DrawText(g, total, Tema.FuentePequena,
                    new Point((int)(x + ancho / 2 - tt.Width / 2f), (int)yBase - tt.Height - 2), Tema.Texto);

                var te = TextRenderer.MeasureText(d.Etiqueta, Tema.FuentePequena);
                TextRenderer.DrawText(g, d.Etiqueta, Tema.FuentePequena,
                    new Point((int)(x + ancho / 2 - te.Width / 2f), (int)area.Bottom + 6), Tema.TextoTenue);
            }
        }

        /// <summary>Redondea el máximo a una escala legible (1, 2, 2.5, 5 x 10^n).</summary>
        private static decimal EscalaBonita(decimal valor)
        {
            if (valor <= 0) return 1m;
            double v = (double)valor * 1.1;
            double potencia = Math.Pow(10, Math.Floor(Math.Log10(v)));
            double[] pasos = { 1, 2, 2.5, 5, 10 };
            foreach (double p in pasos)
                if (p * potencia >= v) return (decimal)(p * potencia);
            return (decimal)(10 * potencia);
        }
    }
}
