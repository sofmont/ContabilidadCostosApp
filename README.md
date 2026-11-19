# Sistema de Contabilidad de Costos por Órdenes de Producción

Aplicación de escritorio en **C# / Windows Forms (.NET 8)** con tema oscuro, barra de navegación superior fija y 8 módulos.

## Cómo abrir y compilar
1. Descomprime la carpeta `CostosOP`.
2. En Visual Studio 2022: **Archivo → Abrir → Proyecto o solución** y elige `CostosOP.csproj`.
3. Presiona **F5**. (Requiere la carga de trabajo "Desarrollo de escritorio de .NET").
   - Desde consola: `dotnet run` dentro de la carpeta.
   - ¿Usas .NET Framework 4.8? Cambia `<TargetFramework>` a `net48`; el código es compatible con C# 7.3.

La primera ejecución carga **datos de demostración** para que veas el flujo completo. Desde *Reportes* puedes recargarlos o iniciar en blanco.
Los datos se guardan automáticamente en `%AppData%\CostosOP\datos_costos.xml`.

## Estructura
```
CostosOP/
├─ Program.cs                  Punto de entrada (cultura es-MX)
├─ FrmPrincipal.cs             Contenedor: barra superior, navegación, barra de estado
├─ Modelos/Entidades.cs        Entidades y enumeraciones
├─ Datos/Repositorio.cs        Reglas de negocio, cálculos, persistencia XML, bitácora
├─ Datos/GeneradorTablas.cs    Tablas de cada módulo (pantalla de Reportes y Excel)
├─ Datos/ExportadorExcel.cs    Genera .xlsx sin librerías externas
├─ UI/Tema.cs                  Paleta oscura y fábricas de controles/grids
├─ UI/Controles.cs             Tarjeta KPI, panel de captura, botón de navegación, gráfica
├─ UI/ModuloBase.cs            Base de los módulos (encabezado, KPI, manejo de errores)
├─ UI/FrmExportar.cs           Ventana para elegir qué módulos exportar a Excel
└─ Modulos/                    Un UserControl por módulo
   ├─ UcMateriaPrima.cs        1. Alta, inventario inicial, compras, salidas a OP
   ├─ UcKardex.cs              2. Tarjetas de almacén (costo promedio)
   ├─ UcArticulosTerminados.cs 3. Recepción de órdenes terminadas y ventas
   ├─ UcOrdenes.cs             4. Órdenes y hoja de costos (MPD/MOD/GIF)
   ├─ UcManoObra.cs            5. Nómina por orden
   ├─ UcGastosIndirectos.cs    6. GIF y prorrateo con tasa de aplicación
   ├─ UcResultados.cs          7. CT y CU por orden, gráfica
   └─ UcReportes.cs            8. Estado de costo, hojas de costos, kardex, bitácora, CSV
```

## Flujo de trabajo
1. **Materia Prima** → alta del material (con inventario inicial) y compras.
2. **Órdenes** → crear la orden (folio OP-1001, OP-1002…).
3. **Materia Prima → Salida a OP** → la MP se valúa a costo promedio y se carga como **MPD**.
4. **Mano de Obra** → horas × tarifa se cargan como **MOD**.
5. **GIF** → capturar gastos; *Aplicar prorrateo*: Tasa = Σ GIF ÷ Σ base (horas MOD, costo MOD o costo MPD). Cada resultado va a su orden.
6. **Órdenes / Art. Terminados** → terminar la orden: su **CT = MPD + MOD + GIF** pasa al almacén de PT con **CU = CT ÷ unidades**.
7. **Resultados** y **Reportes** → tablero y estado de costo de producción y ventas.

## Exportar a Excel
Botón **Exportar a Excel** en la barra superior (o `Ctrl+E`). Marca los módulos que quieras; todos van en un solo `.xlsx`:

| Módulo | Hojas que genera |
|---|---|
| Materia Prima | Catálogo y existencias |
| Tarjetas de Almacén | Una hoja Kardex por material |
| Órdenes | Concentrado de órdenes + hojas de costos |
| Mano de Obra | Nómina + MOD por orden |
| GIF | Gastos + GIF por orden + prorrateos |
| Artículos Terminados | Existencias + movimientos PT |
| Resultados | CT y CU por orden con totales |
| Reportes | Estado de costo + bitácora |

En *Reportes* también puedes exportar solo el reporte que estás viendo. Las hojas llevan formato de moneda, fechas, totales en negritas y encabezado inmovilizado.
No requiere instalar paquetes NuGet (en .NET Framework 4.8 agrega la referencia `System.IO.Compression`).

## Pantallas con zoom (125 % / 150 %)
Todas las medidas se escalan según la configuración de pantalla de Windows y las tarjetas se ajustan al tamaño del texto, por lo que no se enciman.

## Reglas de negocio validadas
- No se permiten salidas mayores a la existencia (MP y PT); se muestra el disponible en vivo.
- Órdenes terminadas o canceladas no aceptan cargos; solo se cancelan órdenes sin costos.
- No se puede terminar una orden sin materia prima; aviso si quedan GIF sin prorratear.
- Gastos ya prorrateados no se pueden eliminar.
- El prorrateo cuadra al centavo (el último renglón absorbe el redondeo).
- Códigos de material únicos; campos obligatorios y montos positivos.

## Atajos
`Ctrl+1` … `Ctrl+8` cambian de módulo · `Ctrl+E` exporta a Excel · `Ctrl+S` guarda.
