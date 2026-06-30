# Guía de armado: E3_Smishing.unity
**Juan Yampara — PhishingVR CC286**

Esta guía te dice exactamente qué crear en Unity, paso a paso.
Sigue el orden. Cada sección = una tarea en el Editor.

---

## PASO 1 — Crear la escena

1. En Unity: **File → New Scene** → elige **Basic (URP)**
2. **File → Save As** → `Assets/Scenes/E3_Smishing.unity`
3. En **File → Build Settings** → **Add Open Scenes** (la agrega a la build)

---

## PASO 2 — Agregar el XRRig

1. En la **ventana Project**, busca el prefab `XRRig` (está en `Assets/Prefabs/`)
2. Arrástralo a la jerarquía de la escena
3. Borra la **Main Camera** que creó la escena por defecto (el XRRig ya trae la suya)

---

## PASO 3 — Crear el entorno básico (sala simple)

Opcional pero recomendado para no tener el escenario en un espacio vacío:

1. **GameObject → 3D Object → Plane** → renómbralo `E3_Floor`
   - Scale: (5, 1, 5)
2. **GameObject → 3D Object → Cube** → `E3_Wall_Back`
   - Position: (0, 1.5, 2.5) / Scale: (5, 3, 0.1)

> No es necesario más detalle visual por ahora. El foco es el teléfono y la UI.

---

## PASO 4 — Crear el Smartphone virtual

### 4a. Modelo del teléfono
1. **GameObject → 3D Object → Cube** → renómbralo `E3_Smartphone`
   - Scale: (0.075, 0.15, 0.01)  ← proporción de teléfono
   - Position: (0.3, 0.9, 0)  ← empieza en una mesa/espacio al lado del usuario
2. Agrégale un material oscuro (negro o gris): **Assets → Create → Material** → `M_Smartphone`
   - Asígnalo arrastrando al objeto

### 4b. Scripts del teléfono
Con `E3_Smartphone` seleccionado, en el Inspector, **Add Component**:
- ✅ `XR Grab Interactable` (busca "XR Grab Interactable")
- ✅ `Rigidbody` (se agrega automáticamente con XRGrabInteractable)
- ✅ `Box Collider` (asegúrate de que esté)
- ✅ `Smartphone Controller` (el script que creamos)

> En Rigidbody: activa **Use Gravity** = true, **Is Kinematic** = false

---

## PASO 5 — Crear la UI del SMS (Canvas World Space)

La pantalla del teléfono es un Canvas que vive como hijo del Smartphone.

### 5a. Crear el Canvas
1. Selecciona `E3_Smartphone` en la jerarquía
2. Clic derecho → **UI → Canvas** → renómbralo `E3_SmartphoneCanvas`
3. En el Canvas component:
   - **Render Mode:** `World Space`
   - **Width:** 400 / **Height:** 700
4. En el **Rect Transform** del Canvas:
   - Scale: (0.0002, 0.0002, 0.0002) ← lo hace pequeño para que quepa en el teléfono
   - Position: (0, 0, -0.006) ← levemente al frente de la cara del cubo

### 5b. Crear el SMS Panel (hijo del Canvas)
1. Clic derecho en `E3_SmartphoneCanvas` → **UI → Panel** → renómbralo `E3_SMSPanel`
2. En la imagen del Panel:
   - Color de fondo: blanco o gris muy claro
3. **Desactívalo** (checkbox arriba a la izquierda del Inspector) — empieza oculto

### 5c. Crear los textos dentro del Panel
Clic derecho en `E3_SMSPanel` → **UI → Text - TextMeshPro** (x3):

| Nombre del GameObject | Posición Y aprox | Rol |
|---|---|---|
| `TMP_Sender` | 300 | Remitente (ej: BANCO-CHILE) |
| `TMP_Body` | 100 | Cuerpo del mensaje |
| `TMP_Link` | -80 | Link falso (color azul subrayado) |

Para `TMP_Sender`: Font Size 28, Bold, color negro
Para `TMP_Body`: Font Size 22, color gris oscuro
Para `TMP_Link`: Font Size 20, color azul (#2563EB), subrayado activado

### 5d. Crear los botones
Clic derecho en `E3_SMSPanel` → **UI → Button - TextMeshPro** (x2):

**Btn_ClickSMSLink** (botón rojo — trampa):
- Text: "Abrir Link"
- Position: (80, -200) / Width: 150 / Height: 50
- Image color: `#DC2626` (rojo)

**Btn_IgnoreSMS** (botón verde — correcto):
- Text: "Ignorar SMS"
- Position: (-80, -200) / Width: 150 / Height: 50
- Image color: `#16A34A` (verde)

---

## PASO 6 — Crear el objeto E3UIController

1. **GameObject → Create Empty** → renómbralo `E3UIController`
2. **Add Component** → `E3 UI Controller` (el script)
3. En el Inspector de E3UIController, arrastra las referencias:

| Campo | Qué arrastrar |
|---|---|
| Sms Panel | `E3_SMSPanel` |
| Sender Text | `TMP_Sender` |
| Sms Body Text | `TMP_Body` |
| Link Preview Text | `TMP_Link` |
| Btn Click SMS Link | `Btn_ClickSMSLink` |
| Btn Ignore SMS | `Btn_IgnoreSMS` |
| E3 Manager | (lo conectas en el Paso 7) |

---

## PASO 7 — Crear el objeto E3Manager

1. **GameObject → Create Empty** → renómbralo `E3Manager`
2. **Add Component** → `E3 Manager`
3. En el Inspector:

| Campo | Qué arrastrar |
|---|---|
| Smartphone Controller | `E3_Smartphone` (el que tiene SmartphoneController) |
| UI Controller | `E3UIController` |
| Feedback Panel | (ver paso 8) |
| Feedback Text | (ver paso 8) |

4. Vuelve a `E3UIController` y arrastra `E3Manager` al campo **E3 Manager**
5. Vuelve a `E3_Smartphone` → en **SmartphoneController** arrastra `E3UIController` al campo **UI Controller**

---

## PASO 8 — Panel de Feedback (resultado del escenario)

1. Clic derecho en la jerarquía → **UI → Canvas** → renómbralo `E3_FeedbackCanvas`
   - Render Mode: **World Space**
   - Posición: (0, 1.6, 1.5) — frente al usuario, a la altura de los ojos
   - Scale: (0.002, 0.002, 0.002)
2. Dentro: **UI → Panel** → `E3_FeedbackPanel`
   - Color: negro semitransparente (Alpha 0.85)
   - Desactivado al inicio
3. Dentro del panel: **UI → Text - TextMeshPro** → `TMP_FeedbackText`
   - Font Size: 32, centrado
4. Arrastra `E3_FeedbackPanel` y `TMP_FeedbackText` al E3Manager en sus campos correspondientes

---

## PASO 9 — Verificar conexiones (checklist)

Antes de darle Play, confirma:

- [ ] `E3_Smartphone` tiene: XRGrabInteractable + BoxCollider + Rigidbody + SmartphoneController
- [ ] `SmartphoneController.uiController` → apunta a `E3UIController`
- [ ] `E3UIController.e3Manager` → apunta a `E3Manager`
- [ ] `E3Manager.smartphoneController` → apunta a `E3_Smartphone`
- [ ] `E3Manager.uiController` → apunta a `E3UIController`
- [ ] `E3_SMSPanel` está **desactivado** al inicio
- [ ] `E3_FeedbackPanel` está **desactivado** al inicio
- [ ] Los botones están dentro del `E3_SMSPanel`

---

## PASO 10 — Probar sin Meta Quest (XR Device Simulator)

1. **Window → Package Manager** → instala **XR Device Simulator** si no está
2. En el XRRig prefab ya debería estar incluido el simulador
3. Dale **Play** en el Editor
4. Con WASD mueve el cuerpo, clic derecho rota la cabeza
5. Simula agarrar el teléfono con el controlador izquierdo/derecho
6. Sube el teléfono a la altura de los ojos → el SMS debería aparecer
7. Presiona uno de los botones → aparece el feedback → la escena termina

---

## Commit cuando todo funcione

```bash
git add Assets/Scripts/E3_Smishing/
git add Assets/Scenes/E3_Smishing.unity
git commit -m "E3: escenario smishing funcional con UI y feedback"
git push origin feature/e3-smishing
```
