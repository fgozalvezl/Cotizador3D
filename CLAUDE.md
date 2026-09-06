# Cotizador3D

App de escritorio en Python (customtkinter) para cotizar impresiones 3D.
Todo el codigo vive en `cotizador.py`. La configuracion del usuario se guarda
en `%LOCALAPPDATA%/Cotizador3D/config_impresion3d.json` (o `~` si no existe
la variable).

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
python -m py_compile cotizador.py
```

No hay tests automatizados todavia. La app requiere entorno grafico para
ejecutarse.
