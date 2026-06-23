# Instrucciones del Equipo — Simulador VR Phishing
## CC286-E.F.P: Ciberseguridad | Universidad de Tarapacá

**Integrantes:**
- Pablo Varas → E1: Email phishing
- Fabian Guarachi → E2: Sitio web clonado
- Juan Yampara → E3: Smishing
- Mathiu Orellana → Sistema central (Telemetría + Resultados)

---

## PASO 1 — Instalar Unity (todos)

1. Descarga **Unity Hub**: https://unity.com/download
2. En Unity Hub → **Installs** → **Install Editor**
3. Versión exacta: **Unity 6000.0.x LTS** (cualquier 6000.0)
4. Al instalar, marca solo estas opciones adicionales:
   - ✅ Android Build Support
   - ✅ OpenJDK
   - ✅ Android SDK & NDK Tools

> ⚠️ Todos deben usar la **misma versión de Unity**. Si uno abre el proyecto con otra versión rompe las escenas de todos.

---

## PASO 2 — Clonar el repositorio Git (todos)

```bash
git clone https://github.com/[USUARIO]/phishing-vr-uta.git
cd phishing-vr-uta
```

> Pablo crea el repositorio en GitHub y comparte el link al grupo.

### Regla de ramas
Cada integrante trabaja en **su propia rama**:

| Integrante | Rama |
|---|---|
| Pablo | `feature/e1-email` |
| Fabian | `feature/e2-web` |
| Juan | `feature/e3-smishing` |
| Mathiu | `feature/core-telemetry` |

```bash
# Cada uno ejecuta esto con su nombre de rama
git checkout -b feature/e1-email
```

**Regla de oro:** Nunca trabajes directamente en `main`. Solo Pablo hace merge a main cuando todo compila.

---

## PASO 3 — Abrir el proyecto en Unity (todos)

1. Unity Hub → **Projects** → **Open** → selecciona la carpeta clonada
2. Primera vez: Unity importa paquetes. Espera que diga **"importing"** y no lo cierres.
3. Cuando abra, ve a **File → Build Settings** y verifica que la plataforma sea **Android**.

---

## PASO 4 — Instalar los paquetes XR (todos, solo primera vez)

Ve a **Window → Package Manager** e instala en este orden:

| Paquete | Versión mínima |
|---|---|
| XR Plugin Management | 4.5.x |
| OpenXR Plugin | 1.11.x |
| XR Interaction Toolkit | 3.0.x |
| Unity OpenXR Meta | 2.x.x |

> En Package Manager: **Packages: Unity Registry** → busca cada uno → Install.

Luego en **Edit → Project Settings → XR Plug-in Management:**
- Pestaña Android → activa ✅ **OpenXR**
- Debajo aparece OpenXR → en Feature Groups activa ✅ **Meta Quest Support**

---

## PASO 5 — Estructura de carpetas (no mover ni renombrar)

```
Assets/
├── Scripts/
│   ├── Core/              ← NO TOCAR (scripts de Pablo que todos usan)
│   │   ├── IScenario.cs
│   │   ├── ScenarioResult.cs
│   │   ├── TelemetryManager.cs
│   │   └── SimulatorOrchestrator.cs
│   ├── E1_Email/          ← Solo Pablo trabaja aquí
│   ├── E2_Web/            ← Solo Fabian trabaja aquí
│   ├── E3_Smishing/       ← Solo Juan trabaja aquí
│   └── Telemetry/         ← Solo Mathiu trabaja aquí
├── Scenes/
│   ├── MainScene.unity    ← Mathiu
│   ├── E1_Email.unity     ← Pablo
│   ├── E2_Web.unity       ← Fabian
│   └── E3_Smishing.unity  ← Juan
└── Prefabs/
    └── XRRig.prefab       ← NO TOCAR (lo crea Pablo, todos lo usan)
```

**Regla:** Solo editas archivos dentro de tu carpeta y tu escena. Si necesitas cambiar algo de Core, avísale a Pablo primero.

---

## PASO 6 — Lo que cada uno debe implementar

### Pablo — E1_Email.unity + Scripts/E1_Email/

**Tu escena debe:**
- Tener una oficina virtual con un monitor mostrando un correo electrónico simulado
- El correo tiene: Btn_ReportPhishing (verde) y Btn_OpenLink (rojo)
- Implementar `RaycastInputInterceptor` que registre cada interacción con el correo
- Implementar `HeadPoseGazeEstimator` que estime dónde mira el usuario
- Al terminar llamar: `OnScenarioCompleted?.Invoke(result)`

**Script principal:** `E1Manager.cs` que implemente `IScenario`

---

### Fabian — E2_Web.unity + Scripts/E2_Web/

**Tu escena debe:**
- Mostrar un navegador web simulado con un sitio clonado (banco, red social)
- El sitio tiene campos de formulario y un botón de "Ingresar datos"
- Implementar retroalimentación visual inmersiva cuando el usuario interactúa (overlay de advertencia)
- Al terminar llamar: `OnScenarioCompleted?.Invoke(result)`

**Script principal:** `E2Manager.cs` que implemente `IScenario`

---

### Juan — E3_Smishing.unity + Scripts/E3_Smishing/

**Tu escena debe:**
- Tener un smartphone virtual que el usuario puede tomar con la mano
- El teléfono muestra un SMS de phishing con un link
- Botones: Btn_ClickSMSLink y Btn_IgnoreSMS
- Registrar tiempo de decisión y si cayó en la trampa
- Al terminar llamar: `OnScenarioCompleted?.Invoke(result)`

**Script principal:** `E3Manager.cs` que implemente `IScenario`

---

### Mathiu — MainScene.unity + Scripts/Telemetry/

**Tu escena debe:**
- Tener el StartPanel (botón iniciar simulación)
- Cargar E1 → E2 → E3 en secuencia usando `SimulatorOrchestrator`
- Recibir los `ScenarioResult` de los 3 escenarios
- Mostrar ResultsPanel con estadísticas al final
- Guardar toda la telemetría en archivo JSON local

**Script principal:** `TelemetryManager.cs` (Singleton que todos usan)

---

## PASO 7 — Contrato que TODOS deben respetar

Tu Manager principal DEBE implementar esta interfaz (está en `Scripts/Core/IScenario.cs`):

```csharp
public class E1Manager : MonoBehaviour, IScenario
{
    public event Action<ScenarioResult> OnScenarioCompleted;

    public void Activate()
    {
        // Mostrar tu escenario, iniciar timer
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        // Ocultar tu escenario
        gameObject.SetActive(false);
    }

    // Cuando el usuario toma una decisión:
    private void FinishScenario(bool detectedPhishing, float decisionTimeMs)
    {
        var result = new ScenarioResult
        {
            ScenarioId = "scenario_1",
            DetectedPhishing = detectedPhishing,
            DecisionTimeMs = decisionTimeMs,
            Timestamp = System.DateTime.UtcNow
        };
        OnScenarioCompleted?.Invoke(result);
    }
}
```

---

## PASO 8 — Workflow de Git (todos los días)

```bash
# Antes de empezar a trabajar — bajar cambios de main
git fetch origin
git merge origin/main

# Mientras trabajas — guardar tu trabajo
git add Assets/Scripts/E1_Email/
git add Assets/Scenes/E1_Email.unity
git commit -m "E1: agrego deteccion de gaze en zona encabezado"

# Al final del día — subir tu trabajo
git push origin feature/e1-email
```

### Qué NUNCA hacer en Git
- ❌ `git add .` o `git add -A` — puede subir archivos de Library/ o Temp/
- ❌ Modificar escenas que no son tuyas
- ❌ Hacer push a `main` directamente

### .gitignore — debe tener esto (Pablo lo configura)
```
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
*.csproj
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
UserSettings/
```

---

## PASO 9 — Cómo probar sin Meta Quest

Para probar en PC sin el casco:
1. **Window → Package Manager** → instala **XR Device Simulator**
2. En la escena, el XRRig prefab ya tiene el simulador incluido
3. Play en el Editor → usa WASD + clic derecho para simular el casco
4. Para probar en el Quest real: **File → Build and Run** (el Quest debe estar conectado por USB con Developer Mode activado)

---

## Contacto técnico

Dudas de Core/XR/Git: **Pablo Varas** (pablo.varas.burgos@alumnos.uta.cl)

---

*Proyecto CC286 — Universidad de Tarapacá — 2026*
