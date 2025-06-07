import customtkinter as ctk
import json
import os
from tkinter import messagebox
from pathlib import Path

# --- LÓGICA DE DATOS Y CONFIGURACIÓN ---

def get_config_file_path():
    """
    Construye la ruta completa y segura al archivo de configuración
    dentro de %LOCALAPPDATA%/Cotizador3D y asegura que el directorio exista.
    """
    base_path = os.getenv('LOCALAPPDATA')
    if not base_path:
        base_path = Path.home()
        
    app_dir = Path(base_path) / "Cotizador3D"
    app_dir.mkdir(parents=True, exist_ok=True)
    
    return app_dir / "config_impresion3d.json"

# --- CONSTANTES ---
CONFIG_FILE = get_config_file_path()
FILAMENT_TYPES = ["PLA", "PETG", "TPU", "ABS", "ASA", "PLA-CF", "PETG-CF", "Nylon"]

def get_default_data():
    """Retorna una estructura de datos por defecto."""
    return {
        "settings": {
            "precio_kwh": "199.74640",
            "consumo_w": "150",
            "desgaste_horas": "5000",
            "precio_repuestos": "305000",
            "margen_error_pct": "10",
            "iva_luz_pct": "21",
            "margen_ganancia_x": "1.5",
            "costo_envio": "0",
            "geometry": "950x700"
        },
        "filaments": []
    }

def load_data():
    """
    Carga la configuración desde un archivo JSON en %LOCALAPPDATA%/Cotizador3D.
    Si no existe o está corrupto, usa valores por defecto.
    """
    if os.path.exists(CONFIG_FILE):
        try:
            with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
                content = f.read()
                if not content:
                    return get_default_data()
                data = json.loads(content)
                
                if "geometry" not in data.get("settings", {}):
                    data["settings"]["geometry"] = "950x700"
                if "costo_envio" not in data.get("settings", {}):
                    data["settings"]["costo_envio"] = "0"
                return data
        except (json.JSONDecodeError, FileNotFoundError):
            return get_default_data()
    else:
        return get_default_data()

def save_data(data):
    """Guarda los datos en el archivo de configuración."""
    try:
        with open(CONFIG_FILE, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=4)
    except Exception as e:
        print(f"Error al guardar el archivo de configuración: {e}")


# --- VENTANA DE EDICIÓN DE FILAMENTO ---
class FilamentEditorWindow(ctk.CTkToplevel):
    def __init__(self, master, filament_data=None, on_close_callback=None):
        super().__init__(master)
        self.transient(master)
        self.grab_set()
        self.on_close_callback = on_close_callback
        self.filament_data = filament_data

        self.title("Editar Filamento" if filament_data else "Agregar Filamento")
        self.protocol("WM_DELETE_WINDOW", self.on_close)
        
        self.master.update_idletasks()
        master_x = master.winfo_x()
        master_y = master.winfo_y()
        master_width = master.winfo_width()
        master_height = master.winfo_height()
        win_width = 400
        win_height = 300
        pos_x = master_x + (master_width // 2) - (win_width // 2)
        pos_y = master_y + (master_height // 2) - (win_height // 2)
        self.geometry(f"{win_width}x{win_height}+{pos_x}+{pos_y}")
        
        self.label_title = ctk.CTkLabel(self, text="Detalles del Filamento", font=ctk.CTkFont(size=16, weight="bold"))
        self.label_title.pack(pady=10)
        self.entry_brand = ctk.CTkEntry(self, placeholder_text="Marca del Filamento")
        self.entry_brand.pack(pady=10, padx=20, fill="x")
        self.combobox_type = ctk.CTkComboBox(self, values=FILAMENT_TYPES)
        self.combobox_type.pack(pady=10, padx=20, fill="x")
        self.entry_price = ctk.CTkEntry(self, placeholder_text="Precio por KG (ej: 18500.00)")
        self.entry_price.pack(pady=10, padx=20, fill="x")
        self.button_save = ctk.CTkButton(self, text="Guardar", command=self.save_filament)
        self.button_save.pack(pady=20, padx=20)

        if self.filament_data:
            self.entry_brand.insert(0, self.filament_data.get("brand", ""))
            self.combobox_type.set(self.filament_data.get("type", "PLA"))
            self.entry_price.insert(0, str(self.filament_data.get("price_kg", "")))

    def save_filament(self):
        brand = self.entry_brand.get().strip()
        f_type = self.combobox_type.get()
        price_str = self.entry_price.get().strip().replace(",", ".")
        if not brand or not f_type or not price_str:
            messagebox.showerror("Error", "Todos los campos son obligatorios.", parent=self)
            return
        try:
            price_kg = float(price_str)
        except ValueError:
            messagebox.showerror("Error", "El precio debe ser un número válido.", parent=self)
            return
        new_data = {"brand": brand, "type": f_type, "price_kg": price_kg}
        if self.on_close_callback:
            self.on_close_callback(new_data, self.filament_data)
        self.on_close()

    def on_close(self):
        self.grab_release()
        self.destroy()

# --- VENTANA DE ADMINISTRACIÓN DE FILAMENTOS ---
class FilamentManagerWindow(ctk.CTkToplevel):
    def __init__(self, master, app_instance):
        super().__init__(master)
        self.app = app_instance
        self.title("Administrar Filamentos")
        self.transient(master)
        self.grab_set()

        self.master.update_idletasks()
        master_x = master.winfo_x()
        master_y = master.winfo_y()
        master_width = master.winfo_width()
        master_height = master.winfo_height()
        win_width = 600
        win_height = 450
        pos_x = master_x + (master_width // 2) - (win_width // 2)
        pos_y = master_y + (master_height // 2) - (win_height // 2)
        self.geometry(f"{win_width}x{win_height}+{pos_x}+{pos_y}")

        self.protocol("WM_DELETE_WINDOW", self.on_close)
        self.main_frame = ctk.CTkFrame(self)
        self.main_frame.pack(fill="both", expand=True, padx=10, pady=10)
        self.label_title = ctk.CTkLabel(self.main_frame, text="Mis Filamentos", font=ctk.CTkFont(size=16, weight="bold"))
        self.label_title.pack(pady=(0, 10))
        self.scrollable_frame = ctk.CTkScrollableFrame(self.main_frame)
        self.scrollable_frame.pack(fill="both", expand=True, padx=5, pady=5)
        self.button_add = ctk.CTkButton(self.main_frame, text="Agregar Nuevo Filamento", command=self.add_filament)
        self.button_add.pack(pady=10)
        self.refresh_filament_list()

    def refresh_filament_list(self):
        for widget in self.scrollable_frame.winfo_children():
            widget.destroy()
        for filament in self.app.data["filaments"]:
            frame = ctk.CTkFrame(self.scrollable_frame, fg_color=("gray20", "gray20"))
            frame.pack(fill="x", pady=5, padx=5)
            info_text = f"{filament['brand']} ({filament['type']}) - ${float(filament['price_kg']):,.2f}/kg"
            label = ctk.CTkLabel(frame, text=info_text, anchor="w")
            label.pack(side="left", fill="x", expand=True, padx=10)
            delete_button = ctk.CTkButton(frame, text="Eliminar", width=80, fg_color="#D32F2F", hover_color="#B71C1C", command=lambda f=filament: self.delete_filament(f))
            delete_button.pack(side="right", padx=(0, 5), pady=5)
            edit_button = ctk.CTkButton(frame, text="Editar", width=80, command=lambda f=filament: self.edit_filament(f))
            edit_button.pack(side="right", padx=5, pady=5)

    def add_filament(self):
        FilamentEditorWindow(self, on_close_callback=self.handle_filament_save)
    
    def edit_filament(self, filament_data):
        FilamentEditorWindow(self, filament_data=filament_data, on_close_callback=self.handle_filament_save)

    def handle_filament_save(self, new_data, old_data):
        if old_data:
            new_data['id'] = old_data.get('id')
            for i, f in enumerate(self.app.data["filaments"]):
                if f.get('id') == old_data.get('id'):
                    self.app.data["filaments"][i].update(new_data)
                    break
        else:
            new_data['id'] = f"{new_data['brand'].lower()}_{new_data['type'].lower()}_{os.urandom(4).hex()}".replace(" ", "_")
            self.app.data["filaments"].append(new_data)
        self.app.save_app_data()
        self.refresh_filament_list()
        self.app.update_filament_combobox()

    def delete_filament(self, filament_to_delete):
        if messagebox.askyesno("Confirmar", f"¿Seguro que quieres eliminar el filamento {filament_to_delete['brand']} ({filament_to_delete['type']})?", parent=self):
            self.app.data["filaments"] = [f for f in self.app.data["filaments"] if f.get('id') != filament_to_delete.get('id')]
            self.app.save_app_data()
            self.refresh_filament_list()
            self.app.update_filament_combobox()

    def on_close(self):
        self.grab_release()
        self.destroy()

# --- APLICACIÓN PRINCIPAL ---
class App(ctk.CTk):
    def __init__(self):
        super().__init__()
        
        self.data = load_data()

        self.title("Calculadora de Costos de Impresión 3D")
        self.geometry(self.data["settings"].get("geometry", "950x700"))
        self.minsize(920, 650)

        ctk.set_appearance_mode("Dark")
        ctk.set_default_color_theme("blue")

        self.protocol("WM_DELETE_WINDOW", self.on_closing)

        self.grid_columnconfigure(0, weight=1)
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        self.frame_inputs = ctk.CTkFrame(self, width=400)
        self.frame_inputs.grid(row=0, column=0, padx=20, pady=20, sticky="nsew")
        
        self.frame_results = ctk.CTkFrame(self, width=400)
        self.frame_results.grid(row=0, column=1, padx=(0, 20), pady=20, sticky="nsew")

        self.create_inputs_widgets()
        self.create_results_widgets()
        self.load_settings_to_ui()
        self.bind_autosave_events()
        self.update_filament_combobox()
        self.clear_results()

    def create_inputs_widgets(self):
        frame = self.frame_inputs
        frame.grid_columnconfigure(1, weight=1)
        row_idx = 0
        label_gastos_fijos = ctk.CTkLabel(frame, text="Parámetros Fijos", font=ctk.CTkFont(size=16, weight="bold"))
        label_gastos_fijos.grid(row=row_idx, column=0, columnspan=2, pady=(10, 15), padx=20); row_idx += 1
        ctk.CTkLabel(frame, text="Precio Kwh:").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.entry_kwh = ctk.CTkEntry(frame)
        self.entry_kwh.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        ctk.CTkLabel(frame, text="Consumo real por hora (W):").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.entry_consumo_w = ctk.CTkEntry(frame)
        self.entry_consumo_w.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        ctk.CTkLabel(frame, text="Vida útil de la Máquina (horas):").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.entry_desgaste_horas = ctk.CTkEntry(frame)
        self.entry_desgaste_horas.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        ctk.CTkLabel(frame, text="Costo Repuestos:").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.entry_precio_repuestos = ctk.CTkEntry(frame)
        self.entry_precio_repuestos.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        ctk.CTkLabel(frame, text="% de Margen de error:").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.entry_margen_error = ctk.CTkEntry(frame)
        self.entry_margen_error.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        separator1 = ctk.CTkFrame(frame, height=2, fg_color="gray50")
        separator1.grid(row=row_idx, column=0, columnspan=2, pady=15, padx=20, sticky="ew"); row_idx += 1
        label_pieza = ctk.CTkLabel(frame, text="Datos de la Impresión", font=ctk.CTkFont(size=16, weight="bold"))
        label_pieza.grid(row=row_idx, column=0, columnspan=2, pady=(5, 15), padx=20); row_idx += 1
        ctk.CTkLabel(frame, text="Filamento a usar:").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.combo_filamento = ctk.CTkComboBox(frame, values=[])
        self.combo_filamento.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        self.button_manage_filaments = ctk.CTkButton(frame, text="Administrar Filamentos", fg_color="gray50", hover_color="gray30", command=self.open_filament_manager)
        self.button_manage_filaments.grid(row=row_idx, column=0, columnspan=2, pady=10, padx=20); row_idx += 1
        
        ctk.CTkLabel(frame, text="Tiempo de impresión:").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        time_frame = ctk.CTkFrame(frame, fg_color="transparent")
        time_frame.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew")
        time_frame.grid_columnconfigure((0, 1, 2, 3), weight=1)
        self.entry_dias = ctk.CTkEntry(time_frame, placeholder_text="Días")
        self.entry_dias.grid(row=0, column=0, padx=(0, 2), sticky="ew")
        self.entry_horas = ctk.CTkEntry(time_frame, placeholder_text="Horas")
        self.entry_horas.grid(row=0, column=1, padx=2, sticky="ew")
        self.entry_minutos = ctk.CTkEntry(time_frame, placeholder_text="Min")
        self.entry_minutos.grid(row=0, column=2, padx=2, sticky="ew")
        self.entry_segundos = ctk.CTkEntry(time_frame, placeholder_text="Seg")
        self.entry_segundos.grid(row=0, column=3, padx=(2, 0), sticky="ew")
        row_idx += 1

        ctk.CTkLabel(frame, text="Gramos de Filamento:").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.entry_gramos = ctk.CTkEntry(frame)
        self.entry_gramos.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        ctk.CTkLabel(frame, text="Margen de Ganancia (x):").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.entry_ganancia = ctk.CTkEntry(frame, placeholder_text="Ej: 1.5 para 50% de ganancia")
        self.entry_ganancia.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        ctk.CTkLabel(frame, text="Costo de Envío:").grid(row=row_idx, column=0, padx=20, pady=5, sticky="w")
        self.entry_envio = ctk.CTkEntry(frame, placeholder_text="Costo fijo de envío")
        self.entry_envio.grid(row=row_idx, column=1, padx=20, pady=5, sticky="ew"); row_idx += 1
        separator2 = ctk.CTkFrame(frame, height=2, fg_color="gray50")
        separator2.grid(row=row_idx, column=0, columnspan=2, pady=15, padx=20, sticky="ew"); row_idx += 1
        self.button_calcular = ctk.CTkButton(frame, text="📊 Calcular Costo", font=ctk.CTkFont(size=14, weight="bold"), height=40, command=self.calculate)
        self.button_calcular.grid(row=row_idx, column=0, columnspan=2, pady=10, padx=20, sticky="ew"); row_idx += 1

    def create_results_widgets(self):
        frame = self.frame_results
        frame.grid_columnconfigure(1, weight=1)

        label_resultados = ctk.CTkLabel(frame, text="Resultados del Cálculo", font=ctk.CTkFont(size=20, weight="bold"))
        label_resultados.grid(row=0, column=0, columnspan=2, pady=(10, 20), padx=20)
        
        self.result_labels = {}
        result_fields = [
            ("Precio Material:", "material"), ("Precio Luz:", "luz"),
            ("Desgaste de la máquina:", "desgaste"), ("Margen de Error:", "error"),
            (f"IVA Luz ({self.data['settings']['iva_luz_pct']}%):", "iva_luz")
        ]
        
        row_idx = 1
        for text, key in result_fields:
            label_text = ctk.CTkLabel(frame, text=text, font=ctk.CTkFont(size=14))
            label_text.grid(row=row_idx, column=0, padx=20, pady=8, sticky="w")
            value_label = ctk.CTkLabel(frame, text="$ 0.00", font=ctk.CTkFont(size=14, weight="bold"), anchor="e")
            value_label.grid(row=row_idx, column=1, padx=20, pady=8, sticky="ew")
            self.result_labels[key] = value_label
            row_idx += 1
            
        separator = ctk.CTkFrame(frame, height=2, fg_color="gray50")
        separator.grid(row=row_idx, column=0, columnspan=2, pady=10, padx=20, sticky="ew"); row_idx += 1
        
        label_costo_text = ctk.CTkLabel(frame, text="COSTO TOTAL:", font=ctk.CTkFont(size=16, weight="bold"))
        label_costo_text.grid(row=row_idx, column=0, padx=20, pady=10, sticky="w")
        self.label_costo_valor = ctk.CTkLabel(frame, text="$ 0.00", font=ctk.CTkFont(size=16, weight="bold"), anchor="e")
        self.label_costo_valor.grid(row=row_idx, column=1, padx=20, pady=10, sticky="ew"); row_idx += 1
        
        label_venta_text = ctk.CTkLabel(frame, text="PRECIO DE VENTA:", font=ctk.CTkFont(size=16, weight="bold"))
        label_venta_text.grid(row=row_idx, column=0, padx=20, pady=10, sticky="w")
        self.label_venta_valor = ctk.CTkLabel(frame, text="$ 0.00", font=ctk.CTkFont(size=16, weight="bold"), anchor="e")
        self.label_venta_valor.grid(row=row_idx, column=1, padx=20, pady=10, sticky="ew"); row_idx += 1

        self.label_envio_text = ctk.CTkLabel(frame, text="Costo de Envío:", font=ctk.CTkFont(size=16, weight="bold"))
        self.label_envio_text.grid(row=row_idx, column=0, padx=20, pady=10, sticky="w")
        self.label_envio_valor = ctk.CTkLabel(frame, text="$ 0.00", font=ctk.CTkFont(size=16, weight="bold"), anchor="e")
        self.label_envio_valor.grid(row=row_idx, column=1, padx=20, pady=10, sticky="ew"); row_idx += 1

        total_frame = ctk.CTkFrame(frame, fg_color="#E65100")
        total_frame.grid(row=row_idx, column=0, columnspan=2, pady=20, padx=20, sticky="ew"); row_idx += 1
        total_frame.grid_columnconfigure(1, weight=1)

        self.label_total_text = ctk.CTkLabel(total_frame, text="PRECIO FINAL", font=ctk.CTkFont(size=20, weight="bold"))
        self.label_total_text.grid(row=0, column=0, padx=20, pady=15, sticky="w")
        self.label_total_valor = ctk.CTkLabel(total_frame, text="$ 0.00", font=ctk.CTkFont(size=22, weight="bold"), anchor="e")
        self.label_total_valor.grid(row=0, column=1, padx=20, pady=15, sticky="e")

    def load_settings_to_ui(self):
        settings = self.data["settings"]
        self.entry_kwh.insert(0, str(settings.get("precio_kwh", "0")))
        self.entry_consumo_w.insert(0, str(settings.get("consumo_w", "0")))
        self.entry_desgaste_horas.insert(0, str(settings.get("desgaste_horas", "0")))
        self.entry_precio_repuestos.insert(0, str(settings.get("precio_repuestos", "0")))
        self.entry_margen_error.insert(0, str(settings.get("margen_error_pct", "0")))
        self.entry_ganancia.insert(0, str(settings.get("margen_ganancia_x", "1.5")))
        self.entry_envio.insert(0, str(settings.get("costo_envio", "0")))
    
    def bind_autosave_events(self):
        widgets_to_bind = [
            self.entry_kwh, self.entry_consumo_w, self.entry_desgaste_horas,
            self.entry_precio_repuestos, self.entry_margen_error, self.entry_ganancia,
            self.entry_envio
        ]
        for widget in widgets_to_bind:
            widget.bind("<FocusOut>", self.save_settings_from_ui)

    def save_settings_from_ui(self, event=None):
        try:
            self.data["settings"]["precio_kwh"] = self.get_float_from_entry(self.entry_kwh, "0")
            self.data["settings"]["consumo_w"] = self.get_float_from_entry(self.entry_consumo_w, "0")
            self.data["settings"]["desgaste_horas"] = self.get_float_from_entry(self.entry_desgaste_horas, "0")
            self.data["settings"]["precio_repuestos"] = self.get_float_from_entry(self.entry_precio_repuestos, "0")
            self.data["settings"]["margen_error_pct"] = self.get_float_from_entry(self.entry_margen_error, "0")
            self.data["settings"]["margen_ganancia_x"] = self.get_float_from_entry(self.entry_ganancia, "1.5")
            self.data["settings"]["costo_envio"] = self.get_float_from_entry(self.entry_envio, "0")
            self.save_app_data()
        except (ValueError, TypeError):
            pass

    def save_app_data(self):
        save_data(self.data)
        
    def open_filament_manager(self):
        FilamentManagerWindow(self, app_instance=self)

    def update_filament_combobox(self):
        filament_names = [f"{f['brand']} ({f['type']})" for f in self.data["filaments"]]
        if not filament_names:
            filament_names = ["No hay filamentos definidos"]
        current_value = self.combo_filamento.get()
        self.combo_filamento.configure(values=filament_names)
        if current_value in filament_names:
            self.combo_filamento.set(current_value)
        else:
            self.combo_filamento.set(filament_names[0])

    def clear_results(self):
        for label in self.result_labels.values():
            label.configure(text="$ 0.00")
        self.label_costo_valor.configure(text="$ 0.00")
        self.label_venta_valor.configure(text="$ 0.00")
        self.label_envio_valor.configure(text="$ 0.00")
        self.label_total_valor.configure(text="$ 0.00")
        
        self.label_envio_text.grid_remove()
        self.label_envio_valor.grid_remove()
        self.label_total_text.configure(text="PRECIO FINAL")

    def get_float_from_entry(self, entry, default="0"):
        return float((entry.get() or default).strip().replace(",", "."))

    def calculate(self):
        try:
            precio_kwh = self.get_float_from_entry(self.entry_kwh)
            consumo_w = self.get_float_from_entry(self.entry_consumo_w)
            vida_util_horas = self.get_float_from_entry(self.entry_desgaste_horas)
            costo_repuestos = self.get_float_from_entry(self.entry_precio_repuestos)
            margen_error_pct = self.get_float_from_entry(self.entry_margen_error)
            iva_luz_pct = float(self.data["settings"].get("iva_luz_pct", 21))
            margen_ganancia = self.get_float_from_entry(self.entry_ganancia)
            gramos_filamento = self.get_float_from_entry(self.entry_gramos)
            costo_envio = self.get_float_from_entry(self.entry_envio)

            dias = self.get_float_from_entry(self.entry_dias)
            horas = self.get_float_from_entry(self.entry_horas)
            minutos = self.get_float_from_entry(self.entry_minutos)
            segundos = self.get_float_from_entry(self.entry_segundos)
            horas_impresion = (dias * 24) + horas + (minutos / 60) + (segundos / 3600)
            if horas_impresion <= 0: raise ValueError("La impresora no materializa objetos de inmediato (aún). El tiempo de impresión debe ser mayor a cero.")
            if gramos_filamento <= 0: raise ValueError("Todavia la materia con masa 0 no se descubre (o quizas sí). Ingrese cuantos gramos de material se utilizará.")

            selected_filament_str = self.combo_filamento.get()
            selected_filament = next((f for f in self.data["filaments"] if f"{f['brand']} ({f['type']})" == selected_filament_str), None)
            if not selected_filament: raise ValueError("Filamento no válido o no seleccionado.")
            precio_kg_filamento = float(selected_filament["price_kg"])
            
            precio_material = (gramos_filamento / 1000) * precio_kg_filamento
            precio_luz = (consumo_w / 1000) * horas_impresion * precio_kwh
            desgaste_maquina = (costo_repuestos / vida_util_horas) * horas_impresion if vida_util_horas > 0 else 0
            costo_base = precio_material + precio_luz + desgaste_maquina
            margen_error_valor = costo_base * (margen_error_pct / 100)
            iva_luz_valor = precio_luz * (iva_luz_pct / 100)
            costo_total = costo_base + margen_error_valor + iva_luz_valor
            precio_venta = costo_total * margen_ganancia
            precio_final_con_envio = precio_venta + costo_envio

            if costo_envio > 0:
                self.label_total_text.configure(text="PRECIO FINAL (con envío):")
                self.label_envio_text.grid()
                self.label_envio_valor.grid()
                self.label_envio_valor.configure(text=f"$ {costo_envio:,.2f}")
            else:
                self.label_total_text.configure(text="PRECIO FINAL:")
                self.label_envio_text.grid_remove()
                self.label_envio_valor.grid_remove()

            self.result_labels["material"].configure(text=f"$ {precio_material:,.2f}")
            self.result_labels["luz"].configure(text=f"$ {precio_luz:,.2f}")
            self.result_labels["desgaste"].configure(text=f"$ {desgaste_maquina:,.2f}")
            self.result_labels["error"].configure(text=f"$ {margen_error_valor:,.2f}")
            self.result_labels["iva_luz"].configure(text=f"$ {iva_luz_valor:,.2f}")
            self.label_costo_valor.configure(text=f"$ {costo_total:,.2f}")
            self.label_venta_valor.configure(text=f"$ {precio_venta:,.2f}")
            self.label_total_valor.configure(text=f"$ {precio_final_con_envio:,.2f}")

        except (ValueError, TypeError) as e:
            messagebox.showerror("Error de Entrada", f"Por favor, verifica que todos los campos contengan números válidos.\n\nDetalle: {e}")
        except Exception as e:
            messagebox.showerror("Error Inesperado", f"Ocurrió un error inesperado: {e}")

    def on_closing(self):
        """Se ejecuta al cerrar la ventana para guardar el estado."""
        self.update_idletasks()
        try:
            size_only = self.geometry().split('+')[0]
            self.data["settings"]["geometry"] = size_only
        except:
            pass
        self.save_settings_from_ui()
        self.destroy()

if __name__ == "__main__":
    app = App()
    app.mainloop()