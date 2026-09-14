using UnityEngine;
using UnityEngine.UI;

public class KeyPushImage : MonoBehaviour
{
    [SerializeField] Image AKeyImage;
    [SerializeField] Image WKeyImage;
    [SerializeField] Image SKeyImage;
    [SerializeField] Image DKeyImage;
    [SerializeField] Image QKeyImage;
    [SerializeField] Image RKeyImage;
    [SerializeField] Image ShiftKeyImage;
    [SerializeField] Image LMouseImage;
    [SerializeField] Image RMouseImage;
    private void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            AKeyImage.color = Color.red;
        }
        else
        {
            AKeyImage.color = Color.white;
        }
        if (Input.GetKey(KeyCode.W))
        {
            WKeyImage.color = Color.red;
        }
        else
        {
            WKeyImage.color = Color.white;
        }
        if (Input.GetKey(KeyCode.S))
        {
            SKeyImage.color = Color.red;
        }
        else
        {
            SKeyImage.color = Color.white;
        }
        if (Input.GetKey(KeyCode.D))
        {
            DKeyImage.color = Color.red;
        }
        else
        {
            DKeyImage.color = Color.white;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            QKeyImage.color = Color.red;
        }
        else
        {
            QKeyImage.color = Color.white;
        }
        if (Input.GetKey(KeyCode.R))
        {
            RKeyImage.color = Color.red;
        }
        else
        {
            RKeyImage.color = Color.white;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            ShiftKeyImage.color = Color.red;
        }
        else
        {
            ShiftKeyImage.color = Color.white;
        }
        if (Input.GetMouseButton(0))
        {
            LMouseImage.color = Color.red;
        }
        else
        {
            LMouseImage.color = Color.white;
        }
        if (Input.GetMouseButton(1))
        {
            RMouseImage.color = Color.red;
        }
        else
        {
            RMouseImage.color = Color.white;
        }
    }
}
