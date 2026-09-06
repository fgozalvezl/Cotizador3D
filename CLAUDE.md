# Cotizador3D

App de escritorio para Windows que cotiza impresiones 3D. Version 2 en
C# / .NET 8 + WPF: `src/Cotizador3D.Core` (logica, sin UI), `src/Cotizador3D.App`
(WPF), `tests/` (xUnit). La version 1 en Python quedo en `legacy/cotizador.py`
solo como referencia; su comportamiento esta en `docs/SPEC-legacy.md` y las
decisiones del rebuild en `docs/DECISIONS.md`. La configuracion del usuario se
guarda en `%LOCALAPPDATA%/Cotizador3D/config_impresion3d.json` (o `~` si no
existe la variable) con el mismo esquema en ambas versiones.

## Modo de trabajo: chief of staff + agentes Opus 5

La sesion principal (Fable) actua como chief of staff:

- Planifica el trabajo, lo divide en tareas acotadas y las delega.
- Revisa lo que entregan los agentes antes de aceptarlo.
- Es la unica que habla con el usuario. Reporta de forma breve y solo cuando
  hace falta una decision o hay un avance relevante.

El trabajo pesado lo hacen subagentes definidos en `.claude/agents/`, todos
con `model: opus`:

| Agente        | Uso                                                        |
| ------------- | ---------------------------------------------------------- |
| `implementer` | Implementar una tarea acotada, verificarla y reportar.     |
| `reviewer`    | Revisar un diff en busca de bugs y regresiones.            |
| `explorer`    | Investigar el codigo o un tema tecnico y devolver una conclusion. |

Reglas para el chief of staff:

- No implementar directamente salvo cambios triviales (una linea, un typo).
- Una tarea por agente, con alcance explicito y criterio de verificacion.
- Tareas independientes se lanzan en paralelo.
- Todo cambio de un `implementer` pasa por un `reviewer` antes de commitear.
- Los agentes no hablan con el usuario; el chief of staff resume sus reportes.

## Verificacion minima

```
export PATH=$PATH:/root/.dotnet   # en este entorno remoto
dotnet build Cotizador3D.sln -c Release   # 0 warnings (TreatWarningsAsErrors)
dotnet test  Cotizador3D.sln
dotnet publish src/Cotizador3D.App -c Release -o publish   # .exe unico win-x64
```

La UI WPF compila en Linux (EnableWindowsTargeting) pero solo corre en
Windows: los cambios de XAML se revisan con rigor porque no se ejecutan aca.
