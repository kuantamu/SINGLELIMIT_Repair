using UnityEngine;
using UnityEngine.UI;

public class HPGageGager : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private float currentHP;
    private float maxHP;

    //‰¼’u‚«
    [SerializeField]
    private CharactorStatus charactorStatus;

    private void Update()
    {
        currentHP = charactorStatus.statusBase.CurrentHP;
        maxHP = charactorStatus.statusBase.MaxHP;
        slider.value = currentHP / maxHP;
    }

    public void SetHPBar(float currentHP, float maxHP)
    {
        slider.value = currentHP / maxHP;
    }
}
