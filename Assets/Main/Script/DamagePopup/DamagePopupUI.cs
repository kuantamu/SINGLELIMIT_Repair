using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DamagePopupUI : MonoBehaviour
{
    public int damage;
    [SerializeField]
    private TMP_Text damageText;
    float count = 0f;
    //　フェードアウトするスピード
    private float fadeOutSpeed = 1f;
    //　移動値
    [SerializeField]
    private float moveSpeed = 1.5f;

    void Awake()
    {
        
    }
    private void Start()
    {
        damageText.fontSize = 100;
        if (damage > 0)
        {
            damageText.color = Color.red;
        }
        else if (damage < 0)
        {
            damageText.color = Color.green;
        }else
        {
            damageText.text = "Miss";
            return;
        }
        damageText.text = damage.ToString();
    }

    void LateUpdate()
    {
        transform.rotation = Camera.main.transform.rotation;
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        count += fadeOutSpeed * Time.deltaTime;

        if (count >= 1)
        {
            Destroy(gameObject);
        }
    }
}
