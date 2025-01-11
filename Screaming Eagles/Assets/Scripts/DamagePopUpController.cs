using TMPro;
using UnityEngine;

public class DamagePopUpController : MonoBehaviour
{
    private Rigidbody2D rb;
    private TextMeshPro textMesh;

    [SerializeField] private float textRollSpeed = 0.05f;
    [SerializeField] private float fadeAwayTimer = 0.5f;
    [SerializeField] private float fadeAwaySpeed = 5f;

    private Color currenTextColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        textMesh = GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        rb.velocity = new Vector2(0, textRollSpeed);
        currenTextColor = textMesh.color;
    }

    private void Update()
    {
        FadeAwayText();
    }

    /// <summary>
    /// After determined ammount of time, makes text slowly fade away and then destroys the game object. 
    /// </summary>
    private void FadeAwayText()
    {
        fadeAwayTimer -= Time.deltaTime;
        if (fadeAwayTimer < 0)
        {
            currenTextColor.a -= fadeAwaySpeed * Time.deltaTime;
            textMesh.color = currenTextColor;
            if (currenTextColor.a < 0)
            {
                Destroy(rb.gameObject);
            }
        }
    }

    public void SetText(int damageAmout)
    {
        textMesh.SetText($"-{damageAmout}");
    }
}
