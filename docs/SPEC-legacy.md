# SPEC-legacy — Cotizador3D (`cotizador.py`)

Especificación de comportamiento del programa legado, para reimplementarlo en
otro lenguaje **preservando exactamente** cada fórmula, texto y formato.

- Archivo fuente: `/home/user/Cotizador3D/cotizador.py` (489 líneas).
- Stack original: Python + `customtkinter` (CTk), `tkinter.messagebox`, `json`, `os`, `pathlib`.
- Todas las citas `cotizador.py:N` son literales del código.
- El idioma de la interfaz es español (con los acentos y errores tipográficos
  que se indican textualmente más abajo).
- **La app legada NO genera PDF ni ningún tipo de exportación.** No existe
  ninguna referencia a PDF/impresión/exportación en el código. El PDF para
  clientes es un requisito nuevo; la sección 2.7 define qué componentes debe
  mostrar y cuáles no.

---

## 1. Archivo de configuración

### 1.1 Ruta

`cotizador.py:9-21`

```python
base_path = os.getenv('LOCALAPPDATA')      # :14
if not base_path:
    base_path = Path.home()                # :16
app_dir = Path(base_path) / "Cotizador3D"  # :18
app_dir.mkdir(parents=True, exist_ok=True) # :19
return app_dir / "config_impresion3d.json" # :21
```

Reglas exactas:

1. Ruta base = variable de entorno `LOCALAPPDATA`.
2. Fallback: si `LOCALAPPDATA` no existe **o está vacía** (`if not base_path`),
   se usa el directorio home del usuario (`Path.home()`).
3. Directorio de la app = `<base>/Cotizador3D`. Se crea siempre, con padres,
   sin error si ya existe (`mkdir(parents=True, exist_ok=True)`).
4. Archivo = `<base>/Cotizador3D/config_impresion3d.json`.
5. Esto se ejecuta **una sola vez al importar el módulo**, en la constante
   global `CONFIG_FILE` (`cotizador.py:24`). El directorio se crea aunque el
   usuario nunca guarde nada.

### 1.2 Esquema del JSON

Estructura de nivel superior: objeto con exactamente dos claves,
`"settings"` (objeto) y `"filaments"` (array). Se serializa con
`json.dump(data, f, indent=4)` y `encoding='utf-8'` (`cotizador.py:70-71`).

#### 1.2.1 `settings` — valores por defecto (`cotizador.py:29-42`)

Bloque literal del código:

```python
# cotizador.py:29-42
return {
    "settings": {
        "precio_kwh": "199.74640",     # :31
        "consumo_w": "150",            # :32
        "desgaste_horas": "5000",      # :33
        "precio_repuestos": "305000",  # :34
        "margen_error_pct": "10",      # :35
        "iva_luz_pct": "21",           # :36
        "margen_ganancia_x": "1.5",    # :37
        "costo_envio": "0",            # :38
        "geometry": "950x700"          # :39
    },
    "filaments": []                    # :41
}
```

| Clave | Valor por defecto | Tipo por defecto | Línea | Unidad / significado |
| --- | --- | --- | --- | --- |
| `precio_kwh` | `"199.74640"` | **string** | `:31` | moneda por kWh |
| `consumo_w` | `"150"` | **string** | `:32` | vatios (W) que consume la impresora |
| `desgaste_horas` | `"5000"` | **string** | `:33` | horas de vida útil de la máquina |
| `precio_repuestos` | `"305000"` | **string** | `:34` | moneda, costo total de repuestos |
| `margen_error_pct` | `"10"` | **string** | `:35` | porcentaje (0-100) |
| `iva_luz_pct` | `"21"` | **string** | `:36` | porcentaje (0-100) |
| `margen_ganancia_x` | `"1.5"` | **string** | `:37` | multiplicador (1.5 = +50 %) |
| `costo_envio` | `"0"` | **string** | `:38` | moneda, costo fijo de envío |
| `geometry` | `"950x700"` | **string** | `:39` | geometría `ANCHOxALTO` de la ventana principal |
| `filaments` | `[]` | **array** (raíz, no dentro de `settings`) | `:41` | lista de filamentos |

**Importante — los defaults son strings, pero lo que se persiste después son
números.** `save_settings_from_ui` (`cotizador.py:373-384`) escribe el
resultado de `get_float_from_entry`, que devuelve `float`
(`cotizador.py:415-416`). Por lo tanto, después del primer autoguardado el
archivo contiene `float` (JSON number) en `precio_kwh`, `consumo_w`,
`desgaste_horas`, `precio_repuestos`, `margen_error_pct`,
`margen_ganancia_x` y `costo_envio`. `iva_luz_pct` **nunca** se reescribe
desde la UI, así que conserva el tipo que tuviera (string `"21"` por defecto).
`geometry` siempre es string.

**Consecuencia para el port:** el lector debe aceptar **tanto string como
número** en todas las claves numéricas. El código legado lo logra con
`str(settings.get(...))` al cargar en la UI (`cotizador.py:356-362`) y con
`float(...)` al calcular (`cotizador.py:425`).

#### 1.2.2 `filaments` — estructura de cada elemento

Un filamento se crea en `cotizador.py:127` y se le agrega `id` en
`cotizador.py:189` o `cotizador.py:195`:

```python
new_data = {"brand": brand, "type": f_type, "price_kg": price_kg}  # :127
```

| Clave | Tipo | Origen | Notas |
| --- | --- | --- | --- |
| `brand` | string | `entry_brand.get().strip()` (`:116`) | marca; obligatorio, no vacío |
| `type` | string | `combobox_type.get()` (`:117`) | normalmente uno de `FILAMENT_TYPES`, pero el combo es editable y admite texto libre |
| `price_kg` | number (float) | `float(price_str)` (`:123`) | precio por kilogramo |
| `id` | string | `:189` (edición) / `:195` (alta) | identificador único |

Generación del `id` en alta (`cotizador.py:195`), literal:

```python
new_data['id'] = f"{new_data['brand'].lower()}_{new_data['type'].lower()}_{os.urandom(4).hex()}".replace(" ", "_")
```

- Se pasa a minúsculas marca y tipo, se une con `_`, se agrega un sufijo
  aleatorio de 4 bytes en hexadecimal (8 caracteres hex minúsculas).
- El `.replace(" ", "_")` se aplica **al string completo ya formateado**, no
  a cada parte por separado (mismo efecto práctico).
- Ejemplo: marca `"Grilon 3"`, tipo `"PLA"` → `grilon_3_pla_a1b2c3d4`.
- En edición el `id` se **preserva** del filamento original (`:189`).

Ejemplo completo de archivo:

```json
{
    "settings": {
        "precio_kwh": 199.7464,
        "consumo_w": 150.0,
        "desgaste_horas": 5000.0,
        "precio_repuestos": 305000.0,
        "margen_error_pct": 10.0,
        "iva_luz_pct": "21",
        "margen_ganancia_x": 1.5,
        "costo_envio": 0.0,
        "geometry": "1024x740"
    },
    "filaments": [
        {
            "brand": "Grilon3",
            "type": "PLA",
            "price_kg": 18500.0,
            "id": "grilon3_pla_9f3c1a02"
        }
    ]
}
```

### 1.3 Lógica de carga, migración y defaults

`load_data()` — `cotizador.py:44-65`:

1. Si el archivo **no existe** (`os.path.exists` falso, `:49`) → se devuelve
   `get_default_data()` (`:65`). **No se escribe** el archivo en ese momento.
2. Si existe, se abre en modo `'r'` con `encoding='utf-8'` y se lee todo el
   contenido (`:51-52`).
3. Si el contenido es **cadena vacía** (`if not content`, `:53`) → se devuelve
   `get_default_data()` (`:54`). Nota: un archivo con solo espacios/saltos de
   línea NO es "vacío" para esta comprobación y llega a `json.loads`, que
   lanzará `JSONDecodeError` → caso 6.
4. Se parsea con `json.loads(content)` (`:55`).
5. **Migraciones** (las únicas dos que existen):
   ```python
   if "geometry" not in data.get("settings", {}):      # :57
       data["settings"]["geometry"] = "950x700"        # :58
   if "costo_envio" not in data.get("settings", {}):   # :59
       data["settings"]["costo_envio"] = "0"           # :60
   ```
   Es decir: solo se rellenan `geometry` (con `"950x700"`) y `costo_envio`
   (con el **string** `"0"`). **Ninguna otra clave faltante se rellena aquí.**
6. Si hay `json.JSONDecodeError` o `FileNotFoundError` → `get_default_data()`
   (`:62-63`). Cualquier otra excepción (p. ej. `KeyError`, `AttributeError`,
   `UnicodeDecodeError`, `PermissionError`) **no** se captura y propaga; la
   app aborta al arrancar (ver sección 6, B1/B2).

Defaults "tardíos" (fuera de `load_data`), que actúan como red de seguridad
parcial para claves faltantes:

- `cotizador.py:220` — `self.data["settings"].get("geometry", "950x700")`.
- `cotizador.py:356-362` — al poblar la UI: `precio_kwh`→`"0"`,
  `consumo_w`→`"0"`, `desgaste_horas`→`"0"`, `precio_repuestos`→`"0"`,
  `margen_error_pct`→`"0"`, `margen_ganancia_x`→`"1.5"`, `costo_envio`→`"0"`.
  **Ojo:** estos defaults difieren de los de `get_default_data()`.
- `cotizador.py:425` — `float(self.data["settings"].get("iva_luz_pct", 21))`
  (default numérico `21`).
- `cotizador.py:315` — **acceso directo sin default**:
  `self.data['settings']['iva_luz_pct']` → `KeyError` si falta (ver B2).

### 1.4 Guardado

`save_data(data)` — `cotizador.py:67-73`:

```python
with open(CONFIG_FILE, 'w', encoding='utf-8') as f:  # :70
    json.dump(data, f, indent=4)                     # :71
```

- Sobrescribe el archivo completo (no hay merge parcial, no hay escritura
  atómica ni backup).
- Indentación de 4 espacios, UTF-8.
- Cualquier excepción se traga y solo se imprime en consola
  (`print(f"Error al guardar el archivo de configuración: {e}")`, `:73`).
  **El usuario nunca ve un error de guardado.**

Momentos en que se guarda:

| Disparador | Línea | Qué guarda |
| --- | --- | --- |
| `<FocusOut>` en cualquiera de los 7 campos de settings | `:371` → `:373-384` | settings + filaments (todo `self.data`) |
| Alta/edición de filamento | `:197` | todo `self.data` |
| Borrado de filamento | `:204` | todo `self.data` |
| Cierre de la ventana principal | `:485` | geometría + settings + filaments |

---

## 2. Fórmulas de cálculo

Todo el cálculo está en `App.calculate()` (`cotizador.py:418-475`). No hay
redondeo intermedio: se opera en `float` de doble precisión y **solo se
redondea al mostrar** (2 decimales, ver §3.4).

### 2.1 Lectura y parseo de entradas

`get_float_from_entry` — `cotizador.py:415-416`:

```python
def get_float_from_entry(self, entry, default="0"):
    return float((entry.get() or default).strip().replace(",", "."))
```

Regla exacta, en este orden:
1. Se toma el texto del campo. Si es **cadena vacía**, se usa `default`
   (`"0"` salvo que se indique otro).
2. Se aplica `.strip()` (espacios al inicio/fin).
3. Se reemplaza **toda** coma `,` por punto `.` (soporta decimal con coma;
   pero también convierte `"1,234.56"` en `"1.234.56"` → `ValueError`).
4. `float(...)`. Si falla → `ValueError`, capturado en `:472`.

Cuidado: un campo con solo espacios (`"   "`) NO es falsy, por lo que pasa a
`.strip()` → `""` → `float("")` → `ValueError`.

Variables leídas en `calculate` (`cotizador.py:420-433`), todas con
`default="0"`:

| Variable | Campo UI | Línea | Unidad |
| --- | --- | --- | --- |
| `precio_kwh` | `entry_kwh` | `:420` | moneda / kWh |
| `consumo_w` | `entry_consumo_w` | `:421` | W |
| `vida_util_horas` | `entry_desgaste_horas` | `:422` | h |
| `costo_repuestos` | `entry_precio_repuestos` | `:423` | moneda |
| `margen_error_pct` | `entry_margen_error` | `:424` | % |
| `iva_luz_pct` | **no hay campo** — se lee de `settings` | `:425` | % |
| `margen_ganancia` | `entry_ganancia` | `:426` | multiplicador |
| `gramos_filamento` | `entry_gramos` | `:427` | g |
| `costo_envio` | `entry_envio` | `:428` | moneda |
| `dias`, `horas`, `minutos`, `segundos` | 4 campos de tiempo | `:430-433` | d, h, min, s |

`iva_luz_pct` se lee **de la configuración, no de la UI**:
```python
iva_luz_pct = float(self.data["settings"].get("iva_luz_pct", 21))   # :425
```

### 2.2 Tiempo de impresión

```python
horas_impresion = (dias * 24) + horas + (minutos / 60) + (segundos / 3600)   # :434
```

- Resultado en **horas decimales**.
- Los 4 componentes son floats, no enteros: `1.5` en el campo "Horas" es
  válido y vale 1,5 h.

### 2.3 Selección del filamento y precio por kg

```python
selected_filament_str = self.combo_filamento.get()                               # :438
selected_filament = next((f for f in self.data["filaments"]
                          if f"{f['brand']} ({f['type']})" == selected_filament_str), None)  # :439
if not selected_filament: raise ValueError("Filamento no válido o no seleccionado.")         # :440
precio_kg_filamento = float(selected_filament["price_kg"])                       # :441
```

- La clave de búsqueda es el string `"{brand} ({type})"` — coincidencia exacta.
- Si hay dos filamentos con la misma marca y tipo, gana **el primero** de la
  lista (`next`), ver B6.

### 2.4 Componentes de costo (fórmulas literales)

```python
precio_material    = (gramos_filamento / 1000) * precio_kg_filamento                                  # :443
precio_luz         = (consumo_w / 1000) * horas_impresion * precio_kwh                                # :444
desgaste_maquina   = (costo_repuestos / vida_util_horas) * horas_impresion if vida_util_horas > 0 else 0  # :445
costo_base         = precio_material + precio_luz + desgaste_maquina                                  # :446
margen_error_valor = costo_base * (margen_error_pct / 100)                                            # :447
iva_luz_valor      = precio_luz * (iva_luz_pct / 100)                                                 # :448
costo_total        = costo_base + margen_error_valor + iva_luz_valor                                  # :449
precio_venta       = costo_total * margen_ganancia                                                    # :450
precio_final_con_envio = precio_venta + costo_envio                                                   # :451
```

Explicación paso a paso, con unidades:

1. **Costo de material** (`:443`)
   `precio_material [moneda] = (gramos_filamento [g] / 1000) * precio_kg_filamento [moneda/kg]`
   Es decir, el costo por gramo del filamento seleccionado es
   `price_kg / 1000`, y se multiplica por los gramos usados. La división por
   1000 es la conversión g → kg.

2. **Costo de luz** (`:444`)
   `precio_luz [moneda] = (consumo_w [W] / 1000) * horas_impresion [h] * precio_kwh [moneda/kWh]`
   `consumo_w / 1000` convierte W → kW; multiplicado por horas da kWh; por el
   precio del kWh da moneda. **No incluye IVA** (el IVA se suma aparte, paso 6).

3. **Desgaste / amortización de la máquina** (`:445`)
   `desgaste_maquina [moneda] = (costo_repuestos [moneda] / vida_util_horas [h]) * horas_impresion [h]`
   Amortización lineal: costo de repuestos repartido sobre la vida útil en
   horas, multiplicado por las horas de este trabajo.
   **Guarda:** si `vida_util_horas <= 0`, `desgaste_maquina = 0` (no hay error
   ni aviso; ver B7).

4. **Costo base** (`:446`)
   `costo_base = precio_material + precio_luz + desgaste_maquina`
   (material + luz sin IVA + desgaste).

5. **Margen de error** (`:447`)
   `margen_error_valor = costo_base * (margen_error_pct / 100)`
   Porcentaje aplicado **solo sobre `costo_base`**, es decir sobre material +
   luz + desgaste. NO se aplica sobre el IVA de la luz.

6. **IVA de la luz** (`:448`)
   `iva_luz_valor = precio_luz * (iva_luz_pct / 100)`
   Se aplica **únicamente sobre el costo de luz**, no sobre material ni
   desgaste, y no sobre el margen de error.

7. **COSTO TOTAL** (`:449`)
   `costo_total = costo_base + margen_error_valor + iva_luz_valor`
   Equivalente expandido:
   `costo_total = precio_material + precio_luz + desgaste_maquina
                + (precio_material + precio_luz + desgaste_maquina) * margen_error_pct/100
                + precio_luz * iva_luz_pct/100`

8. **PRECIO DE VENTA** (`:450`)
   `precio_venta = costo_total * margen_ganancia`
   `margen_ganancia` es un **multiplicador**, no un porcentaje: `1.5` = +50 %,
   `2` = duplicar, `1` = sin ganancia. (Etiqueta y placeholder lo confirman:
   `"Margen de Ganancia (x):"` `:293`, `"Ej: 1.5 para 50% de ganancia"` `:294`.)

9. **PRECIO FINAL** (`:451`)
   `precio_final_con_envio = precio_venta + costo_envio`
   El envío se suma **después** del margen de ganancia: el envío NO se
   multiplica por el margen.

### 2.5 Diagrama de composición

```
                       material  ─┐
                       luz       ─┼─► costo_base ─┬──────────────────────► costo_total ──► × margen_ganancia ──► precio_venta ──► + costo_envio ──► PRECIO FINAL
                       desgaste  ─┘               ├─► × margen_error_pct/100 ─┘                    (:450)                             (:451)
                                                  │
                       luz ──► × iva_luz_pct/100 ─┘
```

### 2.6 Validaciones dentro de `calculate`

```python
if horas_impresion <= 0: raise ValueError("La impresora no materializa objetos de inmediato (aún). El tiempo de impresión debe ser mayor a cero.")  # :435
if gramos_filamento <= 0: raise ValueError("Todavia la materia con masa 0 no se descubre (o quizas sí). Ingrese cuantos gramos de material se utilizará.")  # :436
if not selected_filament: raise ValueError("Filamento no válido o no seleccionado.")  # :440
```

Notas literales (textos con las faltas de ortografía del original,
reprodúzcanse tal cual si se quiere fidelidad total): `"Todavia"` sin tilde,
`"quizas"` sin tilde.

No se validan: valores negativos de `precio_kwh`, `consumo_w`,
`costo_repuestos`, `margen_error_pct`, `margen_ganancia`, `costo_envio`, ni
componentes de tiempo negativos (mientras la suma quede > 0).

### 2.7 Qué debe mostrar el PDF para clientes (requisito nuevo)

El desglose **sin margen de ganancia** son exactamente los valores calculados
hasta `:449` inclusive:

| Concepto | Variable | Línea |
| --- | --- | --- |
| Precio Material | `precio_material` | `:443` |
| Precio Luz | `precio_luz` | `:444` |
| Desgaste de la máquina | `desgaste_maquina` | `:445` |
| Margen de Error | `margen_error_valor` | `:447` |
| IVA Luz (`iva_luz_pct`%) | `iva_luz_valor` | `:448` |
| **COSTO TOTAL** | `costo_total` | `:449` |
| Costo de Envío (solo si > 0) | `costo_envio` | `:453-457` |

El margen de ganancia se aplica **únicamente en `:450`** (`precio_venta =
costo_total * margen_ganancia`). Por lo tanto, un PDF de cliente que muestre
el desglose "sin margen" debe cortar en `costo_total`, y si quiere mostrar un
total pagable debe usar `precio_final_con_envio` (`:451`) sin explicitar el
factor multiplicador ni `precio_venta` como línea de desglose. Nótese que
`precio_final_con_envio - costo_envio - costo_total` es exactamente la
ganancia oculta, así que mostrar simultáneamente `costo_total` y
`precio_final_con_envio` la deja deducible por resta — decisión de producto,
no del código legado.

---

## 3. Ventanas, campos, botones y formato

### 3.0 Apariencia global

- `ctk.set_appearance_mode("Dark")` (`:223`) — modo oscuro fijo.
- `ctk.set_default_color_theme("blue")` (`:224`) — tema azul.
- Ambas llamadas se hacen **después** de crear la ventana raíz (`super().__init__()` en `:215`).

### 3.1 App principal (`class App`, `cotizador.py:213-486`)

- Título de ventana: `"Calculadora de Costos de Impresión 3D"` (`:219`).
- Geometría inicial: `self.data["settings"].get("geometry", "950x700")` (`:220`).
- Tamaño mínimo: `minsize(920, 650)` (`:221`).
- Layout: grid de 1 fila × 2 columnas, ambas con `weight=1` (`:228-230`).
  - Columna 0: `frame_inputs`, `width=400`, `padx=20 pady=20 sticky="nsew"` (`:232-233`).
  - Columna 1: `frame_results`, `width=400`, `padx=(0,20) pady=20 sticky="nsew"` (`:235-236`).
- Orden de inicialización (`:238-243`): construir inputs → construir
  resultados → cargar settings a la UI → enlazar autoguardado → poblar combo
  de filamentos → limpiar resultados.
- Al cerrar: `protocol("WM_DELETE_WINDOW", self.on_closing)` (`:226`).

#### 3.1.1 Panel izquierdo — entradas (`create_inputs_widgets`, `:245-302`)

Sección **"Parámetros Fijos"** (título, `size=16 weight=bold`, `:249`):

| Etiqueta exacta | Widget | Valor inicial | Línea |
| --- | --- | --- | --- |
| `Precio Kwh:` | `entry_kwh` | `str(settings.get("precio_kwh", "0"))` | `:251-253`, `:356` |
| `Consumo real por hora (W):` | `entry_consumo_w` | `str(settings.get("consumo_w", "0"))` | `:254-256`, `:357` |
| `Vida útil de la Máquina (horas):` | `entry_desgaste_horas` | `str(settings.get("desgaste_horas", "0"))` | `:257-259`, `:358` |
| `Costo Repuestos:` | `entry_precio_repuestos` | `str(settings.get("precio_repuestos", "0"))` | `:260-262`, `:359` |
| `% de Margen de error:` | `entry_margen_error` | `str(settings.get("margen_error_pct", "0"))` | `:263-265`, `:360` |

Separador horizontal `height=2, fg_color="gray50"` (`:266-267`).

Sección **"Datos de la Impresión"** (título, `size=16 weight=bold`, `:268`):

| Etiqueta exacta | Widget | Valor inicial | Línea |
| --- | --- | --- | --- |
| `Filamento a usar:` | `combo_filamento` (CTkComboBox, `values=[]` inicial) | ver §3.1.3 | `:270-272` |
| — | Botón `Administrar Filamentos` (`fg_color="gray50"`, `hover_color="gray30"`) → abre `FilamentManagerWindow` | — | `:273-274` |
| `Tiempo de impresión:` | 4 entries en un frame transparente, columnas de peso igual | vacíos | `:276-288` |
| `Gramos de Filamento:` | `entry_gramos` | **vacío** (no se persiste) | `:290-292` |
| `Margen de Ganancia (x):` | `entry_ganancia`, placeholder `Ej: 1.5 para 50% de ganancia` | `str(settings.get("margen_ganancia_x", "1.5"))` | `:293-295`, `:361` |
| `Costo de Envío:` | `entry_envio`, placeholder `Costo fijo de envío` | `str(settings.get("costo_envio", "0"))` | `:296-298`, `:362` |

Los 4 campos de tiempo, en orden izquierda→derecha, con sus placeholders
exactos (`:280-287`): `Días`, `Horas`, `Min`, `Seg`. Todos vacíos al inicio y
nunca persistidos.

Separador horizontal (`:299-300`).

Botón principal: texto `📊 Calcular Costo` (emoji incluido), `size=14
weight=bold`, `height=40`, ocupa las 2 columnas, `command=self.calculate`
(`:301-302`).

#### 3.1.2 Panel derecho — resultados (`create_results_widgets`, `:304-352`)

Título: `"Resultados del Cálculo"` (`size=20 weight=bold`, `:308`).

Filas de desglose (`result_fields`, `:312-316`), en este orden exacto, con
etiqueta a la izquierda (`size=14`) y valor a la derecha (`size=14
weight=bold`, `anchor="e"`), valor inicial `"$ 0.00"` (`:322`):

| # | Etiqueta exacta | Clave interna | Línea |
| --- | --- | --- | --- |
| 1 | `Precio Material:` | `material` | `:313` |
| 2 | `Precio Luz:` | `luz` | `:313` |
| 3 | `Desgaste de la máquina:` | `desgaste` | `:314` |
| 4 | `Margen de Error:` | `error` | `:314` |
| 5 | `IVA Luz ({iva_luz_pct}%):` | `iva_luz` | `:315` |

La etiqueta 5 es una f-string construida **una sola vez al arrancar**:
`f"IVA Luz ({self.data['settings']['iva_luz_pct']}%):"` (`:315`). Con el
default `"21"` el texto es exactamente `IVA Luz (21%):`. Si el valor en el
JSON fuese numérico `21.0`, el texto sería `IVA Luz (21.0%):`.

Separador (`:327-328`), y luego los totales (`size=16 weight=bold`):

| Etiqueta exacta | Variable mostrada | Línea |
| --- | --- | --- |
| `COSTO TOTAL:` | `costo_total` | `:330-333`, valor en `:468` |
| `PRECIO DE VENTA:` | `precio_venta` | `:335-338`, valor en `:469` |
| `Costo de Envío:` | `costo_envio` (fila **ocultable**) | `:340-343`, valor en `:457` |

Bloque final: frame naranja `fg_color="#E65100"` (`:345`) con:
- Etiqueta `PRECIO FINAL` (`size=20 weight=bold`, `:349`) — texto dinámico,
  ver §3.1.5.
- Valor `precio_final_con_envio` (`size=22 weight=bold`, `anchor="e"`, `:351`,
  valor en `:470`).

#### 3.1.3 Combo de filamentos (`update_filament_combobox`, `:392-401`)

```python
filament_names = [f"{f['brand']} ({f['type']})" for f in self.data["filaments"]]  # :393
if not filament_names:
    filament_names = ["No hay filamentos definidos"]                              # :395
current_value = self.combo_filamento.get()                                        # :396
self.combo_filamento.configure(values=filament_names)                             # :397
if current_value in filament_names:
    self.combo_filamento.set(current_value)                                       # :399
else:
    self.combo_filamento.set(filament_names[0])                                   # :401
```

- Formato de cada opción: `"{brand} ({type})"`, exactamente ese espaciado.
- Si no hay filamentos, la única opción es el texto literal
  `"No hay filamentos definidos"`, que nunca coincide con ningún filamento →
  calcular con esa selección produce `"Filamento no válido o no seleccionado."`.
- Se preserva la selección actual si sigue existiendo; si no, se selecciona la
  primera opción.

#### 3.1.4 Autoguardado (`bind_autosave_events` / `save_settings_from_ui`, `:364-384`)

Se enlaza el evento `<FocusOut>` (`:371`) a exactamente **7** campos
(`:365-369`): `entry_kwh`, `entry_consumo_w`, `entry_desgaste_horas`,
`entry_precio_repuestos`, `entry_margen_error`, `entry_ganancia`,
`entry_envio`.

En cada pérdida de foco se ejecuta (`:373-384`):

```python
self.data["settings"]["precio_kwh"]        = self.get_float_from_entry(self.entry_kwh, "0")               # :375
self.data["settings"]["consumo_w"]         = self.get_float_from_entry(self.entry_consumo_w, "0")         # :376
self.data["settings"]["desgaste_horas"]    = self.get_float_from_entry(self.entry_desgaste_horas, "0")    # :377
self.data["settings"]["precio_repuestos"]  = self.get_float_from_entry(self.entry_precio_repuestos, "0")  # :378
self.data["settings"]["margen_error_pct"]  = self.get_float_from_entry(self.entry_margen_error, "0")      # :379
self.data["settings"]["margen_ganancia_x"] = self.get_float_from_entry(self.entry_ganancia, "1.5")        # :380
self.data["settings"]["costo_envio"]       = self.get_float_from_entry(self.entry_envio, "0")             # :381
self.save_app_data()                                                                                      # :382
```

- Es **todo o nada**: si *cualquiera* de los 7 campos no parsea, se lanza
  `ValueError`, se captura en `:383-384` con `pass` y **no se guarda nada**
  (ni siquiera los campos que sí eran válidos). Silencioso, sin aviso.
- NO se persisten: gramos, tiempo, filamento seleccionado, `iva_luz_pct`.

#### 3.1.5 Comportamiento del bloque de envío al calcular (`:453-461`)

```python
if costo_envio > 0:
    self.label_total_text.configure(text="PRECIO FINAL (con envío):")  # :454
    self.label_envio_text.grid()                                       # :455
    self.label_envio_valor.grid()                                      # :456
    self.label_envio_valor.configure(text=f"$ {costo_envio:,.2f}")     # :457
else:
    self.label_total_text.configure(text="PRECIO FINAL:")              # :459
    self.label_envio_text.grid_remove()                                # :460
    self.label_envio_valor.grid_remove()                               # :461
```

- La fila `Costo de Envío:` solo es visible cuando `costo_envio > 0`
  (estrictamente mayor; `0` la oculta, un valor negativo también la oculta).
- El rótulo del bloque naranja alterna entre `PRECIO FINAL (con envío):` y
  `PRECIO FINAL:`. En el estado inicial (`clear_results`, `:413`) es
  `PRECIO FINAL` **sin dos puntos** — ver B4.

#### 3.1.6 Estado inicial / limpieza (`clear_results`, `:403-413`)

- Los 5 valores del desglose, `COSTO TOTAL`, `PRECIO DE VENTA`, `Costo de
  Envío` y `PRECIO FINAL` se ponen en `"$ 0.00"` (`:404-409`).
- Se ocultan la etiqueta y el valor de envío (`grid_remove`, `:411-412`).
- El texto del total se fija en `"PRECIO FINAL"` (`:413`).
- Se llama una sola vez, al arrancar (`:243`). **Nunca se vuelve a llamar**
  (no hay botón "Limpiar"); tras un cálculo fallido los resultados anteriores
  quedan en pantalla (ver B5).

#### 3.1.7 Manejo de errores en `calculate` (`:472-475`)

```python
except (ValueError, TypeError) as e:
    messagebox.showerror("Error de Entrada",
        f"Por favor, verifica que todos los campos contengan números válidos.\n\nDetalle: {e}")  # :473
except Exception as e:
    messagebox.showerror("Error Inesperado", f"Ocurrió un error inesperado: {e}")                # :475
```

- Título 1: `Error de Entrada`. Cuerpo:
  `Por favor, verifica que todos los campos contengan números válidos.` +
  línea en blanco + `Detalle: <mensaje>`.
  Los tres mensajes de validación de §2.6 se muestran a través de este mismo
  cuadro, precedidos por el texto genérico sobre "números válidos" (ver B3).
- Título 2: `Error Inesperado`. Cuerpo: `Ocurrió un error inesperado: <e>`.
- Ninguno de los dos pasa `parent=`, a diferencia de los diálogos de las
  ventanas hijas (ver B10).

### 3.2 `FilamentManagerWindow` (`cotizador.py:137-210`)

- Ventana `CTkToplevel`, modal: `transient(master)` + `grab_set()` (`:142-143`).
- Título: `"Administrar Filamentos"` (`:141`).
- Tamaño fijo `600x450` (`:150-151`), **centrada sobre la ventana padre**
  (`:145-154`):
  ```python
  pos_x = master_x + (master_width // 2) - (win_width // 2)   # :152
  pos_y = master_y + (master_height // 2) - (win_height // 2) # :153
  self.geometry(f"{win_width}x{win_height}+{pos_x}+{pos_y}")  # :154
  ```
  División entera. Su geometría **no se persiste**.
- Cerrar: `protocol("WM_DELETE_WINDOW", self.on_close)` → `grab_release()` +
  `destroy()` (`:156`, `:208-210`).
- Contenido: frame principal (`padx=10 pady=10`), título
  `"Mis Filamentos"` (`size=16 weight=bold`, `:159`), un
  `CTkScrollableFrame` con la lista (`:161-162`), y abajo un botón
  `Agregar Nuevo Filamento` (`:163-164`).

Cada fila de la lista (`refresh_filament_list`, `:167-179`):

- Frame con `fg_color=("gray20", "gray20")` (`:171`).
- Texto (izquierda, `anchor="w"`), formato literal (`:173`):
  ```python
  info_text = f"{filament['brand']} ({filament['type']}) - ${float(filament['price_kg']):,.2f}/kg"
  ```
  Ejemplo: `Grilon3 (PLA) - $18,500.00/kg`. **Sin espacio tras `$`**, a
  diferencia del resto de la app (ver B9).
- Botón `Eliminar` (derecha, `width=80`, `fg_color="#D32F2F"`,
  `hover_color="#B71C1C"`, `:176-177`).
- Botón `Editar` (derecha, `width=80`, color por defecto, `:178-179`).
- El orden visual es el orden del array `filaments` (sin ordenación).
- La lista se reconstruye destruyendo todos los hijos (`:168-169`).

Acciones:

| Botón | Efecto | Línea |
| --- | --- | --- |
| `Agregar Nuevo Filamento` | abre `FilamentEditorWindow(self, on_close_callback=self.handle_filament_save)` sin datos | `:163`, `:181-182` |
| `Editar` | abre `FilamentEditorWindow(self, filament_data=f, on_close_callback=...)` | `:178`, `:184-185` |
| `Eliminar` | pide confirmación y borra | `:176`, `:201-206` |

Confirmación de borrado (`:202`), `messagebox.askyesno`:
- Título: `Confirmar`
- Mensaje: `¿Seguro que quieres eliminar el filamento {brand} ({type})?`
- `parent=self`
- Si el usuario acepta:
  ```python
  self.app.data["filaments"] = [f for f in self.app.data["filaments"] if f.get('id') != filament_to_delete.get('id')]  # :203
  self.app.save_app_data()       # :204
  self.refresh_filament_list()   # :205
  self.app.update_filament_combobox()  # :206
  ```
  Es decir: se eliminan **todos** los filamentos cuyo `id` coincida.

Guardado de un filamento (`handle_filament_save`, `:187-199`):

```python
if old_data:                                   # edición
    new_data['id'] = old_data.get('id')        # :189
    for i, f in enumerate(self.app.data["filaments"]):
        if f.get('id') == old_data.get('id'):  # :191
            self.app.data["filaments"][i].update(new_data)  # :192  (merge, no reemplazo)
            break
else:                                          # alta
    new_data['id'] = f"{...}_{os.urandom(4).hex()}".replace(" ", "_")  # :195
    self.app.data["filaments"].append(new_data)                        # :196
self.app.save_app_data()             # :197
self.refresh_filament_list()         # :198
self.app.update_filament_combobox()  # :199
```

- En edición se hace `dict.update()`: se conservan claves adicionales que
  existieran en el filamento original.
- Tras cualquier alta/edición/borrado se guarda a disco, se refresca la lista
  y se refresca el combo de la ventana principal.

### 3.3 `FilamentEditorWindow` (`cotizador.py:77-134`)

- `CTkToplevel` modal (`transient` + `grab_set`, `:80-81`).
- Título: `"Editar Filamento"` si recibe `filament_data`, `"Agregar Filamento"`
  si no (`:85`).
- Tamaño fijo `400x300` (`:93-94`), centrada sobre su `master` — que es la
  `FilamentManagerWindow`, no la ventana principal (`:88-97`, invocada desde
  `:182` y `:185`). No persiste geometría.
- Cerrar (X o tras guardar): `grab_release()` + `destroy()` (`:86`, `:132-134`).

Widgets, en orden vertical (todos `pack`, `fill="x"`, `padx=20`, `pady=10`):

| # | Widget | Texto / placeholder exacto | Línea |
| --- | --- | --- | --- |
| 1 | Etiqueta | `Detalles del Filamento` (`size=16 weight=bold`) | `:99-100` |
| 2 | `entry_brand` | placeholder `Marca del Filamento` | `:101-102` |
| 3 | `combobox_type` | valores = `FILAMENT_TYPES`; selección inicial = primer valor (`PLA`) | `:103-104` |
| 4 | `entry_price` | placeholder `Precio por KG (ej: 18500.00)` | `:105-106` |
| 5 | Botón | `Guardar` (`pady=20`), `command=self.save_filament` | `:107-108` |

Precarga en modo edición (`:110-113`):
- `entry_brand` ← `filament_data.get("brand", "")`
- `combobox_type` ← `filament_data.get("type", "PLA")` (default `"PLA"`)
- `entry_price` ← `str(filament_data.get("price_kg", ""))` (representación
  Python del float, p. ej. `18500.0`, no `18500.00`)

Guardado (`save_filament`, `:115-130`):

```python
brand = self.entry_brand.get().strip()                        # :116
f_type = self.combobox_type.get()                             # :117  (sin strip)
price_str = self.entry_price.get().strip().replace(",", ".")  # :118
if not brand or not f_type or not price_str:
    messagebox.showerror("Error", "Todos los campos son obligatorios.", parent=self)  # :120
    return
try:
    price_kg = float(price_str)                               # :123
except ValueError:
    messagebox.showerror("Error", "El precio debe ser un número válido.", parent=self)  # :125
    return
new_data = {"brand": brand, "type": f_type, "price_kg": price_kg}  # :127
```

Mensajes de error exactos (ambos con título `Error` y `parent=self`):
- `Todos los campos son obligatorios.` (`:120`)
- `El precio debe ser un número válido.` (`:125`)

Reglas de validación:
- `brand` se recorta y no puede quedar vacío.
- `f_type` no se recorta; solo se comprueba que no sea cadena vacía. El
  `CTkComboBox` es editable, así que se admite cualquier texto libre
  (no está restringido a `FILAMENT_TYPES`).
- `price_str` se recorta y se normaliza la coma decimal a punto antes de
  `float`. Se aceptan `0`, negativos y notación científica (`1e4`).
- No hay comprobación de duplicados (marca+tipo repetidos son posibles).
- Tras guardar con éxito: se invoca `on_close_callback(new_data,
  self.filament_data)` (`:129`) y se cierra la ventana (`:130`).

### 3.4 Formato numérico y de moneda

Todos los valores monetarios de la ventana principal usan la misma f-string
(`:457`, `:463-470`):

```python
f"$ {valor:,.2f}"
```

Es decir, formato **de locale inglés/C** de Python:
- Símbolo `$` seguido de **un espacio**.
- Separador de **miles**: coma `,`.
- Separador **decimal**: punto `.`.
- Siempre **2 decimales**, con redondeo bancario de `format()` (round-half-even
  sobre la representación binaria del float).
- Números negativos: el signo va **entre el `$ ` y el número**
  (`$ -1,234.50`).
- Ejemplo: `1234.5` → `$ 1,234.50`; `0` → `$ 0.00`.

Excepción: la lista de filamentos usa `f"${...:,.2f}/kg"` **sin espacio tras
el `$`** y con sufijo `/kg` (`:173`).

Valor por defecto mostrado en todos los labels de resultado antes de calcular:
la cadena literal `"$ 0.00"` (`:322`, `:332`, `:337`, `:342`, `:351`,
`:405-409`).

Los campos de entrada aceptan **coma o punto** como separador decimal
(`replace(",", ".")` en `:416` y `:118`), pero **no** aceptan separadores de
miles.

---

## 4. Persistencia de geometría y otros estados

### 4.1 Geometría de la ventana principal

Se guarda en `settings.geometry` (string). Al arrancar (`:220`):

```python
self.geometry(self.data["settings"].get("geometry", "950x700"))
```

Al cerrar (`on_closing`, `:477-486`):

```python
self.update_idletasks()                        # :479
try:
    size_only = self.geometry().split('+')[0]  # :481
    self.data["settings"]["geometry"] = size_only  # :482
except:
    pass                                       # :483-484
self.save_settings_from_ui()                   # :485
self.destroy()                                 # :486
```

Reglas:
- Se guarda **solo el tamaño** (`"ANCHOxALTO"`), descartando la posición
  (`+x+y`) mediante `split('+')[0]`.
- La posición de la ventana **no se persiste**: al reabrir, el gestor de
  ventanas decide dónde colocarla.
- El guardado efectivo lo hace `save_settings_from_ui()` (`:485`), que además
  vuelca los 7 settings numéricos y todo `filaments`. Si alguno de esos campos
  es inválido, **no se guarda nada, tampoco la geometría** (ver B8).
- `minsize(920, 650)` (`:221`) acota el tamaño; una geometría guardada menor
  se ve forzada al mínimo por Tk.

### 4.2 Estados NO persistidos

- Posición (x, y) de la ventana principal.
- Geometría y posición de `FilamentManagerWindow` y `FilamentEditorWindow`
  (siempre 600×450 y 400×300, recentradas cada vez).
- `iva_luz_pct` desde la UI (solo se lee; se edita a mano en el JSON).
- Filamento seleccionado en el combo.
- Gramos de filamento y los 4 campos de tiempo (días/horas/min/seg).
- Resultados del último cálculo.
- Modo de apariencia y tema (fijos en código).

### 4.3 Estado persistido, resumen

Únicamente el objeto `{"settings": {...}, "filaments": [...]}` completo, en
`config_impresion3d.json`, reescrito íntegro en cada guardado.

---

## 5. Constantes

```python
CONFIG_FILE = get_config_file_path()   # :24
FILAMENT_TYPES = ["PLA", "PETG", "TPU", "ABS", "ASA", "PLA-CF", "PETG-CF", "Nylon"]  # :25
```

`FILAMENT_TYPES` — 8 elementos, en este orden exacto (el primero, `PLA`, es la
selección por defecto del combo del editor):

1. `PLA`
2. `PETG`
3. `TPU`
4. `ABS`
5. `ASA`
6. `PLA-CF`
7. `PETG-CF`
8. `Nylon`

Otras constantes / literales incrustados:

| Valor | Significado | Línea |
| --- | --- | --- |
| `"950x700"` | geometría por defecto (3 lugares) | `:39`(default), `:58`(migración), `:220`(fallback) |
| `(920, 650)` | `minsize` de la ventana principal | `:221` |
| `"Dark"` / `"blue"` | modo de apariencia / tema | `:223-224` |
| `400 x 300` | tamaño de `FilamentEditorWindow` | `:93-94` |
| `600 x 450` | tamaño de `FilamentManagerWindow` | `:150-151` |
| `400` | `width` de `frame_inputs` y `frame_results` | `:232`, `:235` |
| `#E65100` | naranja del bloque PRECIO FINAL | `:345` |
| `#D32F2F` / `#B71C1C` | rojo botón Eliminar / hover | `:176` |
| `"gray50"` | color de los separadores y del botón Administrar | `:266`, `:299`, `:327`, `:273` |
| `"gray30"` | hover del botón Administrar Filamentos | `:273` |
| `("gray20","gray20")` | fondo de cada fila de filamento | `:171` |
| `1000` | divisor g→kg y W→kW | `:443`, `:444` |
| `24`, `60`, `3600` | conversión de días/min/seg a horas | `:434` |
| `100` | divisor de porcentajes | `:447`, `:448` |
| `4` | bytes aleatorios del `id` (`os.urandom(4)`) | `:195` |
| `"No hay filamentos definidos"` | opción única cuando no hay filamentos | `:395` |
| `21` | fallback numérico de `iva_luz_pct` en `calculate` | `:425` |
| `"1.5"` | fallback de `margen_ganancia_x` en la UI y en autosave | `:361`, `:380` |

Tamaños de fuente usados: 14 (desglose y botón calcular), 16 (títulos de
sección y totales), 20 (título de resultados y "PRECIO FINAL"), 22 (valor del
precio final).

---

## 6. Comportamientos dudosos o bugs observados

Cada punto indica `file:line`, qué pasa, y una recomendación. **La decisión de
replicar o corregir es del chief of staff.**

**B1 — `load_data` revienta si falta la clave `settings`.**
`cotizador.py:57-60`. `data.get("settings", {})` devuelve `{}` cuando la clave
no existe, la condición `"geometry" not in {}` es verdadera, y entonces
`data["settings"]["geometry"] = ...` lanza `KeyError: 'settings'`. El `except`
de `:62` solo captura `JSONDecodeError` y `FileNotFoundError`, así que la app
**se cierra con traceback al arrancar**. Lo mismo ocurre si el JSON de nivel
superior no es un objeto (p. ej. una lista o un número): `TypeError` /
`AttributeError` sin capturar. Recomendación: fusionar contra
`get_default_data()` en vez de dos parches puntuales, y capturar cualquier
excepción para caer a defaults.

**B2 — `iva_luz_pct` faltante hace crashear la construcción de la UI.**
`cotizador.py:315` accede directamente a `self.data['settings']['iva_luz_pct']`
sin default, mientras que `cotizador.py:425` sí usa `.get(..., 21)`. Un JSON
antiguo sin esa clave (no cubierto por las migraciones de `:57-60`) provoca
`KeyError` al construir el panel de resultados. Recomendación: usar el mismo
default en ambos sitios.

**B3 — Los mensajes de validación de negocio se muestran como errores de
formato.** `cotizador.py:435-436` y `:440` lanzan `ValueError` con textos de
negocio ("El tiempo de impresión debe ser mayor a cero", "Filamento no válido
o no seleccionado"), pero se capturan en `:472` y se presentan precedidos de
`"Por favor, verifica que todos los campos contengan números válidos."`, que
no corresponde. Recomendación: separar validación de negocio de errores de
parseo, con cuadros de diálogo distintos.

**B4 — Texto inconsistente del rótulo de precio final.**
`cotizador.py:413` lo inicializa como `"PRECIO FINAL"` (sin dos puntos),
mientras que `:454` usa `"PRECIO FINAL (con envío):"` y `:459`
`"PRECIO FINAL:"` (con dos puntos). El rótulo cambia de forma al primer
cálculo. Recomendación: unificar.

**B5 — Los resultados obsoletos quedan visibles tras un error.**
`clear_results()` (`:403-413`) solo se llama al arrancar (`:243`). Si un
cálculo falla en `:472-475`, se muestra el diálogo de error pero el panel
sigue mostrando los números del cálculo anterior, que ya no corresponden a los
datos en pantalla. Riesgo real de cotizar mal. Recomendación: limpiar
resultados al entrar en el `except`.

**B6 — `id` nulo y filamentos duplicados.**
`cotizador.py:191` compara `f.get('id') == old_data.get('id')`. Si el JSON fue
editado a mano y hay filamentos sin `id`, la comparación `None == None` es
verdadera y se edita **el primer filamento sin id**, no necesariamente el
elegido. El mismo problema en el borrado (`:203`), donde el filtro elimina
**todos** los filamentos sin `id` de una sola vez. Además, dos filamentos con
misma marca y tipo generan la misma etiqueta en el combo y `next(...)` en
`:439` siempre elige el primero, haciendo el segundo inseleccionable.
Recomendación: exigir `id` (generarlo al cargar si falta) y bloquear
duplicados marca+tipo, o desambiguar la etiqueta del combo.

**B7 — `vida_util_horas <= 0` produce desgaste 0 en silencio.**
`cotizador.py:445`: `... if vida_util_horas > 0 else 0`. Un usuario que deje el
campo vacío o en `0` obtiene una cotización sin amortización, sin ningún aviso.
Recomendación: validar como el resto (`raise ValueError`) o al menos avisar.

**B8 — Un campo inválido cancela TODO el guardado, incluida la geometría.**
`cotizador.py:373-384`: los 7 `set` están dentro del mismo `try`; si el
primero falla, ninguno se aplica y `save_app_data()` (`:382`) nunca se ejecuta;
el `except` hace `pass` (`:383-384`) sin avisar. Como `on_closing` (`:485`)
delega en esta función, cerrar la app con un campo mal escrito pierde
silenciosamente la geometría **y** los cambios de settings de toda la sesión.
Recomendación: guardar campo por campo (ignorando solo el inválido) y separar
el guardado de geometría del de settings.

**B9 — Formato de moneda inconsistente y no localizado.**
`cotizador.py:173` usa `${...:,.2f}` (sin espacio) mientras que `:457` y
`:463-470` usan `$ {...:,.2f}` (con espacio). Además, el formato `,`/`.` es el
inglés, incoherente con una app en español que acepta coma decimal en la
entrada (`:416`). Un usuario argentino ve `$ 18,500.00` y escribe `18.500,00`,
que la app rechaza. Recomendación: decidir un locale y aplicarlo en entrada y
salida de forma coherente.

**B10 — `messagebox` de `calculate` sin `parent`.**
`cotizador.py:473` y `:475` no pasan `parent=self`, a diferencia de `:120`,
`:125` y `:202`. El diálogo puede quedar detrás de la ventana o mal
posicionado. Recomendación: pasar siempre el padre.

**B11 — Fallo de guardado invisible.**
`cotizador.py:72-73`: cualquier error al escribir el JSON (permisos, disco
lleno) solo se imprime en `stdout`, que en un build empaquetado con
`pythonw`/`--noconsole` no existe. El usuario cree que guardó. Recomendación:
mostrar el error en la UI.

**B12 — `except:` desnudo al capturar la geometría.**
`cotizador.py:483`. Captura incluso `KeyboardInterrupt` y `SystemExit`.
Recomendación: capturar excepciones concretas.

**B13 — Tipos del JSON inconsistentes entre defaults y guardado.**
Los defaults son strings (`:31-39`), pero el autoguardado escribe floats
(`:375-381`), y la migración de `costo_envio` (`:60`) reinyecta un string. Un
mismo archivo puede tener `"consumo_w": 150.0` y `"iva_luz_pct": "21"`.
Funciona por casualidad (todos los consumidores hacen `str()` o `float()`),
pero la etiqueta de IVA (`:315`) sí depende del tipo: mostrará `IVA Luz (21%):`
o `IVA Luz (21.0%):` según cómo esté guardado. Recomendación: fijar un tipo
(number) en el esquema nuevo y normalizar al cargar.

**B14 — La etiqueta de IVA no se recalcula.**
`cotizador.py:312-316` construye el texto `IVA Luz (X%):` una única vez al
arrancar. No hay UI para cambiar `iva_luz_pct`, pero si se edita el JSON con la
app abierta, o si en el futuro se añade el campo, la etiqueta quedaría
desincronizada del valor usado en `:425`. Recomendación: recalcular la
etiqueta en cada `calculate`.

**B15 — El `%` de IVA no es editable desde la interfaz.**
Es el único parámetro económico sin campo (`:312-316`, `:425`). Obliga a editar
el JSON a mano. Recomendación (mejora, no bug estricto): agregar el campo.

**B16 — El combo de tipo de filamento admite texto libre.**
`cotizador.py:103` crea un `CTkComboBox` editable con `FILAMENT_TYPES`, pero
`:117` toma el texto tal cual, sin `strip()` ni validación contra la lista. Se
pueden guardar tipos arbitrarios o con espacios sobrantes, que luego se usan
para el `id` (`:195`) y para la etiqueta del combo principal (`:393`).
Recomendación: usar un desplegable no editable, o validar contra
`FILAMENT_TYPES`.

**B17 — El combo de filamento de la ventana principal también es editable.**
`cotizador.py:271`. El usuario puede escribir cualquier cosa encima de la
selección y obtener `"Filamento no válido o no seleccionado."` sin entender por
qué. Mismo remedio que B16.

**B18 — Valores negativos aceptados sin validación.**
`precio_kwh`, `consumo_w`, `costo_repuestos`, `margen_error_pct`,
`margen_ganancia`, `costo_envio` y `price_kg` admiten negativos
(`:420-428`, `:123`); los componentes de tiempo también, siempre que la suma
de `:434` quede > 0 (p. ej. `dias=-1, horas=25` → 1 h). Puede producir precios
negativos. Recomendación: validar rangos.

**B19 — El directorio de configuración se crea como efecto secundario de un
import.** `cotizador.py:19` dentro de `get_config_file_path()`, llamada en la
constante global `:24`. Crea `<base>/Cotizador3D` aunque solo se importe el
módulo (p. ej. en un test). Recomendación: crear el directorio de forma
perezosa, solo al guardar.

**B20 — Los gramos y el tiempo no se persisten pero los parámetros sí, sin
señal visual.** Mezclar en el mismo panel campos que se autoguardan al perder
el foco (los 7 de `:365-369`) con campos efímeros (gramos, tiempo) no está
indicado en la UI. No es un bug funcional, pero sí una fuente de confusión.
Recomendación de producto: separarlos visualmente o marcarlos.

**B21 — Sin escritura atómica ni respaldo.** `cotizador.py:70-71` abre en
modo `'w'` (trunca) y luego escribe. Un corte durante la escritura deja el
archivo truncado; en el mejor caso queda vacío y `load_data` cae a defaults
(`:53-54`), perdiendo todos los filamentos. Recomendación: escribir a temporal
y renombrar.
