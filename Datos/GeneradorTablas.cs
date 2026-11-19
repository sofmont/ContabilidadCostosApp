using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using CostosOP.Modelos;

namespace CostosOP.Datos
{
    /// <summary>Secciones (módulos) que el usuario puede elegir al exportar a Excel.</summary>
    public enum SeccionExport
    {
        MateriaPrima, TarjetasAlmacen, Ordenes, ManoObra, GIF, ArticulosTerminados, Resultados, Reportes
    }

    /// <summary>
    /// Construye las tablas (DataTable) de cada módulo. Las usan tanto la pantalla de Reportes
    /// como el exportador a Excel, así ambos muestran exactamente la misma información.
    /// Convenciones:
    ///   • TableName = título que se muestra arriba de la hoja.
    ///   • ExtendedProperties["Hoja"] = nombre corto de la pestaña de Excel (máx. 31 caracteres).
    ///   • Columna opcional "Resaltar" (bool) marca renglones de totales (se muestran en negritas).
    /// </summary>
    public static class GeneradorTablas
    {
        private static Repositorio Repo { get { return Repositorio.Instancia; } }

        // ------------------------------------------------------------------ Catálogo de secciones

        public static string Nombre(SeccionExport s)
        {
            switch (s)
            {
                case SeccionExport.MateriaPrima: return "Materia Prima";
                case SeccionExport.TarjetasAlmacen: return "Tarjetas de Almacén";
                case SeccionExport.Ordenes: return "Órdenes de Producción";
                case SeccionExport.ManoObra: return "Mano de Obra";
                case SeccionExport.GIF: return "Gastos Indirectos (GIF)";
                case SeccionExport.ArticulosTerminados: return "Artículos Terminados";
                case SeccionExport.Resultados: return "Resultados y Costos";
                default: return "Reportes";
            }
        }

        public static string Descripcion(SeccionExport s)
        {
            switch (s)
            {
                case SeccionExport.MateriaPrima: return "Catálogo, existencias y costo promedio";
                case SeccionExport.TarjetasAlmacen: return "Una hoja (kardex) por cada material";
                case SeccionExport.Ordenes: return "Concentrado de órdenes y hojas de costos";
                case SeccionExport.ManoObra: return "Registros de nómina y MOD por orden";
                case SeccionExport.GIF: return "Gastos, lotes de prorrateo y GIF por orden";
                case SeccionExport.ArticulosTerminados: return "Existencias y tarjeta de almacén de PT";
                case SeccionExport.Resultados: return "CT y CU por orden con totales";
                default: return "Estado de costo de producción y ventas, bitácora";
            }
        }

        /// <summary>Hojas de Excel que genera cada sección.</summary>
        public static List<DataTable> Hojas(SeccionExport s)
        {
            switch (s)
            {
                case SeccionExport.MateriaPrima:
                    return new List<DataTable> { CatalogoMateriaPrima() };
                case SeccionExport.TarjetasAlmacen:
                    var tarjetas = Repo.Datos.Materiales.OrderBy(m => m.Codigo).Select(m => TarjetaAlmacen(m)).ToList();
                    if (tarjetas.Count == 0) tarjetas.Add(KardexConsolidado());
                    return tarjetas;
                case SeccionExport.Ordenes:
                    return new List<DataTable> { ConcentradoOrdenes(), HojasDeCostos() };
                case SeccionExport.ManoObra:
                    return new List<DataTable> { Nomina(), ModPorOrden() };
                case SeccionExport.GIF:
                    return new List<DataTable> { GastosIndirectos(), GifPorOrden(), Prorrateos() };
                case SeccionExport.ArticulosTerminados:
                    return new List<DataTable> { ExistenciasPT(), MovimientosPT() };
                case SeccionExport.Resultados:
                    return new List<DataTable> { Resultados() };
                default:
                    return new List<DataTable> { EstadoDeCosto(), Bitacora() };
            }
        }

        // ------------------------------------------------------------------ Materia prima y Kardex

        public static DataTable CatalogoMateriaPrima()
        {
            var t = Nueva("Catálogo y existencias de materia prima", "Materia Prima");
            Columnas(t, "Código", typeof(string), "Nombre", typeof(string), "Unidad", typeof(string),
                "Existencia", typeof(decimal), "Stock Mínimo", typeof(decimal), "Costo Promedio", typeof(decimal),
                "Saldo Importe", typeof(decimal), "Estado", typeof(string), "Resaltar", typeof(bool));
            var mats = Repo.Datos.Materiales.OrderBy(m => m.Codigo).ToList();
            foreach (var m in mats)
                t.Rows.Add(m.Codigo, m.Nombre, m.Unidad, m.Existencia, m.StockMinimo, m.CostoPromedio,
                    m.SaldoImporte, m.BajoMinimo ? "Bajo mínimo" : "Suficiente", false);
            t.Rows.Add("TOTAL", mats.Count + " materiales", "", DBNull.Value, DBNull.Value, DBNull.Value,
                mats.Sum(m => m.SaldoImporte), "", true);
            return t;
        }

        public static DataTable TarjetaAlmacen(MateriaPrima m)
        {
            var t = Nueva(string.Format("Tarjeta de almacén — {0} · {1} ({2}) · Costo promedio", m.Codigo, m.Nombre, m.Unidad),
                "Kardex " + m.Codigo);
            Columnas(t, "Fecha", typeof(DateTime), "Concepto", typeof(string), "Referencia", typeof(string),
                "Entrada Cantidad", typeof(decimal), "Entrada CU", typeof(decimal), "Entrada Importe", typeof(decimal),
                "Salida Cantidad", typeof(decimal), "Salida CU", typeof(decimal), "Salida Importe", typeof(decimal),
                "Saldo Cantidad", typeof(decimal), "Costo Promedio", typeof(decimal), "Saldo Importe", typeof(decimal));
            foreach (var k in Repo.Datos.MovimientosMP.Where(x => x.CodigoMaterial == m.Codigo).OrderBy(x => x.Fecha).ThenBy(x => x.Id))
                t.Rows.Add(k.Fecha, Textos.Movimiento(k.Tipo), k.Referencia,
                    SinCero(k.EntradaCantidad), SinCero(k.EntradaCostoUnitario), SinCero(k.EntradaImporte),
                    SinCero(k.SalidaCantidad), SinCero(k.SalidaCostoUnitario), SinCero(k.SalidaImporte),
                    k.SaldoCantidad, k.CostoPromedio, k.SaldoImporte);
            return t;
        }

        public static DataTable KardexConsolidado()
        {
            var t = Nueva("Kardex consolidado de materia prima (costo promedio)", "Kardex consolidado");
            Columnas(t, "Fecha", typeof(DateTime), "Material", typeof(string), "Movimiento", typeof(string),
                "Referencia", typeof(string), "Entrada Cantidad", typeof(decimal), "Entrada Importe", typeof(decimal),
                "Salida Cantidad", typeof(decimal), "Salida Importe", typeof(decimal), "Saldo Cantidad", typeof(decimal),
                "Costo Promedio", typeof(decimal), "Saldo Importe", typeof(decimal));
            foreach (var m in Repo.Datos.MovimientosMP.OrderBy(x => x.CodigoMaterial).ThenBy(x => x.Fecha).ThenBy(x => x.Id))
                t.Rows.Add(m.Fecha, m.CodigoMaterial, Textos.Movimiento(m.Tipo), m.Referencia,
                    SinCero(m.EntradaCantidad), SinCero(m.EntradaImporte), SinCero(m.SalidaCantidad), SinCero(m.SalidaImporte),
                    m.SaldoCantidad, m.CostoPromedio, m.SaldoImporte);
            return t;
        }

        // ------------------------------------------------------------------ Órdenes

        public static DataTable ConcentradoOrdenes()
        {
            var t = Nueva("Concentrado de órdenes de producción", "Órdenes");
            Columnas(t, "Folio", typeof(string), "Producto", typeof(string), "Cliente", typeof(string),
                "Estado", typeof(string), "Fecha Alta", typeof(DateTime), "Fecha Término", typeof(DateTime),
                "Unidades", typeof(int), "Horas MOD", typeof(decimal), "MPD", typeof(decimal), "MOD", typeof(decimal),
                "GIF", typeof(decimal), "Costo Total", typeof(decimal), "Costo Unitario", typeof(decimal));
            foreach (var o in Repo.Datos.Ordenes.OrderBy(x => x.Folio))
                t.Rows.Add(o.Folio, o.Producto, o.Cliente, Textos.Estado(o.Estado), o.FechaAlta,
                    o.FechaTermino.HasValue ? (object)o.FechaTermino.Value : DBNull.Value,
                    o.Cantidad, o.HorasMOD, o.MPD, o.MOD, o.GIF, o.CostoTotal, o.CostoUnitario);
            return t;
        }

        /// <summary>Hoja de costos de todas las órdenes, con subtotales por orden.</summary>
        public static DataTable HojasDeCostos()
        {
            var t = Nueva("Hojas de costos por orden (detalle de cargos)", "Hojas de costos");
            Columnas(t, "Folio", typeof(string), "Fecha", typeof(DateTime), "Elemento", typeof(string),
                "Concepto", typeof(string), "Referencia", typeof(string), "Cantidad", typeof(decimal),
                "Importe", typeof(decimal), "Resaltar", typeof(bool));
            foreach (var o in Repo.Datos.Ordenes.Where(x => x.Estado != EstadoOrden.Cancelada).OrderBy(x => x.Folio))
            {
                foreach (var c in Repo.CargosDeOrden(o.Folio))
                    t.Rows.Add(o.Folio, c.Fecha, c.Elemento.ToString(), c.Concepto, c.Referencia, c.Cantidad, c.Importe, false);
                t.Rows.Add(o.Folio, DBNull.Value, "CT",
                    string.Format("{0} · {1:N0} u. · MPD {2:C2} + MOD {3:C2} + GIF {4:C2} · CU {5:C4}",
                        o.Producto, o.Cantidad, o.MPD, o.MOD, o.GIF, o.CostoUnitario),
                    "", DBNull.Value, o.CostoTotal, true);
            }
            return t;
        }

        /// <summary>Hoja de costos de una sola orden (pantalla de Reportes).</summary>
        public static DataTable HojaDeCostos(string folio)
        {
            var o = Repo.BuscarOrden(folio);
            var t = Nueva(o == null ? "Hoja de costos (no hay órdenes)"
                : string.Format("Hoja de costos {0} · {1} · {2:N0} u. · {3} · Cliente: {4}",
                    o.Folio, o.Producto, o.Cantidad, Textos.Estado(o.Estado), o.Cliente),
                o == null ? "Hoja de costos" : "Hoja " + o.Folio);
            Columnas(t, "Fecha", typeof(DateTime), "Elemento", typeof(string), "Concepto", typeof(string),
                "Referencia", typeof(string), "Cantidad", typeof(decimal), "Importe", typeof(decimal), "Resaltar", typeof(bool));
            if (o == null) return t;

            foreach (var c in Repo.CargosDeOrden(o.Folio))
                t.Rows.Add(c.Fecha, c.Elemento.ToString(), c.Concepto, c.Referencia, c.Cantidad, c.Importe, false);
            t.Rows.Add(DBNull.Value, "", "Total materia prima directa (MPD)", "", DBNull.Value, o.MPD, true);
            t.Rows.Add(DBNull.Value, "", "Total mano de obra directa (MOD)", "", o.HorasMOD, o.MOD, true);
            t.Rows.Add(DBNull.Value, "", "Total gastos indirectos (GIF)", "", DBNull.Value, o.GIF, true);
            t.Rows.Add(DBNull.Value, "", "COSTO TOTAL (CT = MPD + MOD + GIF)", "", DBNull.Value, o.CostoTotal, true);
            t.Rows.Add(DBNull.Value, "", "COSTO UNITARIO (CT ÷ unidades)", "", (decimal)o.Cantidad, o.CostoUnitario, true);
            return t;
        }

        // ------------------------------------------------------------------ Mano de obra

        public static DataTable Nomina()
        {
            var t = Nueva("Registros de nómina cargados a órdenes (MOD)", "Nómina");
            Columnas(t, "Fecha", typeof(DateTime), "Folio", typeof(string), "Operario", typeof(string),
                "Puesto", typeof(string), "Horas", typeof(decimal), "Tarifa por Hora", typeof(decimal),
                "Importe", typeof(decimal), "Resaltar", typeof(bool));
            var nomina = Repo.Datos.Nomina.OrderBy(n => n.Fecha).ThenBy(n => n.Id).ToList();
            foreach (var n in nomina)
                t.Rows.Add(n.Fecha, n.Folio, n.Operario, n.Puesto, n.Horas, n.TarifaHora, n.Importe, false);
            t.Rows.Add(DBNull.Value, "TOTAL", nomina.Count + " registros", "", nomina.Sum(n => n.Horas), DBNull.Value, nomina.Sum(n => n.Importe), true);
            return t;
        }

        public static DataTable ModPorOrden()
        {
            var t = Nueva("Resumen de mano de obra directa por orden", "MOD por orden");
            Columnas(t, "Folio", typeof(string), "Producto", typeof(string), "Estado", typeof(string),
                "Horas", typeof(decimal), "MOD", typeof(decimal), "Costo por Hora", typeof(decimal));
            foreach (var o in Repo.Datos.Ordenes.Where(x => x.HorasMOD > 0 || x.MOD > 0).OrderBy(x => x.Folio))
                t.Rows.Add(o.Folio, o.Producto, Textos.Estado(o.Estado), o.HorasMOD, o.MOD,
                    o.HorasMOD > 0 ? Math.Round(o.MOD / o.HorasMOD, 2) : 0m);
            return t;
        }

        // ------------------------------------------------------------------ GIF

        public static DataTable GastosIndirectos()
        {
            var t = Nueva("Gastos indirectos de fabricación reales", "GIF");
            Columnas(t, "No", typeof(int), "Fecha", typeof(DateTime), "Concepto", typeof(string),
                "Categoría", typeof(string), "Importe", typeof(decimal), "Estado", typeof(string), "Resaltar", typeof(bool));
            var gastos = Repo.Datos.Gastos.OrderBy(g => g.Fecha).ThenBy(g => g.Id).ToList();
            foreach (var g in gastos)
                t.Rows.Add(g.Id, g.Fecha, g.Concepto, g.Categoria, g.Importe,
                    g.Prorrateado ? "Prorrateado (lote " + g.Lote + ")" : "Pendiente", false);
            t.Rows.Add(DBNull.Value, DBNull.Value, "TOTAL GIF REGISTRADOS", "", gastos.Sum(g => g.Importe), "", true);
            t.Rows.Add(DBNull.Value, DBNull.Value, "Pendientes de prorratear", "", Repo.GifPendiente(), "", true);
            return t;
        }

        public static DataTable GifPorOrden()
        {
            var t = Nueva("GIF aplicados a cada orden (resultado del prorrateo)", "GIF por orden");
            Columnas(t, "Fecha", typeof(DateTime), "Folio", typeof(string), "Producto", typeof(string),
                "Lote", typeof(string), "Valor Base", typeof(decimal), "Importe", typeof(decimal));
            foreach (var c in Repo.Datos.Cargos.Where(x => x.Elemento == ElementoCosto.GIF).OrderBy(x => x.Fecha).ThenBy(x => x.Folio))
            {
                var o = Repo.BuscarOrden(c.Folio);
                t.Rows.Add(c.Fecha, c.Folio, o == null ? "" : o.Producto, c.Referencia, c.Cantidad, c.Importe);
            }
            return t;
        }

        public static DataTable Prorrateos()
        {
            var t = Nueva("Historial de prorrateos de gastos indirectos (tasa de aplicación)", "Prorrateos");
            Columnas(t, "Lote", typeof(int), "Fecha", typeof(DateTime), "Base", typeof(string),
                "Total GIF", typeof(decimal), "Total Base", typeof(decimal), "Tasa", typeof(decimal), "Órdenes", typeof(int));
            foreach (var p in Repo.Datos.Prorrateos.OrderBy(x => x.Lote))
                t.Rows.Add(p.Lote, p.Fecha, Textos.Base(p.Base), p.TotalGIF, p.TotalBase, p.Tasa, p.OrdenesAfectadas);
            return t;
        }

        // ------------------------------------------------------------------ Artículos terminados

        public static DataTable ExistenciasPT()
        {
            var t = Nueva("Existencias de artículos terminados", "Existencias PT");
            Columnas(t, "Producto", typeof(string), "Existencia", typeof(decimal), "Costo Promedio", typeof(decimal),
                "Saldo Importe", typeof(decimal), "Resaltar", typeof(bool));
            var productos = Repo.Datos.Productos.OrderBy(p => p.Producto).ToList();
            foreach (var p in productos) t.Rows.Add(p.Producto, p.Existencia, p.CostoPromedio, p.SaldoImporte, false);
            t.Rows.Add("TOTAL", productos.Sum(p => p.Existencia), DBNull.Value, productos.Sum(p => p.SaldoImporte), true);
            return t;
        }

        public static DataTable MovimientosPT()
        {
            var t = Nueva("Tarjeta de almacén de artículos terminados", "Movimientos PT");
            Columnas(t, "Fecha", typeof(DateTime), "Producto", typeof(string), "Concepto", typeof(string),
                "Referencia", typeof(string), "Entrada Cantidad", typeof(decimal), "Entrada Importe", typeof(decimal),
                "Salida Cantidad", typeof(decimal), "Salida Importe", typeof(decimal), "Saldo Cantidad", typeof(decimal),
                "Costo Promedio", typeof(decimal), "Saldo Importe", typeof(decimal));
            foreach (var m in Repo.Datos.MovimientosPT.OrderBy(x => x.Producto).ThenBy(x => x.Fecha).ThenBy(x => x.Id))
                t.Rows.Add(m.Fecha, m.Producto, m.Concepto, m.Referencia,
                    SinCero(m.EntradaCantidad), SinCero(m.EntradaImporte), SinCero(m.SalidaCantidad), SinCero(m.SalidaImporte),
                    m.SaldoCantidad, m.CostoPromedio, m.SaldoImporte);
            return t;
        }

        // ------------------------------------------------------------------ Resultados y reportes

        public static DataTable Resultados()
        {
            var t = Nueva("Resultados: costo total y unitario por orden (CT = MPD + MOD + GIF; CU = CT ÷ unidades)", "Resultados");
            Columnas(t, "Folio", typeof(string), "Producto", typeof(string), "Estado", typeof(string),
                "Unidades", typeof(int), "MPD", typeof(decimal), "MOD", typeof(decimal), "GIF", typeof(decimal),
                "Costo Total", typeof(decimal), "Costo Unitario", typeof(decimal), "Resaltar", typeof(bool));
            var ordenes = Repo.Datos.Ordenes.Where(o => o.Estado != EstadoOrden.Cancelada).OrderBy(o => o.Folio).ToList();
            foreach (var o in ordenes)
                t.Rows.Add(o.Folio, o.Producto, Textos.Estado(o.Estado), o.Cantidad, o.MPD, o.MOD, o.GIF,
                    o.CostoTotal, o.CostoUnitario, false);
            t.Rows.Add("TOTAL", ordenes.Count + " órdenes", "", ordenes.Sum(o => o.Cantidad),
                ordenes.Sum(o => o.MPD), ordenes.Sum(o => o.MOD), ordenes.Sum(o => o.GIF),
                ordenes.Sum(o => o.CostoTotal), DBNull.Value, true);
            return t;
        }

        public static DataTable EstadoDeCosto()
        {
            var t = Nueva("Estado de costo de producción y ventas al " + DateTime.Today.ToString("dd/MM/yyyy"), "Estado de costo");
            Columnas(t, "Concepto", typeof(string), "Parcial", typeof(decimal), "Total", typeof(decimal), "Resaltar", typeof(bool));
            foreach (var l in Repo.EstadoDeCosto())
                t.Rows.Add(l.Concepto, (object)l.Parcial ?? DBNull.Value, (object)l.Total ?? DBNull.Value, l.Resaltar);
            return t;
        }

        public static DataTable Bitacora()
        {
            var t = Nueva("Bitácora de auditoría (más reciente primero)", "Bitácora");
            Columnas(t, "Fecha y hora", typeof(string), "Módulo", typeof(string), "Acción", typeof(string), "Detalle", typeof(string));
            foreach (var b in Enumerable.Reverse(Repo.Datos.Bitacora))
                t.Rows.Add(b.Fecha.ToString("dd/MM/yyyy HH:mm:ss"), b.Modulo, b.Accion, b.Detalle);
            return t;
        }

        // ------------------------------------------------------------------ Utilerías

        private static DataTable Nueva(string titulo, string hoja)
        {
            var t = new DataTable(titulo) { Locale = CultureInfo.CurrentCulture };
            t.ExtendedProperties["Hoja"] = hoja;
            return t;
        }

        /// <summary>Agrega columnas en pares (nombre, tipo).</summary>
        private static void Columnas(DataTable t, params object[] pares)
        {
            for (int i = 0; i + 1 < pares.Length; i += 2)
                t.Columns.Add((string)pares[i], (Type)pares[i + 1]);
        }

        /// <summary>En las tarjetas de almacén, los ceros de entradas/salidas se dejan en blanco.</summary>
        private static object SinCero(decimal v)
        {
            return v == 0m ? (object)DBNull.Value : v;
        }
    }
}
