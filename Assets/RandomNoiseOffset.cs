using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RandomNoiseOffset : MonoBehaviour
{
    [Tooltip("Максимальный диапазон для случайного смещения шума")]
    public float maxRandomOffset = 1000f;

    private void Start()
    {
        Renderer rend = GetComponent<Renderer>();

        // Создаём уникальную копию материала, чтобы каждый объект был независим
        if (Application.isPlaying)
        {
            rend.material = new Material(rend.material);
        }

        if (rend.material.HasProperty("_NoiseOffset"))
        {
            float x = Random.Range(0f, maxRandomOffset);
            float y = Random.Range(0f, maxRandomOffset);
            rend.material.SetVector("_NoiseOffset", new Vector4(x, y, 0, 0));
        }
        else
        {
            Debug.LogWarning("Material doesn't have _NoiseOffset property!", gameObject);
        }
    }
}
