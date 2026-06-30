using UnityEngine;

namespace PhishingVR.Research.ExperimentA
{
    /// <summary>
    /// Alerta holográfica volumétrica mínima para el benchmark de renderizado (Exp. A).
    ///
    /// Anima escala y rotación. Importante para el experimento: la animación toca
    /// solo la MATRIZ por instancia (Transform), no propiedades de material, de modo
    /// que sigue siendo compatible con GPU Instancing — así el costo medido refleja
    /// el draw de N volúmenes idénticos, que es justo lo que comparas con/sin la técnica.
    /// </summary>
    public sealed class HolographicAlert : MonoBehaviour
    {
        [SerializeField] private float pulseSpeed   = 2.0f;
        [SerializeField] private float pulseAmount  = 0.08f;
        [SerializeField] private float spinDegPerSec = 45f;

        private Vector3 _baseScale;
        private float _phase;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _phase = Random.value * Mathf.PI * 2f; // desfase para que no pulsen al unísono
        }

        private void Update()
        {
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed + _phase) * pulseAmount;
            transform.localScale = _baseScale * s;
            transform.Rotate(Vector3.up, spinDegPerSec * Time.deltaTime, Space.Self);
        }
    }
}
