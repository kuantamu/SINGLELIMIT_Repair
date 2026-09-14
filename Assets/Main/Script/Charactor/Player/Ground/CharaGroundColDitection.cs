using UnityEngine;

public class CharaGroundColDitection : MonoBehaviour
{
    [SerializeField]
    private bool isGround = true;

    public bool GetGround() {  return isGround; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Ground")
        {
            isGround = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.tag == "Ground")
        {
            isGround = false;
        }
    }
}
