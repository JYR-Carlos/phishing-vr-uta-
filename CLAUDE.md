# PhishingVR — Simulador Inmersivo de Phishing en Realidad Virtual
## CC286-E.F.P: Ciberseguridad | Universidad de Tarapacá | 2026

---

## Qué es este proyecto

Simulador educativo de phishing en VR para Meta Quest 3 desarrollado en Unity 6.
Permite entrenar a usuarios para identificar ataques de phishing/smishing/web-spoofing
mediante experiencias inmersivas con retroalimentación en tiempo real.

El proyecto está dividido: **cada integrante desarrolla un escenario independiente**
que se conecta al sistema central mediante la interfaz `IScenario`.

---

## Equipo y responsabilidades

| Integrante | Rama Git | Escena | Scripts |
|---|---|---|---|
| **Pablo Varas** | `feature/e1-email` | `E1_Email.unity` | `Assets/Scripts/E1_Email/` |
| **Fabian Guarachi** | `feature/e2-web` | `E2_Web.unity` | `Assets/Scripts/E2_Web/` |
| **Juan Yampara** | `feature/e3-smishing` | `E3_Smishing.unity` | `Assets/Scripts/E3_Smishing/` |
| **Mathiu Orellana** | `feature/core-telemetry` | `MainScene.unity` | `Assets/Scripts/Telemetry/` |

---

## Stack tecnológico

- **Unity 6000.0.x LTS** (todos deben usar la misma versión)
- **URP** (Universal Render Pipeline)
- **OpenXR Plugin 1.11+** — NO usar Oculus SDK / OVRManager
- **XR Interaction Toolkit 3.0+** (XRI)
- **Unity OpenXR Meta 2.x** — para passthrough y Meta Quest features
- **Target platform:** Android (Meta Quest 3)
- **Input:** New Input System — InputActionReference, nunca Input.GetButton()
- **Passthrough:** CompositionLayer + PassthroughLayerData (NO OVRPassthrough)

---

## Arquitectura del proyecto

```
Assets/
├── Scripts/
│   ├── Core/              ← NO MODIFICAR sin avisar a Pablo
│   │   ├── IScenario.cs        interfaz que E1/E2/E3 implementan
│   │   ├── ScenarioResult.cs   datos que cada escenario entrega
│   │   ├── TelemetryManager.cs Singleton de telemetría (Mathiu)
│   │   └── SimulatorOrchestrator.cs flujo MainScene (Mathiu)
│   ├── E1_Email/          ← Solo Pablo
│   ├── E2_Web/            ← Solo Fabian
│   ├── E3_Smishing/       ← Solo Juan
│   └── Telemetry/         ← Solo Mathiu
├── Scenes/
│   ├── MainScene.unity    ← Mathiu
│   ├── E1_Email.unity     ← Pablo
│   ├── E2_Web.unity       ← Fabian
│   └── E3_Smishing.unity  ← Juan
└── Prefabs/
    └── XRRig.prefab       ← NO MODIFICAR, creado por Pablo
```

---

## Contrato IScenario — lo más importante

**Cada manager de escenario DEBE implementar esta interfaz.**
Está en `Assets/Scripts/Core/IScenario.cs`.

```csharp
// Así debe verse tu Manager
public class E1Manager : MonoBehaviour, IScenario
{
    public event Action<ScenarioResult> OnScenarioCompleted;

    public void Activate()   { gameObject.SetActive(true);  /* iniciar timer */ }
    public void Deactivate() { gameObject.SetActive(false); }

    private void FinishScenario(bool detected, float timeMs)
    {
        OnScenarioCompleted?.Invoke(new ScenarioResult
        {
            ScenarioId       = "scenario_1",   // "scenario_2" o "scenario_3"
            DetectedPhishing = detected,
            DecisionTimeMs   = timeMs,
            Timestamp        = System.DateTime.UtcNow
        });
    }
}
```

El `SimulatorOrchestrator` (Mathiu) llama `Activate()` cuando es tu turno
y espera el evento `OnScenarioCompleted` para continuar al siguiente.

---

## ScenarioResult — datos que debes llenar

```csharp
public class ScenarioResult
{
    public string ScenarioId;        // "scenario_1" / "scenario_2" / "scenario_3"
    public bool DetectedPhishing;    // ¿identificó el ataque correctamente?
    public float DecisionTimeMs;     // ms desde que apareció el estímulo hasta la decisión
    public string BehavioralProfile; // "Cautious" / "Intermediate" / "Impulsive"
    public float RiskScore;          // 0.0 a 1.0
    public DateTime Timestamp;
}
```

---

## Convenciones de código

- **Namespace:** `PhishingVR.Core` para Core / `PhishingVR.E1` para E1 / etc.
- **Nombres de GameObjects:** PascalCase con prefijo de escenario: `E1_MonitorCanvas`, `E2_BrowserPanel`
- **Nombres de scripts:** PascalCase, sufijo Manager para controladores: `E1Manager`, `E2Manager`
- **Eventos:** `event Action<T>` — nunca UnityEvent para comunicación entre escenarios
- **Telemetría:** siempre llamar `TelemetryManager.Instance.RegisterResult(result)` dentro de `FinishScenario`
- **Corrutinas:** para secuencias con tiempos; `async/await` NO (incompatible con algunos builds Android)

---

## Lo que hace cada escenario

### E1 — Email phishing (Pablo)
- Oficina virtual con monitor mostrando correo sospechoso
- Dos botones: `Btn_ReportPhishing` (verde) y `Btn_OpenLink` (rojo)
- `RaycastInputInterceptor`: intercepta entradas XR antes del motor de físicas
- `HeadPoseGazeEstimator`: estima mirada a 30Hz via head pose 6DoF
- `AttentionZoneMapper`: divide el canvas en 5 zonas (Header/Body/Link/Attachment/UI)
- `BehavioralPatternAnalyzer`: calcula RiskScore y clasifica en Cautious/Intermediate/Impulsive

### E2 — Sitio web clonado (Fabian)
- Navegador web virtual con formulario de login falso
- Botones: `Btn_IngresarDatos` y `Btn_CerrarPestana`
- Retroalimentación inmersiva overlay cuando el usuario ingresa datos
- Optimización de ray casting para múltiples elementos interactivos (links, campos)

### E3 — Smishing / SMS phishing (Juan)
- Smartphone virtual que el usuario puede agarrar (XRGrabInteractable)
- SMS visible cuando el teléfono está al nivel de los ojos (±0.25m)
- Botones: `Btn_ClickSMSLink` y `Btn_IgnoreSMS`
- Registro de tiempo de decisión y perfil de respuesta

### Sistema Central (Mathiu)
- `MainScene`: StartPanel → carga E1 → E2 → E3 → ResultsPanel
- `TelemetryManager`: Singleton, recibe ScenarioResult de los 3 escenarios
- Guarda JSON en `Application.persistentDataPath`
- Muestra estadísticas al final: tasa de detección, tiempo promedio

---

## Reglas de Git

```bash
# Tu rama (reemplaza e1-email con la tuya)
git checkout -b feature/e1-email

# Agregar solo TUS archivos
git add Assets/Scripts/E1_Email/
git add Assets/Scenes/E1_Email.unity

# Commit
git commit -m "E1: descripcion corta de lo que hiciste"

# Push
git push origin feature/e1-email
```

**NUNCA:**
- `git add .` o `git add -A`
- Modificar archivos de otra persona sin avisar
- Push directo a `main`
- Cambiar `Assets/Scripts/Core/` sin hablar con Pablo primero

---

## Cómo probar sin Meta Quest físico

1. Instala **XR Device Simulator** desde Package Manager
2. El XRRig prefab ya incluye el simulador
3. En Play Mode: WASD para mover, clic derecho para rotar cabeza, clic izquierdo para seleccionar

---

## Pendientes conocidos

- [ ] PassthroughController: asignar InputActionReference para botón A (Pablo)
- [ ] Btn_ReportPhishing y Btn_OpenLink: asignar targetGraphic (Pablo)
- [ ] E2_Web.unity: crear escena base (Fabian)
- [ ] E3_Smishing.unity: crear escena base (Juan)
- [ ] MainScene: conectar SimulatorOrchestrator con escenas en Build Settings (Mathiu)

---

## Contacto técnico del equipo

Dudas de arquitectura/Core/XR: **Pablo Varas** — pablo.varas.burgos@alumnos.uta.cl
