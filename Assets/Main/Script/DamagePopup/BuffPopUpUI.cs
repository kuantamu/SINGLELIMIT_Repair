using UnityEngine;
using TMPro;

public class BuffPopUpUI : MonoBehaviour
{
    public string buffName;
    public int buffValue;
    [SerializeField]
    private TMP_Text buffText;
    float count = 0f;
    //　フェードアウトするスピード
    private float fadeOutSpeed = 1f;
    //　移動値
    [SerializeField]
    private float moveSpeed = 1.5f;

    public void SetData(string name, int value)
    {
        buffName = name;
        buffValue = value;
    }

    private void Start()
    {
        buffText.fontSize = 100;
        buffText.text = buffName + buffValue.ToString();
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
