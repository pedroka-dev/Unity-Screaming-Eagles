using TMPro;
using UnityEngine;

public class CritialHitPopUpController : MonoBehaviour
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
        currenTextColor = textMesh.color;
    }



    private void Update()
    {
        FadeAwayCrit();
    }

    /// <summary>
    /// Based on TF2 crit popups, shrinks and the start roll up while fading away and decreasing its green color./// </summary>
    private void FadeAwayCrit()
    {
        fadeAwayTimer -= Time.deltaTime;
        if (fadeAwayTimer < 0)
        {
            rb.velocity = new Vector2(0, textRollSpeed);        //TF2 damage popups already start moving from the beginning
            currenTextColor.a -= fadeAwaySpeed * Time.deltaTime;    //Decrease alpha, making it transparent
            currenTextColor.g -= fadeAwaySpeed * Time.deltaTime * 5;    //Decrease green, making it brown-ish. just works. The value 5 is an arbitrary number
            textMesh.color = currenTextColor;
            if (currenTextColor.a < 0)
            {
                Destroy(rb.gameObject);
            }
        }
        else
        {
            textMesh.fontSize -= Time.deltaTime * 10;       //Decreases text size if its not yet time to fade away. . just works. The value 10 is an arbitrary number
        }
    }
}
