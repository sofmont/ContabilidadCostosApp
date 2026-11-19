using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace CostosOP.Datos
{
    /// <summary>
    /// Genera archivos Excel (.xlsx, formato Office Open XML) sin librerías externas.
    /// Cada DataTable se convierte en una hoja con título, encabezados con estilo,
    /// formatos de moneda / fecha / cantidad, renglones de totales en negritas
    /// y el encabezado inmovilizado.
    /// </summary>
    public static class ExportadorExcel
    {
        // Índices de estilos definidos en styles.xml (ver Estilos()).
        private const int EstiloEncabezado = 1;
        private const int EstiloTitulo = 2;
        private const int EstiloSubtitulo = 3;
        private const int EstiloTexto = 4;
        private const int EstiloMoneda = 5;
        private const int EstiloFecha = 6;
        private const int EstiloCantidad = 7;
        private const int EstiloMoneda4 = 8;
        private const int EstiloTasa = 9;
        private const int EstiloEntero = 10;
        private const int DesplazamientoResaltado = 7;   // 11..17 = mismas, en negritas con fondo
        private const int FilaEncabezado = 4;

        /// <summary>Guarda las tablas como hojas de un libro .xlsx.</summary>
        public static void Guardar(string ruta, IList<DataTable> hojas)
        {
            if (hojas == null || hojas.Count == 0)
                throw new ReglaNegocioException("Seleccione al menos una sección para exportar.");

            var nombres = NombresDeHoja(hojas);
            string temporal = ruta + ".tmp";
            using (var fs = new FileStream(temporal, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                Escribir(zip, "[Content_Types].xml", TiposDeContenido(hojas.Count));
                Escribir(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");
                Escribir(zip, "xl/workbook.xml", LibroXml(nombres));
                Escribir(zip, "xl/_rels/workbook.xml.rels", RelacionesLibro(hojas.Count));
                Escribir(zip, "xl/styles.xml", Estilos());
                for (int i = 0; i < hojas.Count; i++)
                    Escribir(zip, "xl/worksheets/sheet" + (i + 1) + ".xml", HojaXml(hojas[i]));
            }
            File.Copy(temporal, ruta, true);
            File.Delete(temporal);
        }

        // ------------------------------------------------------------------ Partes del paquete

        private static string TiposDeContenido(int hojas)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (int i = 1; i <= hojas; i++)
                sb.Append("<Override PartName=\"/xl/worksheets/sheet" + i + ".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            sb.Append("</Types>");
            return sb.ToString();
        }

        private static string LibroXml(IList<string> nombres)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
            sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
            sb.Append("<bookViews><workbookView/></bookViews><sheets>");
            for (int i = 0; i < nombres.Count; i++)
                sb.AppendFormat("<sheet name=\"{0}\" sheetId=\"{1}\" r:id=\"rId{1}\"/>", Xml(nombres[i]), i + 1);
            sb.Append("</sheets></workbook>");
            return sb.ToString();
        }

        private static string RelacionesLibro(int hojas)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 1; i <= hojas; i++)
                sb.AppendFormat("<Relationship Id=\"rId{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{0}.xml\"/>", i);
            sb.AppendFormat("<Relationship Id=\"rId{0}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>", hojas + 1);
            sb.Append("</Relationships>");
            return sb.ToString();
        }

        /// <summary>Hoja de estilos: fuentes, rellenos, bordes y formatos numéricos (es-MX).</summary>
        private static string Estilos()
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<numFmts count=\"6\">");
            sb.Append("<numFmt numFmtId=\"164\" formatCode=\"&quot;$&quot;#,##0.00\"/>");
            sb.Append("<numFmt numFmtId=\"165\" formatCode=\"dd/mm/yyyy\"/>");
            sb.Append("<numFmt numFmtId=\"166\" formatCode=\"#,##0.00\"/>");
            sb.Append("<numFmt numFmtId=\"167\" formatCode=\"&quot;$&quot;#,##0.0000\"/>");
            sb.Append("<numFmt numFmtId=\"168\" formatCode=\"0.000000\"/>");
            sb.Append("<numFmt numFmtId=\"169\" formatCode=\"#,##0\"/>");
            sb.Append("</numFmts>");
            sb.Append("<fonts count=\"5\">");
            sb.Append("<font><sz val=\"11\"/><name val=\"Calibri\"/><family val=\"2\"/></font>");
            sb.Append("<font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/><family val=\"2\"/></font>");
            sb.Append("<font><b/><sz val=\"14\"/><color rgb=\"FF1E2937\"/><name val=\"Calibri\"/><family val=\"2\"/></font>");
            sb.Append("<font><i/><sz val=\"10\"/><color rgb=\"FF6B7280\"/><name val=\"Calibri\"/><family val=\"2\"/></font>");
            sb.Append("<font><b/><sz val=\"11\"/><color rgb=\"FF1E2937\"/><name val=\"Calibri\"/><family val=\"2\"/></font>");
            sb.Append("</fonts>");
            sb.Append("<fills count=\"4\">");
            sb.Append("<fill><patternFill patternType=\"none\"/></fill>");
            sb.Append("<fill><patternFill patternType=\"gray125\"/></fill>");
            sb.Append("<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1E2937\"/><bgColor indexed=\"64\"/></patternFill></fill>");
            sb.Append("<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE3ECFA\"/><bgColor indexed=\"64\"/></patternFill></fill>");
            sb.Append("</fills>");
            sb.Append("<borders count=\"2\">");
            sb.Append("<border><left/><right/><top/><bottom/><diagonal/></border>");
            sb.Append("<border><left/><right/><top/><bottom style=\"thin\"><color rgb=\"FFD0D7E2\"/></bottom><diagonal/></border>");
            sb.Append("</borders>");
            sb.Append("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");

            int[] formatosDatos = { 0, 164, 165, 166, 167, 168, 169 };   // texto, moneda, fecha, cantidad, moneda4, tasa, entero
            sb.Append("<cellXfs count=\"" + (4 + formatosDatos.Length * 2) + "\">");
            sb.Append("<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>");                                   // 0 normal
            sb.Append("<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyAlignment=\"1\">" +
                      "<alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>");                                   // 1 encabezado
            sb.Append("<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>");                   // 2 título
            sb.Append("<xf numFmtId=\"0\" fontId=\"3\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>");                   // 3 subtítulo
            foreach (int fmt in formatosDatos)                                                                                        // 4..10 datos
                sb.AppendFormat("<xf numFmtId=\"{0}\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>", fmt);
            foreach (int fmt in formatosDatos)                                                                                        // 11..17 totales
                sb.AppendFormat("<xf numFmtId=\"{0}\" fontId=\"4\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>", fmt);
            sb.Append("</cellXfs>");
            sb.Append("<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>");
            sb.Append("</styleSheet>");
            return sb.ToString();
        }

        /// <summary>Convierte una DataTable en el XML de una hoja.</summary>
        private static string HojaXml(DataTable t)
        {
            bool tieneResaltar = t.Columns.Contains("Resaltar");
            var columnas = t.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "Resaltar").ToList();
            int n = columnas.Count;
            var estilos = columnas.Select(c => EstiloDeColumna(c)).ToArray();
            var anchos = columnas.Select(c => Math.Max(10.0, c.ColumnName.Length + 4.0)).ToArray();

            var filas = new StringBuilder();

            // Fila 1: título; fila 2: fecha de generación; fila 3 vacía; fila 4: encabezados.
            filas.Append("<row r=\"1\" ht=\"22\" customHeight=\"1\">").Append(CeldaTexto("A1", t.TableName, EstiloTitulo)).Append("</row>");
            filas.Append("<row r=\"2\">").Append(CeldaTexto("A2",
                "Sistema de Contabilidad de Costos por Órdenes de Producción · Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                EstiloSubtitulo)).Append("</row>");
            filas.AppendFormat("<row r=\"{0}\" ht=\"24\" customHeight=\"1\">", FilaEncabezado);
            for (int j = 0; j < n; j++)
                filas.Append(CeldaTexto(Ref(j, FilaEncabezado), columnas[j].ColumnName, EstiloEncabezado));
            filas.Append("</row>");

            int fila = FilaEncabezado;
            foreach (DataRow r in t.Rows)
            {
                fila++;
                bool resaltado = tieneResaltar && r["Resaltar"] is bool && (bool)r["Resaltar"];
                int extra = resaltado ? DesplazamientoResaltado : 0;
                filas.AppendFormat("<row r=\"{0}\">", fila);
                for (int j = 0; j < n; j++)
                {
                    object v = r[columnas[j]];
                    string celda = Ref(j, fila);
                    int estilo = estilos[j] + extra;
                    if (v == null || v == DBNull.Value)
                    {
                        if (resaltado) filas.AppendFormat("<c r=\"{0}\" s=\"{1}\"/>", celda, estilo);
                        continue;
                    }
                    if (v is DateTime)
                    {
                        filas.Append(CeldaNumero(celda, ((DateTime)v).ToOADate().ToString(CultureInfo.InvariantCulture), estilo));
                        anchos[j] = Math.Max(anchos[j], 12);
                    }
                    else if (v is decimal || v is double || v is float || v is int || v is long)
                    {
                        decimal d = Convert.ToDecimal(v, CultureInfo.InvariantCulture);
                        filas.Append(CeldaNumero(celda, d.ToString(CultureInfo.InvariantCulture), estilo));
                        anchos[j] = Math.Max(anchos[j], d.ToString("#,##0.00", CultureInfo.InvariantCulture).Length + 4);
                    }
                    else if (v is bool)
                    {
                        filas.Append(CeldaTexto(celda, (bool)v ? "Sí" : "No", estilo));
                    }
                    else
                    {
                        string texto = Convert.ToString(v, CultureInfo.CurrentCulture);
                        filas.Append(CeldaTexto(celda, texto, EstiloParaTexto(estilo, resaltado)));
                        anchos[j] = Math.Max(anchos[j], Math.Min(60, texto.Length + 2));
                    }
                }
                filas.Append("</row>");
            }
            if (t.Rows.Count == 0)
                filas.AppendFormat("<row r=\"{0}\">{1}</row>", FilaEncabezado + 1,
                    CeldaTexto(Ref(0, FilaEncabezado + 1), "Sin registros", EstiloSubtitulo));

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
            sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
            // Encabezado inmovilizado: al desplazarse hacia abajo siempre se ven los títulos de columna.
            sb.AppendFormat("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"{0}\" topLeftCell=\"A{1}\" activePane=\"bottomLeft\" state=\"frozen\"/>" +
                            "<selection pane=\"bottomLeft\" activeCell=\"A{1}\" sqref=\"A{1}\"/></sheetView></sheetViews>",
                FilaEncabezado, FilaEncabezado + 1);
            sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");
            if (n > 0)
            {
                sb.Append("<cols>");
                for (int j = 0; j < n; j++)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "<col min=\"{0}\" max=\"{0}\" width=\"{1:0.##}\" customWidth=\"1\"/>", j + 1, anchos[j]);
                sb.Append("</cols>");
            }
            sb.Append("<sheetData>").Append(filas).Append("</sheetData>");
            sb.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\"/>");
            sb.Append("<pageSetup orientation=\"landscape\"/>");
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        /// <summary>Un texto en columna numérica (p. ej. "TOTAL") usa el estilo de texto equivalente.</summary>
        private static int EstiloParaTexto(int estilo, bool resaltado)
        {
            return resaltado ? EstiloTexto + DesplazamientoResaltado : (estilo >= EstiloTexto && estilo <= EstiloEntero ? EstiloTexto : estilo);
        }

        /// <summary>Elige el formato según el tipo y el nombre de la columna.</summary>
        private static int EstiloDeColumna(DataColumn c)
        {
            Type t = c.DataType;
            string n = c.ColumnName.ToLowerInvariant();
            if (t == typeof(DateTime)) return EstiloFecha;
            if (t == typeof(int) || t == typeof(long))
                return (n == "no" || n == "lote") ? EstiloTexto : EstiloEntero;
            if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            {
                if (n.Contains("tasa")) return EstiloTasa;
                string[] cantidades = { "cantidad", "horas", "existencia", "unidades", "mínimo", "minimo", "base" };
                if (cantidades.Any(x => n.Contains(x))) return EstiloCantidad;
                if (n.Contains("promedio") || n.Contains("unitario")) return EstiloMoneda4;
                return EstiloMoneda;
            }
            return EstiloTexto;
        }

        // ------------------------------------------------------------------ Utilerías XML

        private static string CeldaTexto(string referencia, string texto, int estilo)
        {
            return string.Format("<c r=\"{0}\" t=\"inlineStr\" s=\"{1}\"><is><t xml:space=\"preserve\">{2}</t></is></c>",
                referencia, estilo, Xml(texto));
        }

        private static string CeldaNumero(string referencia, string valor, int estilo)
        {
            return string.Format("<c r=\"{0}\" s=\"{1}\"><v>{2}</v></c>", referencia, estilo, valor);
        }

        /// <summary>Referencia de celda tipo A1 (columna base 0, fila base 1).</summary>
        private static string Ref(int columna, int fila)
        {
            string letras = string.Empty;
            int c = columna + 1;
            while (c > 0)
            {
                int resto = (c - 1) % 26;
                letras = (char)('A' + resto) + letras;
                c = (c - 1) / 26;
            }
            return letras + fila.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Escapa texto para XML y elimina caracteres de control no permitidos.</summary>
        private static string Xml(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length + 16);
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default:
                        if (ch >= 0x20 || ch == '\t' || ch == '\n' || ch == '\r') sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Nombres de hoja válidos para Excel: máx. 31 caracteres, sin []:*?/\ y sin repetirse.</summary>
        private static List<string> NombresDeHoja(IList<DataTable> hojas)
        {
            var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nombres = new List<string>();
            foreach (var t in hojas)
            {
                string baseNombre = t.ExtendedProperties.ContainsKey("Hoja") ? Convert.ToString(t.ExtendedProperties["Hoja"]) : t.TableName;
                if (string.IsNullOrWhiteSpace(baseNombre)) baseNombre = "Hoja";
                foreach (char ch in new[] { '[', ']', ':', '*', '?', '/', '\\' }) baseNombre = baseNombre.Replace(ch, '-');
                baseNombre = baseNombre.Trim('\'', ' ');
                if (baseNombre.Length > 31) baseNombre = baseNombre.Substring(0, 31);

                string nombre = baseNombre;
                int k = 2;
                while (usados.Contains(nombre))
                {
                    string sufijo = " (" + k++ + ")";
                    nombre = (baseNombre.Length + sufijo.Length > 31 ? baseNombre.Substring(0, 31 - sufijo.Length) : baseNombre) + sufijo;
                }
                usados.Add(nombre);
                nombres.Add(nombre);
            }
            return nombres;
        }

        private static void Escribir(ZipArchive zip, string ruta, string contenido)
        {
            var entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal);
            using (var w = new StreamWriter(entrada.Open(), new UTF8Encoding(false)))
                w.Write(contenido);
        }
    }
}
