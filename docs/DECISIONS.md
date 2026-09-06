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
| B11, B21 | Corregir: escritura atomica (archivo temporal + rename) y error visible al usuario. |
| B12 | Corregir. |
| B13 | Compatibilidad: leer numeros como string o numero; escribir siempre string para que la app Python vieja siga pudiendo leer el archivo. |
| B14, B15 | Corregir: IVA de luz editable en la UI y la etiqueta se recalcula. |
| B16, B17 | Corregir: combos no editables. |
| B18 | Corregir: valores negativos rechazados con mensaje claro. |
| B19 | Corregir: el directorio se crea solo al guardar. |
| B20 | Mantener: gramos y tiempo no se persisten (son por trabajo). |

Los textos de error se reescriben en espanol claro, sin reproducir las faltas
de ortografia del original.

## Compatibilidad del JSON

Misma ruta y mismo esquema: settings como strings, `price_kg` como numero,
igual que escribia la app Python. Claves nuevas en `settings`: `nombre_negocio`
(string, default ""). Los filamentos sin `id` reciben un GUID al cargar; los
ids existentes se conservan. Todo lo demas intacto.
