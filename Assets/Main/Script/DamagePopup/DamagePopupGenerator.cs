using UnityEngine;

public class DamagePopupGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject damageUI;
    [SerializeField]
    private GameObject buffUI;

    public void Damage(int Dame)
    {
        GameObject obj = Instantiate(
            damageUI,
            transform.position + new Vector3(
                Random.Range(-1f, 1f),
                1f,
                Random.Range(-1f, 1f)),
            Quaternion.identity
        );

        DamagePopupUI popup = obj.GetComponent<DamagePopupUI>();

        popup.damage = Dame;
    }
    public void Buff(string name, int value)
    {
        GameObject obj = Instantiate(
            buffUI,
            transform.position + new Vector3(
                Random.Range(-1f, 1f),
                1f,
                Random.Range(-1f, 1f)),
            Quaternion.identity
        );

        BuffPopUpUI popup = obj.GetComponent<BuffPopUpUI>();

        popup.SetData(name, value);
    }
}
