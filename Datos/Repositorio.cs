using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CostosOP.Modelos;

namespace CostosOP.Datos
{
    /// <summary>Excepción para violaciones de reglas de negocio (se muestran al usuario como aviso).</summary>
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje) : base(mensaje) { }
    }

    /// <summary>Renglón de la vista previa de prorrateo de GIF.</summary>
    public class DetalleProrrateo
    {
        public string Folio { get; set; }
        public string Producto { get; set; }
        public decimal ValorBase { get; set; }
        public decimal Importe { get; set; }
    }

    /// <summary>Resultado del cálculo de la tasa de aplicación de GIF.</summary>
    public class ResultadoProrrateo
    {
        public ResultadoProrrateo() { Detalle = new List<DetalleProrrateo>(); }
        public BaseProrrateo Base { get; set; }
        public decimal TotalGIF { get; set; }
        public decimal TotalBase { get; set; }
        public decimal Tasa { get; set; }
        public List<DetalleProrrateo> Detalle { get; private set; }
    }

    /// <summary>Línea de un reporte financiero (estado de costo).</summary>
    public class LineaReporte
    {
        public LineaReporte(string concepto, decimal? parcial, decimal? total, bool resaltar)
        {
            Concepto = concepto; Parcial = parcial; Total = total; Resaltar = resaltar;
        }
        public string Concepto { get; private set; }
        public decimal? Parcial { get; private set; }
        public decimal? Total { get; private set; }
        public bool Resaltar { get; private set; }
    }

    /// <summary>
    /// Repositorio único (Singleton) que concentra los datos y TODAS las reglas de negocio.
    /// La interfaz nunca modifica entidades directamente: siempre llama a estos métodos,
    /// que validan, calculan, auditan y notifican.
    /// Valuación de inventarios: costo promedio ponderado.
    /// </summary>
    public sealed class Repositorio
    {
        private static readonly Repositorio instancia = new Repositorio();
        public static Repositorio Instancia { get { return instancia; } }

        private bool suspendido;

        /// <summary>Se dispara después de cualquier operación que cambie los datos.</summary>
        public event EventHandler DatosCambiados;

        public DatosSistema Datos { get; private set; }
        public string RutaArchivo { get; private set; }

        private Repositorio()
        {
            Datos = new DatosSistema();
            RutaArchivo = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CostosOP", "datos_costos.xml");
        }

        // ======================================================================
        //  PERSISTENCIA
        // ======================================================================

        /// <summary>Carga los datos guardados; si no existen, carga datos de demostración.</summary>
        public void Cargar()
        {
            try
            {
                if (File.Exists(RutaArchivo))
                {
                    var serializador = new XmlSerializer(typeof(DatosSistema));
                    using (var fs = File.OpenRead(RutaArchivo))
                    {
                        Datos = (DatosSistema)serializador.Deserialize(fs) ?? new DatosSistema();
                    }
                    Normalizar();
                    return;
                }
            }
            catch (Exception)
            {
                // Archivo dañado: se respalda y se reinicia con datos de ejemplo.
                try { File.Copy(RutaArchivo, RutaArchivo + ".danado", true); } catch { /* sin acción */ }
            }
            CargarDemo();
        }

        /// <summary>Guarda los datos en XML (escritura atómica mediante archivo temporal).</summary>
        public void Guardar()
        {
            string carpeta = Path.GetDirectoryName(RutaArchivo);
            if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);
            string temporal = RutaArchivo + ".tmp";
            var serializador = new XmlSerializer(typeof(DatosSistema));
            using (var fs = File.Create(temporal))
            {
                serializador.Serialize(fs, Datos);
            }
            File.Copy(temporal, RutaArchivo, true);
            File.Delete(temporal);
        }

        private void Normalizar()
        {
            // Garantiza que ninguna colección quede nula tras deserializar.
            var d = Datos;
            if (d.Materiales == null) d.Materiales = new List<MateriaPrima>();
            if (d.MovimientosMP == null) d.MovimientosMP = new List<MovimientoKardex>();
            if (d.Ordenes == null) d.Ordenes = new List<OrdenProduccion>();
            if (d.Cargos == null) d.Cargos = new List<CargoOrden>();
            if (d.Nomina == null) d.Nomina = new List<RegistroNomina>();
            if (d.Gastos == null) d.Gastos = new List<GastoIndirecto>();
            if (d.Prorrateos == null) d.Prorrateos = new List<ProrrateoAplicado>();
            if (d.Productos == null) d.Productos = new List<ProductoTerminado>();
            if (d.MovimientosPT == null) d.MovimientosPT = new List<MovimientoPT>();
            if (d.Bitacora == null) d.Bitacora = new List<RegistroAuditoria>();
            if (d.SiguienteFolio < 1001) d.SiguienteFolio = 1001;
            if (d.SiguienteId < 1) d.SiguienteId = 1;
        }

        /// <summary>Inicia el sistema sin datos.</summary>
        public void IniciarEnBlanco()
        {
            Datos = new DatosSistema();
            Auditar("Sistema", "Inicio en blanco", "Se eliminaron todos los datos.");
            Notificar();
        }

        // ======================================================================
        //  CONSULTAS
        // ======================================================================

        public MateriaPrima BuscarMaterial(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            return Datos.Materiales.FirstOrDefault(m =>
                string.Equals(m.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public OrdenProduccion BuscarOrden(string folio)
        {
            if (string.IsNullOrWhiteSpace(folio)) return null;
            return Datos.Ordenes.FirstOrDefault(o =>
                string.Equals(o.Folio, folio.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public ProductoTerminado BuscarProducto(string producto)
        {
            if (string.IsNullOrWhiteSpace(producto)) return null;
            return Datos.Productos.FirstOrDefault(p =>
                string.Equals(p.Producto, producto.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Órdenes que aún aceptan cargos (abiertas o en proceso).</summary>
        public List<OrdenProduccion> OrdenesActivas()
        {
            return Datos.Ordenes.Where(o => o.EstaActiva).OrderBy(o => o.Folio).ToList();
        }

        public List<CargoOrden> CargosDeOrden(string folio)
        {
            return Datos.Cargos.Where(c => c.Folio == folio)
                .OrderBy(c => c.Fecha).ThenBy(c => c.Id).ToList();
        }

        public decimal GifPendiente()
        {
            return Datos.Gastos.Where(g => !g.Prorrateado).Sum(g => g.Importe);
        }

        /// <summary>Sugiere el siguiente código de material libre (MP-001, MP-002…).</summary>
        public string SugerirCodigoMaterial()
        {
            int n = Datos.Materiales.Count + 1;
            string codigo;
            do { codigo = "MP-" + n.ToString("000"); n++; } while (BuscarMaterial(codigo) != null);
            return codigo;
        }

        // ======================================================================
        //  MÓDULO 1 y 2: MATERIA PRIMA Y TARJETAS DE ALMACÉN
        // ======================================================================

        /// <summary>Alta de material en catálogo con inventario inicial opcional.</summary>
        public MateriaPrima AltaMaterial(string codigo, string nombre, string unidad, decimal stockMinimo,
            decimal cantidadInicial, decimal costoInicial, DateTime fecha)
        {
            codigo = Requerido(codigo, "Código").ToUpperInvariant();
            nombre = Requerido(nombre, "Nombre");
            unidad = Requerido(unidad, "Unidad de medida");
            if (BuscarMaterial(codigo) != null)
                throw new ReglaNegocioException("Ya existe un material con el código " + codigo + ".");
            if (stockMinimo < 0 || cantidadInicial < 0 || costoInicial < 0)
                throw new ReglaNegocioException("Las cantidades y costos no pueden ser negativos.");
            if (cantidadInicial > 0 && costoInicial <= 0)
                throw new ReglaNegocioException("Indique el costo unitario del inventario inicial.");

            var m = new MateriaPrima { Codigo = codigo, Nombre = nombre, Unidad = unidad, StockMinimo = stockMinimo };
            Datos.Materiales.Add(m);

            if (cantidadInicial > 0)
                RegistrarEntrada(m, TipoMovimiento.InventarioInicial, "Inventario inicial", "INV-INI",
                    cantidadInicial, costoInicial, fecha);

            Auditar("Materia Prima", "Alta de material",
                string.Format("{0} {1}; inventario inicial {2:N2} {3} a {4:C4}", codigo, nombre, cantidadInicial, unidad, costoInicial));
            Notificar();
            return m;
        }

        /// <summary>Entrada al almacén por compra.</summary>
        public void RegistrarCompra(string codigo, decimal cantidad, decimal costoUnitario, string referencia, DateTime fecha)
        {
            var m = MaterialExistente(codigo);
            Positivo(cantidad, "Cantidad");
            Positivo(costoUnitario, "Costo unitario");
            string refe = string.IsNullOrWhiteSpace(referencia) ? "S/F" : referencia.Trim();

            RegistrarEntrada(m, TipoMovimiento.Compra, "Compra", refe, cantidad, costoUnitario, fecha);
            Auditar("Materia Prima", "Compra",
                string.Format("{0}: {1:N2} {2} a {3:C4} ({4})", m.Codigo, cantidad, m.Unidad, costoUnitario, refe));
            Notificar();
        }

        /// <summary>
        /// Salida de materia prima a producción. Valida existencias, valúa a costo promedio
        /// y carga el importe como MPD a la orden correspondiente.
        /// </summary>
        public decimal SalidaAProduccion(string codigo, string folio, decimal cantidad, DateTime fecha)
        {
            var m = MaterialExistente(codigo);
            var orden = OrdenActiva(folio);
            Positivo(cantidad, "Cantidad");
            if (cantidad > m.Existencia)
                throw new ReglaNegocioException(string.Format(
                    "Existencia insuficiente de {0}.\nDisponible: {1:N2} {2}\nSolicitado: {3:N2} {2}",
                    m.Nombre, m.Existencia, m.Unidad, cantidad));

            decimal costoUnitario = m.CostoPromedio;
            // Si se agota la existencia se toma el saldo exacto para no dejar centavos huérfanos.
            decimal importe = cantidad == m.Existencia ? m.SaldoImporte : R2(cantidad * costoUnitario);

            m.Existencia -= cantidad;
            m.SaldoImporte = m.Existencia == 0 ? 0m : R2(m.SaldoImporte - importe);
            if (m.Existencia == 0) m.CostoPromedio = 0m;

            var mov = new MovimientoKardex
            {
                Id = NuevoId(), Fecha = fecha, CodigoMaterial = m.Codigo,
                Tipo = TipoMovimiento.SalidaProduccion, Concepto = "Salida a producción",
                Referencia = orden.Folio, Folio = orden.Folio,
                SalidaCantidad = cantidad, SalidaCostoUnitario = costoUnitario, SalidaImporte = importe,
                SaldoCantidad = m.Existencia, CostoPromedio = m.CostoPromedio, SaldoImporte = m.SaldoImporte
            };
            Datos.MovimientosMP.Add(mov);

            AgregarCargo(orden, ElementoCosto.MPD, m.Nombre, "VS-" + mov.Id, cantidad, importe, fecha);
            Auditar("Materia Prima", "Salida a producción",
                string.Format("{0}: {1:N2} {2} a {3} por {4:C2}", m.Codigo, cantidad, m.Unidad, orden.Folio, importe));
            Notificar();
            return importe;
        }

        private void RegistrarEntrada(MateriaPrima m, TipoMovimiento tipo, string concepto, string referencia,
            decimal cantidad, decimal costoUnitario, DateTime fecha)
        {
            decimal importe = R2(cantidad * costoUnitario);
            m.Existencia += cantidad;
            m.SaldoImporte = R2(m.SaldoImporte + importe);
            m.CostoPromedio = m.Existencia > 0 ? R4(m.SaldoImporte / m.Existencia) : 0m;

            Datos.MovimientosMP.Add(new MovimientoKardex
            {
                Id = NuevoId(), Fecha = fecha, CodigoMaterial = m.Codigo, Tipo = tipo,
                Concepto = concepto, Referencia = referencia,
                EntradaCantidad = cantidad, EntradaCostoUnitario = costoUnitario, EntradaImporte = importe,
                SaldoCantidad = m.Existencia, CostoPromedio = m.CostoPromedio, SaldoImporte = m.SaldoImporte
            });
        }

        // ======================================================================
        //  MÓDULO 4: ÓRDENES DE PRODUCCIÓN
        // ======================================================================

        /// <summary>Crea una orden con folio consecutivo automático (OP-1001…).</summary>
        public string CrearOrden(string producto, string cliente, int cantidad, DateTime fecha)
        {
            producto = Requerido(producto, "Producto");
            cliente = string.IsNullOrWhiteSpace(cliente) ? "Inventario propio" : cliente.Trim();
            if (cantidad <= 0) throw new ReglaNegocioException("La cantidad a producir debe ser mayor que cero.");

            string folio = "OP-" + Datos.SiguienteFolio;
            Datos.SiguienteFolio++;
            Datos.Ordenes.Add(new OrdenProduccion
            {
                Folio = folio, Producto = producto, Cliente = cliente, Cantidad = cantidad,
                FechaAlta = fecha, Estado = EstadoOrden.Abierta
            });
            Auditar("Órdenes", "Alta de orden", string.Format("{0}: {1} x {2} para {3}", folio, cantidad, producto, cliente));
            Notificar();
            return folio;
        }

        /// <summary>Cancela una orden únicamente si no tiene costos cargados.</summary>
        public void CancelarOrden(string folio)
        {
            var o = OrdenActiva(folio);
            if (Datos.Cargos.Any(c => c.Folio == o.Folio))
                throw new ReglaNegocioException("La orden " + o.Folio + " ya tiene costos cargados y no puede cancelarse.");
            o.Estado = EstadoOrden.Cancelada;
            Auditar("Órdenes", "Cancelación", o.Folio);
            Notificar();
        }

        /// <summary>
        /// Termina la orden y transfiere su costo total al almacén de artículos terminados.
        /// </summary>
        public void TerminarOrden(string folio, DateTime fecha)
        {
            var o = OrdenActiva(folio);
            if (o.MPD <= 0)
                throw new ReglaNegocioException("La orden " + o.Folio + " no tiene materia prima cargada.");
            if (o.CostoTotal <= 0)
                throw new ReglaNegocioException("La orden " + o.Folio + " no tiene costos acumulados.");

            o.Estado = EstadoOrden.Terminada;
            o.FechaTermino = fecha;

            var pt = BuscarProducto(o.Producto);
            if (pt == null)
            {
                pt = new ProductoTerminado { Producto = o.Producto };
                Datos.Productos.Add(pt);
            }
            pt.Existencia += o.Cantidad;
            pt.SaldoImporte = R2(pt.SaldoImporte + o.CostoTotal);
            pt.CostoPromedio = pt.Existencia > 0 ? R4(pt.SaldoImporte / pt.Existencia) : 0m;

            Datos.MovimientosPT.Add(new MovimientoPT
            {
                Id = NuevoId(), Fecha = fecha, Producto = pt.Producto,
                Concepto = "Entrada de producción", Referencia = o.Folio,
                EntradaCantidad = o.Cantidad, EntradaImporte = o.CostoTotal,
                SaldoCantidad = pt.Existencia, CostoPromedio = pt.CostoPromedio, SaldoImporte = pt.SaldoImporte
            });
            Auditar("Artículos Terminados", "Terminación de orden",
                string.Format("{0}: {1} unidades, CT {2:C2}, CU {3:C4}", o.Folio, o.Cantidad, o.CostoTotal, o.CostoUnitario));
            Notificar();
        }

        private void AgregarCargo(OrdenProduccion o, ElementoCosto elemento, string concepto, string referencia,
            decimal cantidad, decimal importe, DateTime fecha)
        {
            Datos.Cargos.Add(new CargoOrden
            {
                Id = NuevoId(), Fecha = fecha, Folio = o.Folio, Elemento = elemento,
                Concepto = concepto, Referencia = referencia, Cantidad = cantidad, Importe = importe
            });
            switch (elemento)
            {
                case ElementoCosto.MPD: o.MPD = R2(o.MPD + importe); break;
                case ElementoCosto.MOD: o.MOD = R2(o.MOD + importe); break;
                default: o.GIF = R2(o.GIF + importe); break;
            }
            if (o.Estado == EstadoOrden.Abierta) o.Estado = EstadoOrden.EnProceso;
        }

        // ======================================================================
        //  MÓDULO 5: MANO DE OBRA
        // ======================================================================

        /// <summary>Registra nómina de un operario y la carga como MOD a la orden.</summary>
        public decimal RegistrarNomina(string folio, string operario, string puesto, decimal horas, decimal tarifaHora, DateTime fecha)
        {
            var o = OrdenActiva(folio);
            operario = Requerido(operario, "Operario");
            puesto = string.IsNullOrWhiteSpace(puesto) ? "Operario" : puesto.Trim();
            Positivo(horas, "Horas trabajadas");
            Positivo(tarifaHora, "Tarifa por hora");
            if (horas > 744) throw new ReglaNegocioException("Las horas capturadas exceden las de un mes (744).");

            decimal importe = R2(horas * tarifaHora);
            var reg = new RegistroNomina
            {
                Id = NuevoId(), Fecha = fecha, Folio = o.Folio, Operario = operario, Puesto = puesto,
                Horas = horas, TarifaHora = tarifaHora, Importe = importe
            };
            Datos.Nomina.Add(reg);
            o.HorasMOD += horas;
            AgregarCargo(o, ElementoCosto.MOD, "Nómina " + operario, "NOM-" + reg.Id, horas, importe, fecha);
            Auditar("Mano de Obra", "Registro de nómina",
                string.Format("{0}: {1} {2:N2} h x {3:C2} = {4:C2}", o.Folio, operario, horas, tarifaHora, importe));
            Notificar();
            return importe;
        }

        // ======================================================================
        //  MÓDULO 6: GASTOS INDIRECTOS DE FABRICACIÓN
        // ======================================================================

        public void RegistrarGasto(string concepto, string categoria, decimal importe, DateTime fecha)
        {
            concepto = Requerido(concepto, "Concepto");
            categoria = Requerido(categoria, "Categoría");
            Positivo(importe, "Importe");
            Datos.Gastos.Add(new GastoIndirecto
            {
                Id = NuevoId(), Fecha = fecha, Concepto = concepto, Categoria = categoria, Importe = R2(importe)
            });
            Auditar("GIF", "Registro de gasto", string.Format("{0} ({1}) {2:C2}", concepto, categoria, importe));
            Notificar();
        }

        public void EliminarGasto(int id)
        {
            var g = Datos.Gastos.FirstOrDefault(x => x.Id == id);
            if (g == null) throw new ReglaNegocioException("Seleccione un gasto.");
            if (g.Prorrateado)
                throw new ReglaNegocioException("El gasto ya fue prorrateado (lote " + g.Lote + ") y no puede eliminarse.");
            Datos.Gastos.Remove(g);
            Auditar("GIF", "Eliminación de gasto", string.Format("{0} {1:C2}", g.Concepto, g.Importe));
            Notificar();
        }

        public static decimal ValorBase(OrdenProduccion o, BaseProrrateo b)
        {
            switch (b)
            {
                case BaseProrrateo.HorasMOD: return o.HorasMOD;
                case BaseProrrateo.CostoMOD: return o.MOD;
                default: return o.MPD;
            }
        }

        /// <summary>
        /// Calcula (sin aplicar) la tasa: Tasa = Σ GIF pendientes / Σ base de órdenes activas.
        /// El último renglón absorbe la diferencia por redondeo para que la suma cuadre al centavo.
        /// </summary>
        public ResultadoProrrateo CalcularProrrateo(BaseProrrateo b)
        {
            var r = new ResultadoProrrateo { Base = b, TotalGIF = GifPendiente() };
            var ordenes = OrdenesActivas().Where(o => ValorBase(o, b) > 0).ToList();
            r.TotalBase = ordenes.Sum(o => ValorBase(o, b));
            r.Tasa = r.TotalBase > 0 ? Math.Round(r.TotalGIF / r.TotalBase, 6) : 0m;

            decimal acumulado = 0m;
            for (int i = 0; i < ordenes.Count; i++)
            {
                var o = ordenes[i];
                decimal valor = ValorBase(o, b);
                decimal importe = (i == ordenes.Count - 1)
                    ? R2(r.TotalGIF - acumulado)
                    : (r.TotalBase > 0 ? R2(valor * r.TotalGIF / r.TotalBase) : 0m);
                acumulado += importe;
                r.Detalle.Add(new DetalleProrrateo { Folio = o.Folio, Producto = o.Producto, ValorBase = valor, Importe = importe });
            }
            return r;
        }

        /// <summary>Aplica el prorrateo: carga GIF a cada orden y marca los gastos como aplicados.</summary>
        public ResultadoProrrateo AplicarProrrateo(BaseProrrateo b, DateTime fecha)
        {
            var r = CalcularProrrateo(b);
            if (r.TotalGIF <= 0)
                throw new ReglaNegocioException("No hay gastos indirectos pendientes de prorratear.");
            if (r.Detalle.Count == 0)
                throw new ReglaNegocioException("No hay órdenes activas con " + Textos.Base(b).ToLower() +
                    " mayor a cero. Registre primero los costos de la base elegida.");

            int lote = Datos.Prorrateos.Count == 0 ? 1 : Datos.Prorrateos.Max(p => p.Lote) + 1;
            foreach (var d in r.Detalle)
            {
                var o = BuscarOrden(d.Folio);
                AgregarCargo(o, ElementoCosto.GIF,
                    string.Format("GIF aplicados (tasa {0:N6})", r.Tasa), "PRO-" + lote, d.ValorBase, d.Importe, fecha);
            }
            foreach (var g in Datos.Gastos.Where(x => !x.Prorrateado))
            {
                g.Prorrateado = true;
                g.Lote = lote;
            }
            Datos.Prorrateos.Add(new ProrrateoAplicado
            {
                Lote = lote, Fecha = fecha, Base = b, TotalGIF = r.TotalGIF,
                TotalBase = r.TotalBase, Tasa = r.Tasa, OrdenesAfectadas = r.Detalle.Count
            });
            Auditar("GIF", "Prorrateo aplicado",
                string.Format("Lote {0}: {1:C2} sobre {2} (tasa {3:N6}) a {4} órdenes", lote, r.TotalGIF, Textos.Base(b), r.Tasa, r.Detalle.Count));
            Notificar();
            return r;
        }

        // ======================================================================
        //  MÓDULO 3: ARTÍCULOS TERMINADOS (salidas por venta)
        // ======================================================================

        public decimal VenderProductoTerminado(string producto, decimal cantidad, string referencia, DateTime fecha)
        {
            var pt = BuscarProducto(producto);
            if (pt == null) throw new ReglaNegocioException("Seleccione un artículo terminado.");
            Positivo(cantidad, "Cantidad");
            if (cantidad > pt.Existencia)
                throw new ReglaNegocioException(string.Format(
                    "Existencia insuficiente de {0}.\nDisponible: {1:N0}\nSolicitado: {2:N0}", pt.Producto, pt.Existencia, cantidad));

            decimal costoUnitario = pt.CostoPromedio;
            decimal importe = cantidad == pt.Existencia ? pt.SaldoImporte : R2(cantidad * costoUnitario);
            pt.Existencia -= cantidad;
            pt.SaldoImporte = pt.Existencia == 0 ? 0m : R2(pt.SaldoImporte - importe);
            if (pt.Existencia == 0) pt.CostoPromedio = 0m;

            string refe = string.IsNullOrWhiteSpace(referencia) ? "S/R" : referencia.Trim();
            Datos.MovimientosPT.Add(new MovimientoPT
            {
                Id = NuevoId(), Fecha = fecha, Producto = pt.Producto, Concepto = "Salida por venta", Referencia = refe,
                SalidaCantidad = cantidad, SalidaImporte = importe,
                SaldoCantidad = pt.Existencia, CostoPromedio = pt.CostoPromedio, SaldoImporte = pt.SaldoImporte
            });
            Auditar("Artículos Terminados", "Salida por venta",
                string.Format("{0}: {1:N0} u. costo {2:C2} ({3})", pt.Producto, cantidad, importe, refe));
            Notificar();
            return importe;
        }

        // ======================================================================
        //  MÓDULO 7 y 8: RESULTADOS Y REPORTES
        // ======================================================================

        /// <summary>Estado de costo de producción y de lo vendido.</summary>
        public List<LineaReporte> EstadoDeCosto()
        {
            var d = Datos;
            var ordenes = d.Ordenes.Where(o => o.Estado != EstadoOrden.Cancelada).ToList();

            decimal invInicialMP = d.MovimientosMP.Where(m => m.Tipo == TipoMovimiento.InventarioInicial).Sum(m => m.EntradaImporte);
            decimal compras = d.MovimientosMP.Where(m => m.Tipo == TipoMovimiento.Compra).Sum(m => m.EntradaImporte);
            decimal disponible = invInicialMP + compras;
            decimal invFinalMP = d.Materiales.Sum(m => m.SaldoImporte);
            decimal mpUtilizada = disponible - invFinalMP;
            decimal mod = ordenes.Sum(o => o.MOD);
            decimal gif = ordenes.Sum(o => o.GIF);
            decimal costoProduccion = mpUtilizada + mod + gif;
            decimal invFinalPEP = ordenes.Where(o => o.EstaActiva).Sum(o => o.CostoTotal);
            decimal costoTerminados = costoProduccion - invFinalPEP;
            decimal invFinalPT = d.Productos.Sum(p => p.SaldoImporte);
            decimal costoVendido = costoTerminados - invFinalPT;

            return new List<LineaReporte>
            {
                new LineaReporte("Inventario inicial de materia prima", invInicialMP, null, false),
                new LineaReporte("(+) Compras de materia prima", compras, null, false),
                new LineaReporte("(=) Materia prima disponible", null, disponible, true),
                new LineaReporte("(−) Inventario final de materia prima", invFinalMP, null, false),
                new LineaReporte("(=) Materia prima directa utilizada", null, mpUtilizada, true),
                new LineaReporte("(+) Mano de obra directa", null, mod, false),
                new LineaReporte("(+) Gastos indirectos de fabricación aplicados", null, gif, false),
                new LineaReporte("(=) COSTO DE PRODUCCIÓN DEL PERIODO", null, costoProduccion, true),
                new LineaReporte("(+) Inventario inicial de producción en proceso", null, 0m, false),
                new LineaReporte("(−) Inventario final de producción en proceso", null, invFinalPEP, false),
                new LineaReporte("(=) COSTO DE ARTÍCULOS TERMINADOS", null, costoTerminados, true),
                new LineaReporte("(+) Inventario inicial de artículos terminados", null, 0m, false),
                new LineaReporte("(−) Inventario final de artículos terminados", null, invFinalPT, false),
                new LineaReporte("(=) COSTO DE LO VENDIDO", null, costoVendido, true),
                new LineaReporte("GIF reales pendientes de aplicar (informativo)", GifPendiente(), null, false)
            };
        }

        // ======================================================================
        //  DATOS DE DEMOSTRACIÓN
        // ======================================================================

        public void CargarDemo()
        {
            suspendido = true;
            try
            {
                Datos = new DatosSistema();
                DateTime f = DateTime.Today.AddDays(-20);

                AltaMaterial("MP-001", "Tela de mezclilla", "m", 80, 400, 38.50m, f);
                AltaMaterial("MP-002", "Hilo industrial", "cono", 10, 60, 24.00m, f);
                AltaMaterial("MP-003", "Botones metálicos", "pza", 300, 2000, 1.15m, f);
                AltaMaterial("MP-004", "Cierre de 20 cm", "pza", 100, 500, 6.80m, f);
                RegistrarCompra("MP-001", 250, 41.00m, "FAC-A1203", f.AddDays(2));
                RegistrarCompra("MP-003", 1000, 1.25m, "FAC-B0457", f.AddDays(3));

                string op1 = CrearOrden("Chamarra de mezclilla", "Distribuidora del Norte", 120, f.AddDays(4));
                string op2 = CrearOrden("Pantalón de trabajo", "Industrias Monclova", 200, f.AddDays(5));
                string op3 = CrearOrden("Camisa industrial", "Aceros del Bajío", 150, f.AddDays(8));

                SalidaAProduccion("MP-001", op1, 180, f.AddDays(5));
                SalidaAProduccion("MP-002", op1, 8, f.AddDays(5));
                SalidaAProduccion("MP-003", op1, 720, f.AddDays(5));
                SalidaAProduccion("MP-004", op1, 120, f.AddDays(5));
                SalidaAProduccion("MP-001", op2, 260, f.AddDays(6));
                SalidaAProduccion("MP-002", op2, 12, f.AddDays(6));
                SalidaAProduccion("MP-003", op2, 400, f.AddDays(6));
                SalidaAProduccion("MP-004", op2, 200, f.AddDays(6));

                RegistrarNomina(op1, "Juan Pérez", "Costurero", 64, 85m, f.AddDays(7));
                RegistrarNomina(op1, "María López", "Cortadora", 40, 90m, f.AddDays(7));
                RegistrarNomina(op2, "Juan Pérez", "Costurero", 72, 85m, f.AddDays(9));
                RegistrarNomina(op2, "Luis Ramírez", "Planchador", 48, 78m, f.AddDays(9));

                RegistrarGasto("Lubricantes y agujas", "Materia prima indirecta", 3200m, f.AddDays(10));
                RegistrarGasto("Sueldo de supervisor de planta", "Sueldos de supervisores", 18000m, f.AddDays(10));
                RegistrarGasto("Recibo de energía eléctrica", "Energía eléctrica (luz)", 6400m, f.AddDays(10));
                RegistrarGasto("Depreciación de maquinaria", "Depreciación", 5000m, f.AddDays(10));
                AplicarProrrateo(BaseProrrateo.HorasMOD, f.AddDays(12));

                TerminarOrden(op1, f.AddDays(14));
                VenderProductoTerminado("Chamarra de mezclilla", 80, "FAC-V0091", f.AddDays(16));

                SalidaAProduccion("MP-001", op3, 150, f.AddDays(15));
                SalidaAProduccion("MP-003", op3, 900, f.AddDays(15));
                RegistrarNomina(op3, "María López", "Cortadora", 30, 90m, f.AddDays(17));
                RegistrarGasto("Mantenimiento de máquinas de coser", "Mantenimiento", 2400m, f.AddDays(18));

                Datos.Bitacora.Clear();
                Auditar("Sistema", "Datos de demostración", "Se cargaron datos de ejemplo.");
            }
            finally
            {
                suspendido = false;
            }
            Notificar();
        }

        // ======================================================================
        //  UTILERÍAS
        // ======================================================================

        private MateriaPrima MaterialExistente(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) throw new ReglaNegocioException("Seleccione un material.");
            var m = BuscarMaterial(codigo);
            if (m == null) throw new ReglaNegocioException("El material " + codigo + " no existe.");
            return m;
        }

        private OrdenProduccion OrdenActiva(string folio)
        {
            if (string.IsNullOrWhiteSpace(folio)) throw new ReglaNegocioException("Seleccione una orden de producción.");
            var o = BuscarOrden(folio);
            if (o == null) throw new ReglaNegocioException("La orden " + folio + " no existe.");
            if (!o.EstaActiva)
                throw new ReglaNegocioException("La orden " + o.Folio + " está " + Textos.Estado(o.Estado).ToLower() +
                    " y ya no admite movimientos.");
            return o;
        }

        private static string Requerido(string valor, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor)) throw new ReglaNegocioException("El campo «" + campo + "» es obligatorio.");
            return valor.Trim();
        }

        private static void Positivo(decimal valor, string campo)
        {
            if (valor <= 0) throw new ReglaNegocioException("El campo «" + campo + "» debe ser mayor que cero.");
        }

        private int NuevoId() { return Datos.SiguienteId++; }
        private static decimal R2(decimal v) { return Math.Round(v, 2, MidpointRounding.AwayFromZero); }
        private static decimal R4(decimal v) { return Math.Round(v, 4, MidpointRounding.AwayFromZero); }

        private void Auditar(string modulo, string accion, string detalle)
        {
            Datos.Bitacora.Add(new RegistroAuditoria { Fecha = DateTime.Now, Modulo = modulo, Accion = accion, Detalle = detalle });
        }

        private void Notificar()
        {
            if (suspendido) return;
            var h = DatosCambiados;
            if (h != null) h(this, EventArgs.Empty);
        }
    }
}
