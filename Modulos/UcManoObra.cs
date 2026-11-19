using System;
using System.Linq;
using System.Windows.Forms;
using CostosOP.UI;

namespace CostosOP.Modulos
{
    /// <summary>
    /// MÓDULO 5 — Mano de Obra Directa: registro de nómina y horas de operarios
    /// vinculadas a cada orden (cada pago se carga como MOD).
    /// </summary>
    public class UcManoObra : ModuloBase
    {
        private readonly TarjetaKpi kpiNomina, kpiHoras, kpiTarifa, kpiOperarios;
        private readonly DataGridView gridNomina, gridResumen;
        private ComboBox cbOrden, cbOperario, cbPuesto;
        private NumericUpDown numHoras, numTarifa;
        private DateTimePicker dtpFecha;
        private Label lblImporte;

        public UcManoObra()
            : base("Mano de Obra Directa", "Nómina de operarios por orden · Importe = horas × tarifa por hora")
        {
            kpiNomina = AgregarKpi("MOD aplicada", Tema.Verde);
            kpiHoras = AgregarKpi("Horas de MOD", Tema.Azul);
            kpiTarifa = AgregarKpi("Tarifa promedio", Tema.Morado);
            kpiOperarios = AgregarKpi("Operarios", Tema.Ambar);

            var lateral = Tema.CrearPanelLateral();
            lateral.Controls.Add(ConstruirFormulario());

            gridNomina = Tema.CrearGrid();
            gridResumen = Tema.CrearGrid();
            var arriba = Tema.CrearTarjeta("Registros de nómina cargados a órdenes", gridNomina);
            var abajo = Tema.CrearTarjeta("Resumen de MOD por orden", gridResumen);
            Tema.Apilar(Cuerpo, lateral, Tema.CrearEspaciador(), Tema.DosFilas(arriba, abajo, 60f));
        }

        private PanelFormulario ConstruirFormulario()
        {
            var f = new PanelFormulario();
            f.Seccion("Registro de nómina", "Indica el pago a operarios por cada orden en la que trabajaron.");
            cbOrden = f.Campo("Orden de producción", Tema.CrearCombo());
            cbOperario = f.Campo("Operario", Tema.CrearCombo(true));
            cbPuesto = f.Campo("Puesto", Tema.CrearCombo(true));
            cbPuesto.Items.AddRange(new object[] { "Operario", "Costurero", "Cortador", "Ensamblador", "Soldador", "Planchador", "Tornero" });
            numHoras = f.Campo("Horas trabajadas", Tema.CrearNumero(2, 744m));
            numTarifa = f.Campo("Tarifa por hora", Tema.CrearNumero(2));
            dtpFecha = f.Campo("Fecha de pago", Tema.CrearFecha());
            lblImporte = f.Indicador("Importe MOD: $0.00", Tema.Verde);

            numHoras.ValueChanged += (s, e) => ActualizarImporte();
            numTarifa.ValueChanged += (s, e) => ActualizarImporte();
            // Al elegir un operario conocido se sugieren su puesto y última tarifa.
            cbOperario.SelectedIndexChanged += (s, e) =>
            {
                var ultimo = Repo.Datos.Nomina.Where(n => n.Operario == cbOperario.Text)
                    .OrderByDescending(n => n.Id).FirstOrDefault();
                if (ultimo == null) return;
                cbPuesto.Text = ultimo.Puesto;
                numTarifa.Value = Math.Min(numTarifa.Maximum, ultimo.TarifaHora);
            };

            var btn = f.Boton("Registrar nómina", Tema.Verde);
            btn.Click += (s, e) => Ejecutar(() =>
            {
                string folio = Tema.ValorCombo(cbOrden);
                decimal importe = Repo.RegistrarNomina(folio, cbOperario.Text, cbPuesto.Text,
                    numHoras.Value, numTarifa.Value, dtpFecha.Value.Date);
                numHoras.Value = 0;
                return string.Format("Nómina de {0} cargada a {1}: {2:C2} de MOD.", cbOperario.Text.Trim(), folio, importe);
            });
            return f;
        }

        private void ActualizarImporte()
        {
            lblImporte.Text = "Importe MOD: " + (numHoras.Value * numTarifa.Value).ToString("C2");
        }

        public override void Refrescar()
        {
            var nomina = Repo.Datos.Nomina;
            decimal total = nomina.Sum(n => n.Importe);
            decimal horas = nomina.Sum(n => n.Horas);

            kpiNomina.Valor = total.ToString("C2");
            kpiNomina.Detalle = nomina.Count + " registros";
            kpiHoras.Valor = horas.ToString("N2");
            kpiHoras.Detalle = "Base para prorrateo de GIF";
            kpiTarifa.Valor = (horas > 0 ? total / horas : 0m).ToString("C2");
            kpiTarifa.Detalle = "Por hora (ponderada)";
            kpiOperarios.Valor = nomina.Select(n => n.Operario).Distinct().Count().ToString("N0");
            kpiOperarios.Detalle = "Con pagos registrados";

            Tema.LlenarCombo(cbOrden, Repo.OrdenesActivas().Select(o => new ItemCombo(o.Folio, o.Folio + " · " + o.Producto)));

            string texto = cbOperario.Text;
            cbOperario.Items.Clear();
            foreach (var op in nomina.Select(n => n.Operario).Distinct().OrderBy(x => x)) cbOperario.Items.Add(op);
            cbOperario.Text = texto;

            gridNomina.DataSource = nomina.OrderByDescending(n => n.Fecha).ThenByDescending(n => n.Id).Select(n => new
            {
                Fecha = n.Fecha,
                Folio = n.Folio,
                Operario = n.Operario,
                Puesto = n.Puesto,
                Horas = n.Horas,
                TarifaPorHora = n.TarifaHora,
                Importe = n.Importe
            }).ToList();

            gridResumen.DataSource = Repo.Datos.Ordenes.Where(o => o.HorasMOD > 0 || o.MOD > 0)
                .OrderBy(o => o.Folio).Select(o => new
                {
                    Folio = o.Folio,
                    Producto = o.Producto,
                    Estado = CostosOP.Modelos.Textos.Estado(o.Estado),
                    Horas = o.HorasMOD,
                    MOD = o.MOD,
                    CostoPorHora = o.HorasMOD > 0 ? Math.Round(o.MOD / o.HorasMOD, 2) : 0m
                }).ToList();

            ActualizarImporte();
        }
    }
}
