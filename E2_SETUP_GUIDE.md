# E2 Setup Guide — Escenario Web (Sitio Falsificado)
**PhishingVR · CC286-E.F.P · Universidad de Tarapacá 2026**

> Guía para armar manualmente la escena `E2_Web.unity` en el Editor de Unity.
> Todo nombre de campo y clase está extraído directamente del código fuente en `Assets/Scripts/E2_Web/`.

---

## 1. Resumen

El Escenario 2 simula un navegador web virtual en el que el usuario debe distinguir sitios
legítimos de sitios de phishing clonados. `E2Manager` orquesta una secuencia de sitios
definidos como `SiteData` (ScriptableObjects); para cada sitio espera que el usuario presione
uno de dos botones — **Ingresar credenciales** o **Reportar sitio** — y registra el tiempo
de decisión. Tras cada elección, `E2FeedbackPanel` muestra retroalimentación inmersiva en color
(verde / rojo / ámbar). Al terminar la secuencia, el manager calcula el perfil conductual
(`Cautious`, `Intermediate`, `Impulsive`) y el `RiskScore`, los empaqueta en un `ScenarioResult`
y dispara el evento `OnScenarioCompleted` que el `SimulatorOrchestrator` (Mathiu) usa para
avanzar al siguiente escenario.

Cada evento de decisión se envía también a una API REST local mediante `E2ApiClient`
(activa solo si el símbolo de compilación `PHISHINGVR_FLASK_ENABLED` está definido; en
desarrollo funciona como stub en consola).

---

## 2. Jerarquía de GameObjects a crear

Crear en `E2_Web.unity`. Nomenclatura: prefijo `E2_`, PascalCase (convención del proyecto).

```
E2_ScenarioRoot                     ← GameObject vacío — E2Manager.cs aquí
│
├── E2_BrowserPanel                 ← World Space Canvas — BrowserController.cs aquí
│   │   Canvas (World Space, 1.2 × 0.9 m, 100 px/unit)
│   │   TrackedDeviceGraphicRaycaster   (reemplaza GraphicRaycaster estándar)
│   │   Box Collider (Is Trigger = true, Size ≈ 1.2, 0.9, 0.01)
│   │
│   ├── E2_URLBar                   ← Panel HorizontalLayoutGroup
│   │   ├── E2_PadlockValid         ← Image (candado cerrado verde) — padlockValid
│   │   ├── E2_PadlockInvalid       ← Image (candado rojo/tachado)  — padlockInvalid
│   │   └── E2_URLText              ← TextMeshPro - Text (UI)        — urlText
│   │
│   ├── E2_SiteContent              ← Panel VerticalLayoutGroup
│   │   ├── E2_LogoText             ← TextMeshPro - Text (UI)        — logoText
│   │   ├── E2_FormFieldsText       ← TextMeshPro - Text (UI)        — formFieldsText
│   │   └── E2_ExcessiveFieldsWarning ← Panel con texto de aviso     — excessiveFieldsWarning
│   │
│   └── E2_ActionButtons            ← Panel HorizontalLayoutGroup
│       ├── Btn_IngresarDatos       ← Button (TMP)  → btnEnterCredentials en E2Manager
│       └── Btn_ReportarSitio       ← Button (TMP)  → btnReportSite en E2Manager
│
└── E2_FeedbackOverlay              ← GameObject vacío — E2FeedbackPanel.cs aquí
    └── E2_FeedbackPanel            ← World Space Canvas (overlay)   — campo "panel"
        │   Canvas (World Space, 1.0 × 0.6 m, posición delante del visor)
        │   TrackedDeviceGraphicRaycaster
        │
        ├── E2_FeedbackHeader       ← TextMeshPro - Text (UI)  — headerText
        ├── E2_FeedbackDetail       ← TextMeshPro - Text (UI)  — detailText
        ├── E2_FeedbackIndicators   ← TextMeshPro - Text (UI)  — indicatorsText
        ├── E2_FeedbackBackground   ← Image (Panel fondo)       — panelBackground
        └── Btn_DismissFeedback     ← Button (TMP) → E2FeedbackPanel.Dismiss()
```

### Notas de configuración del Canvas para VR

| Propiedad del Canvas  | Valor recomendado                                             |
| --------------------- | ------------------------------------------------------------- |
| Render Mode           | World Space                                                   |
| Width / Height        | 1200 / 900 (px, con 100 px/unit = 1.2 × 0.9 m)                |
| Posición en escena    | (0, 1.4, 1.8) — frente al usuario de pie                      |
| Rotation              | (0, 0, 0)                                                     |
| Canvas Scaler → Scale | 0.01 (1 px = 1 cm)                                            |
| GraphicRaycaster      | **Eliminar** y reemplazar por `TrackedDeviceGraphicRaycaster` |

---

## 3. Assets de configuración — SiteData (ScriptableObjects)

### Cómo crear

```
Menú Unity: Assets > Create > PhishingVR > Site Data
```

Crea el archivo en `Assets/Data/E2_SiteData/` (crear esa carpeta si no existe).

### Mínimo 3 instancias (2 phishing + 1 legítimo)

---

#### SD_Phish_01 — BadURL + NoHTTPS

| Campo                   | Valor                                                                                                                                                                          |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Site Id                 | `site_phish_01`                                                                                                                                                                |
| Display Name            | `BancoChile Falso (HTTP + dominio incorrecto)`                                                                                                                                 |
| Display URL             | `http://bancochile-seguridad.com/login`                                                                                                                                        |
| Has HTTPS               | ☐ (false)                                                                                                                                                                      |
| Logo Text               | `BancoChile`                                                                                                                                                                   |
| Is Phishing             | ☑ (true)                                                                                                                                                                       |
| Active Indicators       | `BadURL` + `NoHTTPS`                                                                                                                                                           |
| Form Field Labels       | `Usuario`, `Contraseña`, `Número de cuenta`                                                                                                                                    |
| Educational Explanation | `El sitio usaba HTTP sin cifrado y un dominio diferente al oficial (bancochile-seguridad.com en lugar de bancochile.cl). Sitios legítimos usan HTTPS y dominios verificables.` |

---

#### SD_Phish_02 — TypoInLogo + ExcessiveFormFields

| Campo                   | Valor                                                                                                                                               |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Site Id                 | `site_phish_02`                                                                                                                                     |
| Display Name            | `BancoChile Falso (logo con typo + campos extra)`                                                                                                   |
| Display URL             | `https://bancochile.cl/login`                                                                                                                       |
| Has HTTPS               | ☑ (true)                                                                                                                                            |
| Logo Text               | `BancoCh1le`  ← "i" reemplazada por "1"                                                                                                             |
| Is Phishing             | ☑ (true)                                                                                                                                            |
| Active Indicators       | `TypoInLogo` + `ExcessiveFormFields`                                                                                                                |
| Form Field Labels       | `Usuario`, `Contraseña`, `RUT`, `Fecha de nacimiento`, `Número de tarjeta`, `CVV`                                                                   |
| Educational Explanation | `El logotipo tenía un error tipográfico sutil (BancoCh1le) y el formulario solicitaba datos excesivos e inusuales para un simple inicio de sesión.` |

---

#### SD_Legit_01 — Sitio legítimo (control)

| Campo                   | Valor                                         |
| ----------------------- | --------------------------------------------- |
| Site Id                 | `site_legit_01`                               |
| Display Name            | `BancoChile Oficial`                          |
| Display URL             | `https://bancochile.cl/login`                 |
| Has HTTPS               | ☑ (true)                                      |
| Logo Text               | `BancoChile`                                  |
| Is Phishing             | ☐ (false)                                     |
| Active Indicators       | `None`                                        |
| Form Field Labels       | `Usuario`, `Contraseña`                       |
| Educational Explanation | *(vacío — no se muestra en sitios legítimos)* |

---

## 4. Cableado de referencias en el Inspector

### E2Manager (componente en `E2_ScenarioRoot`)

| Script / Componente | Campo del Inspector   | Qué arrastrar ahí                                             |
| ------------------- | --------------------- | ------------------------------------------------------------- |
| E2Manager           | Sites (lista)         | SD_Phish_01, SD_Legit_01, SD_Phish_02 (en ese orden sugerido) |
| E2Manager           | Browser               | GameObject `E2_BrowserPanel` (tiene BrowserController)        |
| E2Manager           | Feedback Panel        | GameObject `E2_FeedbackOverlay` (tiene E2FeedbackPanel)       |
| E2Manager           | Btn Report Site       | Button de `Btn_ReportarSitio`                                 |
| E2Manager           | Btn Enter Credentials | Button de `Btn_IngresarDatos`                                 |

> **Nota:** `E2Manager` conecta los `onClick` vía código (`AddListener`) en `Activate()`.
> No es necesario asignar métodos desde el Inspector en los eventos `OnClick()` de estos botones.
> Solo hace falta arrastrar la referencia al campo del Inspector listado arriba.

---

### BrowserController (componente en `E2_BrowserPanel`)

| Script / Componente | Campo del Inspector      | Qué arrastrar ahí                      |
| ------------------- | ------------------------ | -------------------------------------- |
| BrowserController   | Url Text                 | TMP_Text de `E2_URLText`               |
| BrowserController   | Padlock Valid            | GameObject `E2_PadlockValid`           |
| BrowserController   | Padlock Invalid          | GameObject `E2_PadlockInvalid`         |
| BrowserController   | Logo Text                | TMP_Text de `E2_LogoText`              |
| BrowserController   | Form Fields Text         | TMP_Text de `E2_FormFieldsText`        |
| BrowserController   | Excessive Fields Warning | GameObject `E2_ExcessiveFieldsWarning` |

---

### E2FeedbackPanel (componente en `E2_FeedbackOverlay`)

| Script / Componente | Campo del Inspector  | Qué arrastrar ahí                                                      |
| ------------------- | -------------------- | ---------------------------------------------------------------------- |
| E2FeedbackPanel     | Panel                | GameObject `E2_FeedbackPanel` (el Canvas hijo que se activa/desactiva) |
| E2FeedbackPanel     | Header Text          | TMP_Text de `E2_FeedbackHeader`                                        |
| E2FeedbackPanel     | Detail Text          | TMP_Text de `E2_FeedbackDetail`                                        |
| E2FeedbackPanel     | Indicators Text      | TMP_Text de `E2_FeedbackIndicators`                                    |
| E2FeedbackPanel     | Panel Background     | Image de `E2_FeedbackBackground`                                       |
| E2FeedbackPanel     | Display Duration     | `5` (segundos, valor por defecto)                                      |
| E2FeedbackPanel     | Color Correct        | Verde #2EA64A (`R:0.18 G:0.65 B:0.29`)                                 |
| E2FeedbackPanel     | Color Incorrect      | Rojo #CC3838 (`R:0.80 G:0.22 B:0.22`)                                  |
| E2FeedbackPanel     | Color False Positive | Ámbar #EB9C12 (`R:0.92 G:0.61 B:0.07`)                                 |

---

## 5. Conexión de botones y eventos

### Botones de acción del navegador

Los métodos `OnReport()` y `OnEnterCredentials()` son **privados** y se conectan en código.
Solo se requiere arrastrar los componentes Button a los campos del Inspector de `E2Manager`
(Sección 4). No toques el panel `OnClick()` de estos dos botones en el Editor.

### Botón de cierre del feedback

| Botón                 | Evento OnClick() | Objeto               | Método público            |
| --------------------- | ---------------- | -------------------- | ------------------------- |
| `Btn_DismissFeedback` | OnClick ()       | `E2_FeedbackOverlay` | `E2FeedbackPanel.Dismiss` |

**Pasos en el Inspector:**
1. Seleccionar `Btn_DismissFeedback`.
2. En el componente `Button → OnClick()`, clic en **+**.
3. Arrastrar `E2_FeedbackOverlay` al slot de objeto.
4. En el dropdown de función, seleccionar: `E2FeedbackPanel → Dismiss ()`.

---

## 6. Integración con XR Interaction Toolkit

### Canvas interactivos (E2_BrowserPanel y E2_FeedbackPanel)

1. **Eliminar** el componente `Graphic Raycaster` que Unity agrega por defecto.
2. **Agregar** `Tracked Device Graphic Raycaster` (paquete XR Interaction Toolkit).
3. En el XRRig prefab (no modificar el prefab, solo verificar que exista):
   - Cada controlador debe tener un `XR Ray Interactor` apuntando al Canvas.
   - El `XR Ray Interactor` debe tener `Interaction Layer Mask` que incluya la capa `UI` o la capa personalizada que uses.

### Capa de colisión para el ray-casting

| GameObject       | Componente adicional            | Layer | Is Trigger |
| ---------------- | ------------------------------- | ----- | ---------- |
| E2_BrowserPanel  | Box Collider (1.2 × 0.9 × 0.01) | `UI`  | ☑ true     |
| E2_FeedbackPanel | Box Collider (1.0 × 0.6 × 0.01) | `UI`  | ☑ true     |

> **⚠️ TODO: revisar** — Confirma con Pablo cuál `Interaction Layer Mask` usa el XRRig prefab
> para los Canvas de E1 (debería ser la misma capa para consistencia).

### Botones individuales

No se requiere `XR Simple Interactable` en cada botón. El `Tracked Device Graphic Raycaster`
delega los eventos al sistema de UI de Unity. Sí se requiere que el Button tenga una
`Image` con `Raycast Target = true` (activo por defecto en Unity UI).

---

## 7. Checklist de prueba en Play Mode (XR Device Simulator)

Antes de probar: verifica que el `XR Device Simulator` esté activo en el XRRig prefab
(`Window → XR → XR Device Simulator`).

### Controles del simulador

| Acción                     | Control                                |
| -------------------------- | -------------------------------------- |
| Mover cámara (cabeza)      | WASD                                   |
| Rotar cabeza               | Clic derecho + arrastrar               |
| Apuntar ray (mano derecha) | Mantener `Shift` + mover mouse         |
| Seleccionar / clic         | Clic izquierdo (mientras Shift activo) |

### Pasos de validación

- [ ] **Sitio phishing detectado correctamente**
  1. `E2Manager.Activate()` se llama (en desarrollo: activar el GO manualmente o llamar desde Awake temporalmente).
  2. Aparece `E2_BrowserPanel` con `SD_Phish_01` (URL en HTTP, candado inválido visible).
  3. Apuntar el ray a `Btn_ReportarSitio` y clicar.
  4. El panel de feedback aparece en **verde** con el header "¡Correcto!" y lista los indicadores (`BadURL`, `NoHTTPS`).
  5. El log de consola muestra: `[E2ApiClient] stub → {"SessionId":...,"Correct":true,...}`.
  6. Tras 5 s (o al clicar Dismiss), el feedback desaparece y aparece `SD_Legit_01`.

- [ ] **Sitio legítimo — falso positivo**
  1. Con `SD_Legit_01` visible (HTTPS, logo correcto, sin warning de campos).
  2. Clicar `Btn_ReportarSitio`.
  3. Feedback aparece en **ámbar** con el header "Falso positivo".
  4. En consola: `"Correct":false`.

- [ ] **Sitio phishing no detectado**
  1. Con `SD_Phish_02` visible (logo `BancoCh1le`, campos excesivos con warning visible).
  2. Clicar `Btn_IngresarDatos`.
  3. Feedback en **rojo** con header "Sitio falsificado no detectado" y el texto de `EducationalExplanation`.

- [ ] **Fin de secuencia**
  1. Al terminar los 3 sitios, verificar en consola que `OnScenarioCompleted` se invoca
     (agrega un `Debug.Log` temporal en `FinishScenario` si es necesario).
  2. El `ScenarioResult.ScenarioId` debe ser `"scenario_2"`.
  3. `BehavioralProfile` debe reflejar el tiempo promedio de decisión.

---

## 8. Problemas comunes

### 1. `NullReferenceException` en `BrowserController.ShowSite` o `E2FeedbackPanel.Show`

**Síntoma:** Excepción al activar el escenario, con referencia a `urlText`, `padlockValid`, etc.

**Causa:** Un campo `[SerializeField]` en el Inspector quedó sin asignar (campo en gris/vacío).

**Solución:** Seleccionar el GameObject con el componente en error → inspector → verificar que
cada campo apuntado en la Sección 4 tiene un objeto asignado (no dice "None").

---

### 2. Canvas no recibe ray-casting — los botones no responden

**Síntoma:** El ray del simulador atraviesa el Canvas sin activar botones.

**Causa A:** El Canvas tiene `Graphic Raycaster` estándar en lugar de `Tracked Device Graphic Raycaster`.

**Causa B:** La capa del Canvas/Collider no coincide con el `Interaction Layer Mask` del `XR Ray Interactor`.

**Solución:**
- Verificar que el componente en el Canvas sea `Tracked Device Graphic Raycaster` (XRI).
- En el XRRig → controlador derecho → `XR Ray Interactor` → `Interaction Layer Mask` → incluir la capa `UI`.

---

### 3. `E2_FeedbackPanel` siempre visible (no se oculta al iniciar)

**Síntoma:** El panel de feedback aparece en escena aunque no haya ningún evento aún.

**Causa:** El método `Awake()` de `E2FeedbackPanel` llama a `panel.SetActive(false)`, pero
el campo `panel` no está asignado en el Inspector → `NullReferenceException` silencioso en Awake.

**Solución:** Asignar el GameObject `E2_FeedbackPanel` (el Canvas hijo) al campo `Panel` del
componente `E2FeedbackPanel` en `E2_FeedbackOverlay`.

---

### 4. La secuencia se congela — `WaitUntil` nunca se resuelve

**Síntoma:** El navegador muestra un sitio pero los botones no avanzan la corrutina.

**Causa:** Los Button de `Btn_ReportarSitio` / `Btn_IngresarDatos` no están correctamente
asignados en el Inspector de `E2Manager` (campos `Btn Report Site` / `Btn Enter Credentials`),
por lo que `AddListener` en `Activate()` no conecta a ningún componente real.

**Solución:** Asignar los componentes Button (no los GameObjects) en los campos correspondientes
del `E2Manager` → `Buttons` section del Inspector. Verificar usando `Debug.Log` dentro de
`OnReport()` y `OnEnterCredentials()`.

---

*Última actualización: 2026-06-29 — basado en el código fuente de `Assets/Scripts/E2_Web/`.*
