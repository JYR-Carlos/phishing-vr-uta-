# Guía de armado: E2_Web.unity
**Escenario 2 — Sitio Web Clonado | PhishingVR CC286**

Los scripts ya están listos. Esta guía te dice qué crear en Unity.
Sigue el orden exacto.

---

## PASO 1 — Crear la escena

1. **File → New Scene** → **Basic (URP)**
2. **File → Save As** → `Assets/Scenes/E2_Web.unity`
3. **File → Build Settings** → **Add Open Scenes**

---

## PASO 2 — XRRig

1. Busca el prefab `XRRig` en `Assets/Prefabs/`
2. Arrástralo a la jerarquía
3. Borra la **Main Camera** que viene por defecto

---

## PASO 3 — Entorno (sala de escritorio simple)

| GameObject | Tipo | Position | Scale |
|---|---|---|---|
| `E2_Floor` | 3D Cube | (0, -0.05, 0) | (6, 0.1, 6) |
| `E2_Desk` | 3D Cube | (0, 0.35, 0.8) | (2.5, 0.05, 1.2) |
| `E2_Wall_Back` | 3D Cube | (0, 1.5, 2) | (6, 3, 0.1) |

> Asígnales materiales distintos para que se vea mejor (Floor=gris, Desk=madera, Wall=blanco).

---

## PASO 4 — Canvas principal: el "Navegador"

Este es el monitor virtual con el sitio web falso.

### 4a. Crear el Canvas
1. Clic derecho en la jerarquía → **UI → Canvas** → renómbralo `E2_BrowserCanvas`
2. Configura el Canvas component:
   - **Render Mode:** `World Space`
   - **Width:** 800 / **Height:** 600
3. **Rect Transform:**
   - Position: (0, 1.2, 1.4) — frente al usuario, sobre el escritorio
   - Rotation: (0, 0, 0)
   - Scale: (0.002, 0.002, 0.002)
4. Agrega el component **Canvas Scaler**:
   - UI Scale Mode: `Constant Pixel Size`

### 4b. Estructura interna del Canvas (crear en orden)

```
E2_BrowserCanvas (Canvas World Space)
├── BG_Panel          ← Image, color blanco
├── BrowserBar        ← Image, color gris claro (#E5E7EB), height 50
│   ├── Padlock_Valid     ← Image, color verde, 30x30
│   ├── Padlock_Invalid   ← Image, color rojo, 30x30
│   └── TMP_URL           ← TextMeshProUGUI, font size 20, gris oscuro
├── BrowserContent    ← Image transparente (solo layout)
│   ├── TMP_Logo          ← TextMeshProUGUI, font 36, Bold, centrado
│   ├── TMP_FormFields    ← TextMeshProUGUI, font 18, multiline
│   └── ExcessiveFieldsWarning ← Image amarillo + TMP_Text: "⚠ Se solicitan datos inusuales"
└── BrowserButtons    ← solo layout, abajo del canvas
    ├── Btn_IngresarDatos   ← Button TMP, color rojo #DC2626
    └── Btn_ReportarSitio   ← Button TMP, color naranja #D97706
```

**Posiciones aproximadas (Anchor = center):**

| Objeto | Pos Y | Height |
|---|---|---|
| BG_Panel | 0 | 600 (full) |
| BrowserBar | 265 | 50 |
| TMP_URL | dentro de BrowserBar | — |
| Padlock_Valid | dentro de BrowserBar (izquierda) | 30 |
| Padlock_Invalid | mismo lugar que Padlock_Valid | 30 |
| BrowserContent | 50 | 430 |
| TMP_Logo | dentro, arriba | — |
| TMP_FormFields | dentro, centro | — |
| ExcessiveFieldsWarning | dentro, abajo | 40 |
| BrowserButtons | -250 | 60 |
| Btn_IngresarDatos | derecha, dentro de BrowserButtons | 50 |
| Btn_ReportarSitio | izquierda, dentro de BrowserButtons | 50 |

---

## PASO 5 — Panel de Feedback

Este panel aparece después de que el usuario toma una decisión.

1. Clic derecho en la jerarquía → **UI → Canvas** → `E2_FeedbackCanvas`
   - Render Mode: **World Space**
   - Width: 600 / Height: 400
   - Position: (0, 1.4, 0.9) — más cerca del usuario que el navegador
   - Scale: (0.002, 0.002, 0.002)
2. Dentro del canvas: **UI → Panel** → `FeedbackBG`
   - Empieza **desactivado**
3. Dentro de `FeedbackBG`:

| Nombre | Tipo | Rol |
|---|---|---|
| `TMP_Header` | TextMeshProUGUI | "¡Correcto!" / "Sitio falsificado..." |
| `TMP_Detail` | TextMeshProUGUI | Explicación educativa |
| `TMP_Indicators` | TextMeshProUGUI | Lista de indicadores de phishing |
| `Btn_Dismiss` | Button TMP | "Continuar" — llama a feedbackPanel.Dismiss() |

---

## PASO 6 — Crear el GameObject E2Manager

1. **GameObject → Create Empty** → `E2Manager`
2. **Add Component** → `E2 Manager`
3. Conectar en el Inspector:

| Campo | Qué arrastrar |
|---|---|
| Sites | (lo conectas en el Paso 8, después de crear los SiteData) |
| Browser | el GameObject que tiene el script `BrowserController` |
| Feedback Panel | el que tiene el script `E2FeedbackPanel` |
| Btn Report Site | `Btn_ReportarSitio` |
| Btn Enter Credentials | `Btn_IngresarDatos` |

---

## PASO 7 — Agregar scripts a los GameObjects

### BrowserController
1. Selecciona `E2_BrowserCanvas` (o crea un GO vacío hijo llamado `E2_BrowserController`)
2. **Add Component** → `Browser Controller`
3. Conectar:

| Campo | Qué arrastrar |
|---|---|
| Url Text | `TMP_URL` |
| Padlock Valid | `Padlock_Valid` |
| Padlock Invalid | `Padlock_Invalid` |
| Logo Text | `TMP_Logo` |
| Form Fields Text | `TMP_FormFields` |
| Excessive Fields Warning | `ExcessiveFieldsWarning` |

### E2FeedbackPanel
1. Selecciona `E2_FeedbackCanvas` (o un GO vacío llamado `E2_FeedbackController`)
2. **Add Component** → `E2 Feedback Panel`
3. Conectar:

| Campo | Qué arrastrar |
|---|---|
| Panel | `FeedbackBG` |
| Header Text | `TMP_Header` |
| Detail Text | `TMP_Detail` |
| Indicators Text | `TMP_Indicators` |
| Panel Background | `FeedbackBG` (el componente Image) |

---

## PASO 8 — Crear los SiteData ScriptableObjects

Los SiteData son los "datos" de cada sitio web que el escenario muestra.

### Opción A — Automática (recomendada)
Usa el script generador que está en `Assets/Scripts/E2_Web/Editor/E2SiteDataGenerator.cs`:

1. En Unity: menú superior → **Tools → PhishingVR → Generar SiteData E2**
2. Esto crea automáticamente 3 assets en `Assets/Data/E2/`:
   - `Site_BancoEstadoFalso.asset` ← phishing
   - `Site_RedSocialFalsa.asset` ← phishing
   - `Site_BancoEstadoReal.asset` ← legítimo

### Opción B — Manual
1. En la ventana **Project**: clic derecho → **Create → PhishingVR → Site Data**
2. Renombra el asset y llena los campos:

**Site falso (phishing):**
- Site Id: `site_banco_falso`
- Display Name: `BancoEstado`
- Display URL: `http://bancestado-cl.tk/login`
- Has HTTPS: ❌ (false)
- Logo Text: `BancEstado` ← typo intencional
- Is Phishing: ✅ (true)
- Active Indicators: `BadURL | NoHTTPS | TypoInLogo`
- Form Field Labels: `RUT`, `Clave`, `Clave Tarjeta`, `PIN`
- Educational Explanation: `Este sitio usa HTTP sin cifrado y el dominio es falso (...)`

**Site legítimo:**
- Display URL: `https://www.bancoestado.cl/login`
- Has HTTPS: ✅ (true)
- Is Phishing: ❌ (false)

### Conectar al E2Manager
Arrastra los 3 assets al campo **Sites** del `E2Manager` (el orden importa: se muestran en secuencia).

---

## PASO 9 — Conectar el botón Dismiss del Feedback

1. Selecciona `Btn_Dismiss` dentro del panel de feedback
2. En el componente Button → **On Click()** → presiona **+**
3. Arrastra el GO que tiene `E2FeedbackPanel`
4. Función: `E2FeedbackPanel.Dismiss`

---

## PASO 10 — Checklist de verificación

- [ ] `E2_BrowserCanvas` tiene Render Mode = World Space
- [ ] `Padlock_Invalid` está activo por defecto (para sitios sin HTTPS)
- [ ] `ExcessiveFieldsWarning` está desactivado por defecto
- [ ] `FeedbackBG` está desactivado por defecto
- [ ] `E2Manager.Sites` tiene 3 assets (mínimo 1 phishing)
- [ ] `BrowserController` tiene todas las referencias conectadas
- [ ] `E2FeedbackPanel` tiene todas las referencias conectadas
- [ ] Los botones `Btn_IngresarDatos` y `Btn_ReportarSitio` son visibles sobre el canvas

---

## PASO 11 — Probar

1. Dale **Play** en el Editor (con XR Device Simulator activo)
2. El escenario arranca mostrando el primer sitio automáticamente
3. Presiona Btn_ReportarSitio si crees que es phishing, Btn_IngresarDatos si crees que es real
4. Debería aparecer el panel de feedback con color verde (correcto) o rojo (incorrecto)
5. Después del feedback pasa al siguiente sitio
6. Al terminar los 3 sitios, se dispara `OnScenarioCompleted`

---

## Commit

```bash
git add Assets/Scripts/E2_Web/
git add Assets/Scenes/E2_Web.unity
git add Assets/Data/E2/
git commit -m "E2: escena navegador con SiteData y feedback"
git push origin feature/e2-web
```
