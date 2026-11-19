using System;
using System.Collections.Generic;

namespace CostosOP.Modelos
{
    // ==========================================================================
    //  ENUMERACIONES
    // ==========================================================================

    /// <summary>Tipo de movimiento en la tarjeta de almacén de materia prima.</summary>
    public enum TipoMovimiento { InventarioInicial, Compra, SalidaProduccion }

    /// <summary>Ciclo de vida de una orden de producción.</summary>
    public enum EstadoOrden { Abierta, EnProceso, Terminada, Cancelada }

    /// <summary>Los tres elementos del costo de producción.</summary>
    public enum ElementoCosto { MPD, MOD, GIF }

    /// <summary>Base para calcular la tasa de aplicación de GIF.</summary>
    public enum BaseProrrateo { HorasMOD, CostoMOD, CostoMPD }

    // ==========================================================================
    //  ENTIDADES (todas con constructor vacío y propiedades públicas para XmlSerializer)
    // ==========================================================================

    /// <summary>Material del catálogo con su existencia valuada a costo promedio.</summary>
    public class MateriaPrima
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Unidad { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal Existencia { get; set; }
        public decimal CostoPromedio { get; set; }
        public decimal SaldoImporte { get; set; }

        /// <summary>Indica si la existencia está en o por debajo del mínimo.</summary>
        public bool BajoMinimo { get { return Existencia <= StockMinimo; } }
    }

    /// <summary>Renglón de la tarjeta de almacén (Kardex) de materia prima.</summary>
    public class MovimientoKardex
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string CodigoMaterial { get; set; }
        public TipoMovimiento Tipo { get; set; }
        public string Concepto { get; set; }
        public string Referencia { get; set; }
        public string Folio { get; set; }

        public decimal EntradaCantidad { get; set; }
        public decimal EntradaCostoUnitario { get; set; }
        public decimal EntradaImporte { get; set; }

        public decimal SalidaCantidad { get; set; }
        public decimal SalidaCostoUnitario { get; set; }
        public decimal SalidaImporte { get; set; }

        public decimal SaldoCantidad { get; set; }
        public decimal CostoPromedio { get; set; }
        public decimal SaldoImporte { get; set; }
    }

    /// <summary>Orden de producción con acumulación independiente de MPD, MOD y GIF.</summary>
    public class OrdenProduccion
    {
        public string Folio { get; set; }
        public string Producto { get; set; }
        public string Cliente { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime? FechaTermino { get; set; }
        public EstadoOrden Estado { get; set; }

        public decimal MPD { get; set; }
        public decimal MOD { get; set; }
        public decimal GIF { get; set; }
        public decimal HorasMOD { get; set; }

        /// <summary>CT = MPD + MOD + GIF</summary>
        public decimal CostoTotal { get { return MPD + MOD + GIF; } }

        /// <summary>CU = CT / unidades producidas.</summary>
        public decimal CostoUnitario
        {
            get { return Cantidad > 0 ? Math.Round(CostoTotal / Cantidad, 4) : 0m; }
        }

        public bool EstaActiva
        {
            get { return Estado == EstadoOrden.Abierta || Estado == EstadoOrden.EnProceso; }
        }
    }

    /// <summary>Cargo de costo aplicado a una orden (hoja de costos).</summary>
    public class CargoOrden
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Folio { get; set; }
        public ElementoCosto Elemento { get; set; }
        public string Concepto { get; set; }
        public string Referencia { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Importe { get; set; }
    }

    /// <summary>Registro de nómina de un operario cargado a una orden.</summary>
    public class RegistroNomina
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Folio { get; set; }
        public string Operario { get; set; }
        public string Puesto { get; set; }
        public decimal Horas { get; set; }
        public decimal TarifaHora { get; set; }
        public decimal Importe { get; set; }
    }

    /// <summary>Gasto indirecto de fabricación real (pendiente o ya prorrateado).</summary>
    public class GastoIndirecto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; }
        public string Categoria { get; set; }
        public decimal Importe { get; set; }
        public bool Prorrateado { get; set; }
        public int Lote { get; set; }
    }

    /// <summary>Historial de cada aplicación de GIF (lote de prorrateo).</summary>
    public class ProrrateoAplicado
    {
        public int Lote { get; set; }
        public DateTime Fecha { get; set; }
        public BaseProrrateo Base { get; set; }
        public decimal TotalGIF { get; set; }
        public decimal TotalBase { get; set; }
        public decimal Tasa { get; set; }
        public int OrdenesAfectadas { get; set; }
    }

    /// <summary>Existencia valuada de un artículo terminado.</summary>
    public class ProductoTerminado
    {
        public string Producto { get; set; }
        public decimal Existencia { get; set; }
        public decimal CostoPromedio { get; set; }
        public decimal SaldoImporte { get; set; }
    }

    /// <summary>Renglón de la tarjeta de almacén de artículos terminados.</summary>
    public class MovimientoPT
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Producto { get; set; }
        public string Concepto { get; set; }
        public string Referencia { get; set; }
        public decimal EntradaCantidad { get; set; }
        public decimal EntradaImporte { get; set; }
        public decimal SalidaCantidad { get; set; }
        public decimal SalidaImporte { get; set; }
        public decimal SaldoCantidad { get; set; }
        public decimal CostoPromedio { get; set; }
        public decimal SaldoImporte { get; set; }
    }

    /// <summary>Entrada de la bitácora de auditoría.</summary>
    public class RegistroAuditoria
    {
        public DateTime Fecha { get; set; }
        public string Modulo { get; set; }
        public string Accion { get; set; }
        public string Detalle { get; set; }
    }

    /// <summary>Contenedor raíz que se persiste en disco (XML).</summary>
    public class DatosSistema
    {
        public DatosSistema()
        {
            Materiales = new List<MateriaPrima>();
            MovimientosMP = new List<MovimientoKardex>();
            Ordenes = new List<OrdenProduccion>();
            Cargos = new List<CargoOrden>();
            Nomina = new List<RegistroNomina>();
            Gastos = new List<GastoIndirecto>();
            Prorrateos = new List<ProrrateoAplicado>();
            Productos = new List<ProductoTerminado>();
            MovimientosPT = new List<MovimientoPT>();
            Bitacora = new List<RegistroAuditoria>();
            SiguienteFolio = 1001;
            SiguienteId = 1;
        }

        public List<MateriaPrima> Materiales { get; set; }
        public List<MovimientoKardex> MovimientosMP { get; set; }
        public List<OrdenProduccion> Ordenes { get; set; }
        public List<CargoOrden> Cargos { get; set; }
        public List<RegistroNomina> Nomina { get; set; }
        public List<GastoIndirecto> Gastos { get; set; }
        public List<ProrrateoAplicado> Prorrateos { get; set; }
        public List<ProductoTerminado> Productos { get; set; }
        public List<MovimientoPT> MovimientosPT { get; set; }
        public List<RegistroAuditoria> Bitacora { get; set; }
        public int SiguienteFolio { get; set; }
        public int SiguienteId { get; set; }
    }

    /// <summary>Textos legibles para enumeraciones.</summary>
    public static class Textos
    {
        public static string Estado(EstadoOrden e)
        {
            switch (e)
            {
                case EstadoOrden.Abierta: return "Abierta";
                case EstadoOrden.EnProceso: return "En proceso";
                case EstadoOrden.Terminada: return "Terminada";
                default: return "Cancelada";
            }
        }

        public static string Base(BaseProrrateo b)
        {
            switch (b)
            {
                case BaseProrrateo.HorasMOD: return "Horas de mano de obra directa";
                case BaseProrrateo.CostoMOD: return "Costo de mano de obra directa";
                default: return "Costo de materia prima directa";
            }
        }

        public static string Movimiento(TipoMovimiento t)
        {
            switch (t)
            {
                case TipoMovimiento.InventarioInicial: return "Inventario inicial";
                case TipoMovimiento.Compra: return "Compra";
                default: return "Salida a producción";
            }
        }
    }
}
