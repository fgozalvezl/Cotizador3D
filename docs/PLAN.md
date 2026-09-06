# Plan de rebuild: Cotizador3D v2

## Decision de stack

| Capa | Eleccion | Motivo |
| --- | --- | --- |
| Lenguaje / runtime | C# / .NET 8 | Nativo de Windows, arranque rapido, un solo .exe autocontenido. |
| UI | WPF (MVVM) | Render por DirectX, animaciones declarativas (Storyboards, VisualStates), theming completo. |
| PDF | QuestPDF (licencia Community) | Motor puro .NET, sin dependencias externas, layout fluido, se puede testear en CI Linux. |
| Tests | xUnit | Estandar .NET; la logica de calculo y persistencia se testea sin UI. |
| Empaquetado | `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` | Un ejecutable, sin instalar .NET en la maquina del usuario. |

Alternativas descartadas: Electron/Tauri (runtime web, mas pesado o dependiente
de WebView2), WinUI 3 (no compila fuera de Windows, tooling inmaduro), Avalonia
(no es el stack nativo de Windows).

## Estructura

```
Cotizador3D.sln
Directory.Build.props              # TFM, nullable, warnings, version
src/
  Cotizador3D.Core/                # net8.0, sin UI
    Models/       Settings, Filament, QuoteInput, QuoteResult, AppData
    Calculation/  QuoteCalculator (formulas exactas del legado)
    Persistence/  ConfigStore (mismo JSON y ruta que el legado, con migracion)
    Export/       QuotePdfExporter (desglose SIN margen de ganancia)
  Cotizador3D.App/                 # net8.0-windows, WPF
    App.xaml, MainWindow.xaml
    ViewModels/   MainViewModel, FilamentManagerViewModel, FilamentEditorViewModel
    Views/        FilamentManagerWindow, FilamentEditorWindow
    Themes/       Colors.xaml, Controls.xaml (estilos + animaciones), Animations.xaml
    Services/     DialogService, FileDialogService
tests/
  Cotizador3D.Core.Tests/          # xUnit: calculo, persistencia, PDF
build/
  publish.ps1                      # genera el .exe unico
.github/workflows/build.yml        # build + tests + artefacto .exe
```

## Reglas de compatibilidad

- El archivo `config_impresion3d.json` en `%LOCALAPPDATA%/Cotizador3D` se lee y
  escribe con el MISMO esquema que la app Python (valores como strings), para que
  el usuario no pierda su configuracion ni sus filamentos al migrar.
- Las formulas de calculo se implementan exactamente como en `docs/SPEC-legacy.md`.
  Los bugs listados alli se corrigen solo si el chief of staff lo decide y se
  documenta en `docs/DECISIONS.md`.

## PDF para el cliente

Contenido: datos del trabajo, desglose de costos (material, energia, desgaste,
margen de error, envio), total. **Nunca** aparece el margen de ganancia ni como
linea ni como porcentaje: el total mostrado es el precio final ya calculado, y
las lineas del desglose se escalan proporcionalmente para que sumen ese total.
De esa forma el cliente ve un desglose coherente sin saber cuanto es ganancia.

## Fases y delegacion

1. **Spec** (explorer): `docs/SPEC-legacy.md`. 
2. **Core** (implementer A): solucion, Core, ConfigStore, QuoteCalculator, tests.
3. **UI** (implementer B, en paralelo con 4): WPF MVVM, theming, animaciones.
4. **PDF** (implementer C, en paralelo con 3): QuotePdfExporter + tests que
   generan el PDF y verifican que no contenga la palabra "ganancia" ni el valor
   del multiplicador.
5. **Review** (reviewer): diff completo, bugs y regresiones contra la spec.
6. **Empaquetado**: publish.ps1, workflow de CI, README nuevo.

## Verificacion

```
dotnet build -c Release
dotnet test
dotnet publish src/Cotizador3D.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

En este entorno (Linux) se compila y se corren los tests del Core; la UI se
compila con `EnableWindowsTargeting` pero solo se ejecuta en Windows.
