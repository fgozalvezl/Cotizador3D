# Decisiones del rebuild

Referencias a bugs: `docs/SPEC-legacy.md` seccion 6.

## Formulas

Se replican EXACTAMENTE las formulas de `SPEC-legacy.md` seccion 2.4 (sin
redondeo intermedio, `double`). Ningun cambio de negocio.

## PDF para el cliente (funcionalidad nueva)

- El PDF NUNCA muestra el margen de ganancia: ni la linea "precio de venta", ni
  el multiplicador, ni la palabra "ganancia".
- Para que el desglose sea coherente con lo que paga el cliente, cada linea de
  costo (material, energia, IVA energia, desgaste, margen de error) se muestra
  multiplicada por `margen_ganancia`. Asi la suma de las lineas es exactamente
  `precio_venta`, se agrega el envio como linea aparte (solo si > 0) y el total
  es `precio_final_con_envio`. El cliente no puede deducir la ganancia por
  resta.
- Redondeo: cada linea se redondea a 2 decimales; el total es
  `round(precio_final_con_envio)`, el MISMO importe que muestra la ventana
  principal. La diferencia del redondeo la absorbe la linea de costo MAS GRANDE
  de las que se muestran, para que las lineas impresas sumen exactamente el
  total impreso sin que ninguna quede negativa (si el residuo negativo no entra
  entero en esa linea, sigue por la siguiente mas grande). La linea "Margen de
  error" se muestra solo si `margen_error_pct > 0` (misma regla que el envio);
  con 0% no aparece.
  Valores que redondean a 0 se muestran como `$ 0,00` (nunca `-0,00`).
- El desglose interno (con costo real y ganancia) sigue visible solo en la UI.
- El PDF incluye: nombre del negocio (opcional, configurable), fecha, nombre
  del cliente y del trabajo (opcionales, se piden al exportar), filamento
  (marca y tipo), gramos, tiempo de impresion, tabla de desglose y total.
  Textos en espanol. No hay "cantidad de piezas": la cotizacion es por trabajo,
  como en la app original.
- Contrato: `Cotizador3D.Core.Export.QuotePdfExporter.Exportar(QuoteResult,
  ClientQuoteInfo, string rutaSalida)` con `ClientQuoteInfo { NombreNegocio,
  Cliente, Trabajo, Filamento, Gramos, HorasImpresion, Fecha }`.

## Bugs del legado: que se corrige

| Bug | Decision |
| --- | --- |
| B1, B2 | Corregir: carga tolerante, cualquier clave faltante toma el default. |
| B3, B10 | Corregir: errores de validacion como avisos en la UI, no como excepciones. |
| B4 | Corregir: rotulo consistente "PRECIO FINAL". |
| B5 | Corregir: al fallar un calculo se limpian los resultados. |
| B6 | Corregir: cada filamento tiene un `id` (string; se conservan los ids legados, los faltantes reciben un GUID) y la seleccion usa el id, no el string. Se conserva `brand (type)` como texto visible. |
| B7 | Mantener el comportamiento (desgaste 0 si vida util <= 0) pero mostrar un aviso. |
| B8 | Corregir: se guarda lo valido; la geometria se guarda siempre. |
| B9 | Corregir: formato es-AR (`$ 1.234,56`) en UI y PDF, dos decimales. |
| B11, B21 | Corregir: escritura atomica (archivo temporal + rename), copia previa en `config_impresion3d.json.bak` (solo en el primer guardado de cada sesion, para que la copia conserve el estado con el que arranco la app y el autoguardado no la pise) y error visible al usuario (modal si ocurre al cerrar). Si el archivo existe pero no se puede leer o esta corrupto, la app arranca con defaults, avisa y BLOQUEA el guardado hasta reabrir, para no pisar la configuracion real. |
| B12 | Corregir. |
| B13 | Compatibilidad: leer numeros como string o numero; escribir siempre string para que la app Python vieja siga pudiendo leer el archivo. |
| B14, B15 | Corregir: IVA de luz editable en la UI y la etiqueta se recalcula. |
| B16, B17 | Corregir: combos no editables. |
| B18 | Corregir: valores negativos rechazados con mensaje claro. |
| B19 | Corregir: el directorio se crea solo al guardar. |
| B20 | Mantener: gramos y tiempo no se persisten (son por trabajo). |

Los textos de error se reescriben en espanol claro, sin reproducir las faltas
de ortografia del original.

Campos vacios: como en el legado, un campo de parametro vacio vale 0 para el
calculo; al autoguardar, ganancia vacia se guarda como 1.5 y el resto como 0.

## Compatibilidad del JSON

Misma ruta y mismo esquema: settings como strings, `price_kg` como numero,
igual que escribia la app Python. Claves nuevas en `settings`: `nombre_negocio`
(string, default ""). Los filamentos sin `id` reciben un GUID al cargar; los
ids existentes se conservan. Las claves desconocidas del archivo se conservan
al guardar. Todo lo demas intacto.
