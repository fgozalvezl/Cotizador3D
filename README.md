# Cotizador3D

Aplicacion de escritorio para Windows que calcula el costo de una impresion 3D
(material, energia, desgaste de la maquina, margen de error, IVA, ganancia y
envio) y exporta una cotizacion en PDF para el cliente.

Version 2: reescrita desde cero en C# / .NET 8 con WPF. Un solo `.exe`, sin
instalar nada mas.

## Funcionalidades

- Calculo del precio final con el mismo modelo de costos de la version 1.
- Gestion de filamentos (marca, tipo, precio por kg).
- Interfaz oscura con animaciones en botones, campos, resultados y avisos.
- Exportacion a PDF con desglose para el cliente. El PDF **no muestra el margen
  de ganancia**: cada linea del desglose ya lo incluye, de modo que las lineas
  suman exactamente el total que paga el cliente.
- Autoguardado de parametros y de la posicion de la ventana.
- Compatible con la configuracion de la version 1
  (`%LOCALAPPDATA%\Cotizador3D\config_impresion3d.json`): los filamentos y
  parametros existentes se conservan.

## Descargar y usar

1. Bajar `Cotizador3D.exe` desde la ultima ejecucion de GitHub Actions
   (artefacto `Cotizador3D-win-x64`) o generarlo con `build/publish.ps1`.
2. Ejecutarlo. No requiere instalacion ni .NET en la maquina.

## Compilar

Requiere .NET 8 SDK.

```
dotnet build Cotizador3D.sln -c Release
dotnet test  Cotizador3D.sln
pwsh ./build/publish.ps1          # genera publish/Cotizador3D.exe
```

El Core y sus tests compilan y corren en Windows, Linux y macOS. La interfaz
WPF compila en cualquier plataforma pero solo se ejecuta en Windows.

## Estructura

```
src/Cotizador3D.Core/    Modelos, calculo, persistencia, formato y PDF (sin UI)
src/Cotizador3D.App/     Interfaz WPF (MVVM)
tests/                   Tests xUnit del Core
docs/                    Plan, decisiones y especificacion del legado
legacy/                  Version 1 en Python, solo como referencia
build/publish.ps1        Publicacion del ejecutable unico
```

## Licencias

- Codigo: MIT (ver `LICENSE`).
- PDF generado con [QuestPDF](https://www.questpdf.com) bajo licencia
  Community (gratuita para empresas con ingresos anuales menores a 1 M USD).
- Fuente Lato (SIL Open Font License) embebida por QuestPDF; el archivo
  `LatoFont/OFL.txt` acompaña al ejecutable por exigencia de esa licencia.
