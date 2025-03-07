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
        rb.velocity = new Vector2(0, textRollSpeed);        //TF2 damage popups already start moving from the beginning
        currenTextColor = textMesh.color;
    }

    private void Update()
    {
        FadeAwayText();
    }

    /// <summary>
    /// Based on TF2 damage popups, rise slowly then after some time starts to fade away.
    /// </summary>
    private void FadeAwayText()
    {
        fadeAwayTimer -= Time.deltaTime;
        if (fadeAwayTimer < 0)
        {
            currenTextColor.a -= fadeAwaySpeed * Time.deltaTime;    //Decrease alpha, making it transparent
            textMesh.color = currenTextColor;
            if (currenTextColor.a < 0)
            {
                Destroy(rb.gameObject);
            }
        }
    }

    public void SetText(int damageAmout)
    {
        float textSizeModifier = 0f;
        if (damageAmout > 0)
            textSizeModifier += damageAmout / 25;  //Increase or decrease font size based on damage. just works. 20 is an arbitrary number

        textMesh.SetText($"-{damageAmout}");
        textMesh.fontSize += textSizeModifier;
    }
}
