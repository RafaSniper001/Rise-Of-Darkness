using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// realce dos botões (junto da troca de sprite do Button): com o mouse em cima ou selecionado
// pelo teclado/controle, o botão cresce um pouco e o texto clareia. Usa o tempo sem escala
// porque o menu de pausa roda com Time.timeScale = 0
public class UIButtonFX : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public float hoverScale = 1.04f;
    public Color normalTextColor = new Color(0.82f, 0.88f, 1f);
    public Color hoverTextColor = Color.white;
    public float speed = 14f;

    TMP_Text label;
    Vector3 baseScale;
    bool pointerOver;
    bool selected;
    float blend;

    void Awake()
    {
        label = GetComponentInChildren<TMP_Text>();
        baseScale = transform.localScale;
    }

    void OnDisable()
    {
        pointerOver = false;
        selected = false;
        blend = 0f;
        Apply();
    }

    void Update()
    {
        float target = pointerOver || selected ? 1f : 0f;
        if (Mathf.Approximately(blend, target))
            return;

        blend = Mathf.MoveTowards(blend, target, speed * Time.unscaledDeltaTime);
        Apply();
    }

    void Apply()
    {
        transform.localScale = baseScale * Mathf.Lerp(1f, hoverScale, blend);
        if (label != null)
            label.color = Color.Lerp(normalTextColor, hoverTextColor, blend);
    }

    public void OnPointerEnter(PointerEventData eventData) { pointerOver = true; }
    public void OnPointerExit(PointerEventData eventData) { pointerOver = false; }
    public void OnSelect(BaseEventData eventData) { selected = true; }
    public void OnDeselect(BaseEventData eventData) { selected = false; }
}
