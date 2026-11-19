using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace CostosOP.UI
{
    /// <summary>Elemento para ComboBox: valor interno + texto visible.</summary>
    public class ItemCombo
    {
        public ItemCombo(string valor, string texto) { Valor = valor; Texto = texto; }
        public string Valor { get; private set; }
        public string Texto { get; private set; }
        public override string ToString() { return Texto; }
    }

    /// <summary>
    /// Paleta oscura, tipografía y fábricas de controles estilizados.
    /// Centralizar el estilo aquí permite cambiar el tema completo desde un solo archivo.
    /// </summary>
    public static class Tema
    {
        // ---------------------------- Paleta ----------------------------
        public static readonly Color Fondo = Color.FromArgb(0x0D, 0x11, 0x17);       // #0D1117
        public static readonly Color Superficie = Color.FromArgb(0x14, 0x1A, 0x23);  // #141A23
        public static readonly Color Tarjeta = Color.FromArgb(0x1E, 0x29, 0x37);     // #1E2937
        public static readonly Color FilaAlterna = Color.FromArgb(0x18, 0x20, 0x2B);
        public static readonly Color Borde = Color.FromArgb(0x2B, 0x36, 0x46);
        public static readonly Color Seleccion = Color.FromArgb(0x1D, 0x3B, 0x63);
        public static readonly Color Texto = Color.FromArgb(0xE6, 0xED, 0xF3);
        public static readonly Color TextoTenue = Color.FromArgb(0x8B, 0x97, 0xA7);
        public static readonly Color Azul = Color.FromArgb(0x2F, 0x81, 0xF7);
        public static readonly Color Verde = Color.FromArgb(0x10, 0xB9, 0x81);
        public static readonly Color Ambar = Color.FromArgb(0xF5, 0x9E, 0x0B);
        public static readonly Color Rojo = Color.FromArgb(0xEF, 0x44, 0x44);
        public static readonly Color Morado = Color.FromArgb(0xA7, 0x8B, 0xFA);

        // ---------------------------- Tipografía ----------------------------
        public static readonly Font Fuente = new Font("Segoe UI", 9.5f);
        public static readonly Font FuenteNegrita = new Font("Segoe UI Semibold", 9.5f);
        public static readonly Font FuentePequena = new Font("Segoe UI", 8.25f);
        public static readonly Font FuenteSeccion = new Font("Segoe UI Semibold", 11f);
        public static readonly Font FuenteTitulo = new Font("Segoe UI Semibold", 17f);
        public static readonly Font FuenteKpi = new Font("Segoe UI Semibold", 17f);

        /// <summary>
        /// Factor de escala de la pantalla (1.0 = 100 %, 1.25 = 125 %, 1.5 = 150 %).
        /// Todas las medidas fijas en píxeles pasan por E() para que nada se encime con el zoom de Windows.
        /// </summary>
        public static readonly float Escala = CalcularEscala();

        public static int E(int pixeles) { return (int)Math.Round(pixeles * Escala); }

        public static int AnchoLateral { get { return PanelFormulario.AnchoControl + E(50); } }

        private static float CalcularEscala()
        {
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero)) return Math.Max(1f, g.DpiX / 96f);
            }
            catch { return 1f; }
        }

        public static Color Mezclar(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        // ---------------------------- Layout ----------------------------

        /// <summary>
        /// Agrega controles acoplados en el orden visual deseado (del más externo al más interno).
        /// WinForms acopla primero el último control agregado, por eso se agregan en orden inverso.
        /// </summary>
        public static void Apilar(Control padre, params Control[] externosPrimero)
        {
            padre.SuspendLayout();
            for (int i = externosPrimero.Length - 1; i >= 0; i--) padre.Controls.Add(externosPrimero[i]);
            padre.ResumeLayout(true);
        }

        public static Panel CrearPanelLateral()
        {
            return new Panel { Dock = DockStyle.Left, Width = AnchoLateral, BackColor = Superficie };
        }

        public static Panel CrearEspaciador(DockStyle dock = DockStyle.Left, int tamano = 14)
        {
            var p = new Panel { Dock = dock, BackColor = Fondo };
            if (dock == DockStyle.Left || dock == DockStyle.Right) p.Width = E(tamano); else p.Height = E(tamano);
            return p;
        }

        /// <summary>Tarjeta con título y contenido (normalmente un DataGridView).</summary>
        public static Panel CrearTarjeta(string titulo, Control contenido, out Label lblTitulo)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Superficie, Padding = new Padding(E(14), E(8), E(14), E(12)), Margin = new Padding(0) };
            lblTitulo = new Label
            {
                Text = titulo, Dock = DockStyle.Top, Height = FuenteSeccion.Height + E(14), Font = FuenteSeccion,
                AutoEllipsis = true,
                ForeColor = Texto, TextAlign = ContentAlignment.MiddleLeft
            };
            contenido.Dock = DockStyle.Fill;
            Apilar(p, lblTitulo, contenido);
            return p;
        }

        public static Panel CrearTarjeta(string titulo, Control contenido)
        {
            Label ignorar;
            return CrearTarjeta(titulo, contenido, out ignorar);
        }

        /// <summary>Divide un área en dos filas (porcentaje para la superior).</summary>
        public static TableLayoutPanel DosFilas(Control arriba, Control abajo, float porcentajeArriba)
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Fondo,
                Margin = new Padding(0), Padding = new Padding(0)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, porcentajeArriba));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f - porcentajeArriba));
            arriba.Dock = DockStyle.Fill;
            abajo.Dock = DockStyle.Fill;
            arriba.Margin = new Padding(0, 0, 0, 7);
            abajo.Margin = new Padding(0, 7, 0, 0);
            t.Controls.Add(arriba, 0, 0);
            t.Controls.Add(abajo, 0, 1);
            return t;
        }

        // ---------------------------- Controles de captura ----------------------------

        public static TextBox CrearTexto()
        {
            return new TextBox { BackColor = Tarjeta, ForeColor = Texto, BorderStyle = BorderStyle.FixedSingle, Font = Fuente };
        }

        public static ComboBox CrearCombo(bool editable = false)
        {
            var cb = new ComboBox
            {
                DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat, BackColor = Tarjeta, ForeColor = Texto, Font = Fuente,
                MaxDropDownItems = 14, IntegralHeight = false
            };
            if (editable)
            {
                cb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cb.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
            return cb;
        }

        public static NumericUpDown CrearNumero(int decimales, decimal maximo = 999999999m)
        {
            var n = new NumericUpDown
            {
                DecimalPlaces = decimales, Minimum = 0m, Maximum = maximo, ThousandsSeparator = true,
                BackColor = Tarjeta, ForeColor = Texto, BorderStyle = BorderStyle.FixedSingle,
                Font = Fuente, TextAlign = HorizontalAlignment.Right
            };
            // Selecciona todo al entrar para capturar rápido sin borrar manualmente.
            n.Enter += (s, e) => n.Select(0, n.Text.Length);
            return n;
        }

        public static DateTimePicker CrearFecha()
        {
            return new DateTimePicker
            {
                Format = DateTimePickerFormat.Short, Font = Fuente, Value = DateTime.Today,
                CalendarMonthBackground = Tarjeta, CalendarForeColor = Texto,
                CalendarTitleBackColor = Azul, CalendarTitleForeColor = Color.White
            };
        }

        public static Button CrearBoton(string texto, Color color)
        {
            var b = new Button
            {
                Text = texto, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White,
                Font = FuenteNegrita, Height = Math.Max(E(38), FuenteNegrita.Height + E(16)), Cursor = Cursors.Hand, UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Mezclar(color, Color.White, 0.15f);
            b.FlatAppearance.MouseDownBackColor = Mezclar(color, Color.Black, 0.2f);
            return b;
        }

        /// <summary>Botón tipo "pestaña segmentada" para alternar sub-operaciones (ancho ya escalado).</summary>
        public static Button CrearBotonSegmento(string texto, int anchoPixeles)
        {
            var b = new Button
            {
                Text = texto, FlatStyle = FlatStyle.Flat, BackColor = Tarjeta, ForeColor = TextoTenue,
                Font = FuenteNegrita, Width = anchoPixeles, Height = FuenteNegrita.Height + E(14), Margin = new Padding(0, 0, E(4), 0),
                Cursor = Cursors.Hand, UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Mezclar(Tarjeta, Color.White, 0.08f);
            return b;
        }

        public static void MarcarSegmento(Button b, bool activo)
        {
            b.BackColor = activo ? Azul : Tarjeta;
            b.ForeColor = activo ? Color.White : TextoTenue;
            b.FlatAppearance.MouseOverBackColor = activo ? Mezclar(Azul, Color.White, 0.12f) : Mezclar(Tarjeta, Color.White, 0.08f);
        }

        // ---------------------------- ComboBox helpers ----------------------------

        /// <summary>Rellena un combo conservando la selección previa (por valor) si aún existe.</summary>
        public static void LlenarCombo(ComboBox cb, IEnumerable<ItemCombo> items)
        {
            var previo = cb.SelectedItem as ItemCombo;
            string valorPrevio = previo == null ? null : previo.Valor;
            cb.BeginUpdate();
            cb.Items.Clear();
            foreach (var i in items) cb.Items.Add(i);
            cb.EndUpdate();
            if (cb.Items.Count == 0) { cb.SelectedIndex = -1; return; }
            int idx = 0;
            if (valorPrevio != null)
            {
                for (int i = 0; i < cb.Items.Count; i++)
                    if (((ItemCombo)cb.Items[i]).Valor == valorPrevio) { idx = i; break; }
            }
            cb.SelectedIndex = idx;
        }

        public static string ValorCombo(ComboBox cb)
        {
            var item = cb.SelectedItem as ItemCombo;
            return item == null ? null : item.Valor;
        }

        public static void SeleccionarValor(ComboBox cb, string valor)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                var item = cb.Items[i] as ItemCombo;
                if (item != null && item.Valor == valor) { cb.SelectedIndex = i; return; }
            }
        }

        // ---------------------------- DataGridView ----------------------------

        /// <summary>DataGridView de solo lectura con estilo oscuro y formato automático de columnas.</summary>
        public static DataGridView CrearGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = Superficie, BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Borde,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeight = FuenteNegrita.Height + E(18),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false, ReadOnly = true, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = Fuente
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Tarjeta;
            g.ColumnHeadersDefaultCellStyle.ForeColor = TextoTenue;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Tarjeta;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextoTenue;
            g.ColumnHeadersDefaultCellStyle.Font = FuenteNegrita;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

            g.DefaultCellStyle.BackColor = Superficie;
            g.DefaultCellStyle.ForeColor = Texto;
            g.DefaultCellStyle.SelectionBackColor = Seleccion;
            g.DefaultCellStyle.SelectionForeColor = Color.White;
            g.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = FilaAlterna;
            g.RowTemplate.Height = Fuente.Height + E(14);

            // Doble búfer para evitar parpadeo (propiedad protegida, se activa por reflexión).
            var prop = typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null) prop.SetValue(g, true, null);

            g.DataBindingComplete += AlEnlazarGrid;
            return g;
        }

        /// <summary>Oculta ceros en las columnas de entradas/salidas (estilo Kardex).</summary>
        public static void OcultarCerosMovimientos(DataGridView g)
        {
            g.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0 || !(e.Value is decimal)) return;
                string nombre = g.Columns[e.ColumnIndex].Name;
                if ((nombre.StartsWith("Entrada") || nombre.StartsWith("Salida")) && (decimal)e.Value == 0m)
                {
                    e.Value = string.Empty;
                    e.FormattingApplied = true;
                }
            };
        }

        /// <summary>Selecciona la fila cuyo valor en la columna indicada coincide.</summary>
        public static void SeleccionarFila(DataGridView g, string columna, string valor)
        {
            if (string.IsNullOrEmpty(valor) || !g.Columns.Contains(columna)) return;
            foreach (DataGridViewRow r in g.Rows)
            {
                if (Convert.ToString(r.Cells[columna].Value) == valor)
                {
                    g.CurrentCell = r.Cells[columna];
                    r.Selected = true;
                    return;
                }
            }
        }

        public static string ValorCelda(DataGridView g, string columna)
        {
            if (g.CurrentRow == null || !g.Columns.Contains(columna)) return null;
            return Convert.ToString(g.CurrentRow.Cells[columna].Value);
        }

        private static void AlEnlazarGrid(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            var g = (DataGridView)sender;
            foreach (DataGridViewColumn c in g.Columns)
            {
                c.HeaderText = Humanizar(c.Name);
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                Type t = c.ValueType;
                if (t == typeof(decimal) || t == typeof(double) || t == typeof(decimal?))
                {
                    c.DefaultCellStyle.Format = FormatoNumerico(c.Name);
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (t == typeof(int))
                {
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    if (c.Name != "No" && c.Name != "Lote") c.DefaultCellStyle.Format = "N0";
                }
                else if (t == typeof(DateTime) || t == typeof(DateTime?))
                {
                    c.DefaultCellStyle.Format = "dd/MM/yyyy";
                    c.FillWeight = 70;
                }
            }
        }

        /// <summary>"CostoUnitario" → "Costo Unitario".</summary>
        public static string Humanizar(string nombre)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < nombre.Length; i++)
            {
                char c = nombre[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(nombre[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Regla de formato según el nombre de la columna.</summary>
        private static string FormatoNumerico(string nombre)
        {
            string n = nombre.ToLowerInvariant();
            if (n.Contains("tasa")) return "N6";
            string[] cantidades = { "cant", "horas", "existencia", "unidades", "mínimo", "minimo", "base" };
            if (cantidades.Any(x => n.Contains(x))) return "N2";
            if (n.Contains("promedio")) return "C4";
            return "C2";
        }
    }
}
