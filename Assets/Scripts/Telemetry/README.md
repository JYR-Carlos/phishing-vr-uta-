# PhishVR — Sistema de Instrumentación de Rendimiento

Paper: "Optimización de Renderizado OpenXR y Latencia de Hand Tracking en VR Educativa"  
Plataforma: Meta Quest 3 · Unity 6 · OpenXR 1.16.1 · com.unity.xr.oculus 4.5.4

---

## Arquitectura de componentes

```
PerfHarness (GameObject)
 ├── PerfSampler               – Ring buffer de métricas por frame (GC-free)
 ├── HandLatencyProbe          – Proxy latencia via XRDisplaySubsystem.TryGetMotionToPhoton
 ├── RenderConditionController – Aplica FFR / render scale / MSAA en runtime
 ├── ExperimentRunner          – Orquesta condiciones A/B con warm-up
 ├── CsvLogger                 – Escribe raw.csv y summary.csv
 └── PerfHud (hijo)
      └── Canvas (World Space) – HUD en vivo anclado a la cámara
```

---

## Setup en Unity

### 1. Crear condiciones (primera vez)

```
PhishVR > Create Default Render Conditions
```

Crea `Assets/Data/Conditions/C0_Baseline.asset`, `C1_FFR.asset`, `C2_FFR_Scale.asset`, `C3_Aggressive.asset`.

### 2. Crear escena de prueba

```
PhishVR > Create PerfTest Scene
```

Abre `Assets/Scenes/PerfTest.unity`. Añade el prefab `PerfHarness` si no se adjuntó automáticamente.

### 3. Configurar ExperimentRunner

En el Inspector de `ExperimentRunner`:
- **Conditions**: arrastra los 4 assets en orden C0 → C3
- **Measure Duration Sec**: 60 (publicación) / 30 (debug rápido)
- **Warmup Duration Sec**: 5
- **Auto Advance**: activado para correr sin intervención
- **Advance Action** (opcional): asigna `XRI RightHand / Primary Button` para avance manual

### 4. Iniciar el experimento

Llama `ExperimentRunner.StartExperiment()` desde un botón UI o desde `Start()`.

---

## Condiciones del paper

| ID | displayName | FFR | Render Scale | MSAA |
|---|---|---|---|---|
| C0_Baseline | Baseline | Off (0) | 1.00 | 4x |
| C1_FFR | FFR High | High (3) | 1.00 | 4x |
| C2_FFR_Scale | FFR+Scale | High (3) | 0.85 | 2x |
| C3_Aggressive | Aggressive | High (3) | 0.70 | 1x |

---

## APIs verificadas (no inventadas)

| Componente | API | Fuente |
|---|---|---|
| FFR | `Unity.XR.Oculus.Utils.foveatedRenderingLevel` | FFR.cs · oculus 4.5.4 |
| GPU time | `XRDisplaySubsystem.TryGetAppGPUTimeLastFrame()` | OculusPerformance.cs |
| Motion-to-photon | `XRDisplaySubsystem.TryGetMotionToPhoton()` | OculusPerformance.cs |
| Dropped frames | `XRDisplaySubsystem.TryGetDroppedFrameCount()` | XR module built-in |
| CPU frame time | `FrameTimingManager.GetLatestTimings()` | Unity 6 built-in |
| Refresh rate | `Unity.XR.Oculus.Performance.TrySetDisplayRefreshRate()` | OculusPerformance.cs |
| Render scale | `UnityEngine.XR.XRSettings.eyeTextureResolutionScale` | XR module built-in |

> **Nota latencia:** `TryGetMotionToPhoton()` es un valor calculado por el runtime Oculus, no
> una medición física con hardware externo. Se documenta así en el paper como "software proxy".

> **Nota MSAA:** URP no está instalado en este proyecto. El código usa `QualitySettings.antiAliasing`
> y compila el bloque `#if URP_AVAILABLE` condicionalmente si URP se añade en el futuro.

---

## Extraer los CSV del Quest 3

Con el Quest conectado por USB (modo desarrollador activo):

```bash
# Ver todos los CSVs de la sesión
adb shell ls "/sdcard/Android/data/com.UTA.PhishVRCL/files/"

# Descargar al PC
adb pull "/sdcard/Android/data/com.UTA.PhishVRCL/files/" ./resultados/

# Alternativa: la ruta exacta se imprime en Logcat al terminar la sesión
adb logcat | grep CsvLogger
```

---

## Análisis estadístico (Python)

```python
import pandas as pd
from scipy import stats

raw     = pd.read_csv("raw_20260630_120000.csv")
summary = pd.read_csv("summary_20260630_120000.csv")

# Comparar C0 vs C3 en CPU
c0 = raw[raw.condition_id == "C0_Baseline"]["cpu_ms"]
c3 = raw[raw.condition_id == "C3_Aggressive"]["cpu_ms"]
t, p = stats.ttest_ind(c0, c3)
print(f"CPU t-test C0 vs C3: t={t:.3f} p={p:.4f}")
```

---

## Checklist antes del experimento

- [ ] Build mode: **Release** (Development Build OFF para no distorsionar GPU timings)
- [ ] Cerrar Oculus Developer Hub durante la captura (añade carga de red/USB)
- [ ] Quest 3 en modo **Standalone** (sin Air Link ni cable de streaming)
- [ ] Batería > 80% al inicio
- [ ] Sustained Performance Mode: activar en Android Player Settings si se requiere estabilidad térmica
