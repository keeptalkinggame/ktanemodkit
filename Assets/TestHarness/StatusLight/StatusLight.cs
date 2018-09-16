using UnityEngine;
using System.Collections;

public class StatusLight : MonoBehaviour
{

    public GameObject InactiveLight;
    public GameObject StrikeLight;
    public GameObject PassLight;

    public void SetPass()
    {
        StopAllCoroutines();
        InactiveLight.SetActive(false);
        PassLight.SetActive(true);
        StrikeLight.SetActive(false);
    }

    public void SetInActive()
    {
        StopAllCoroutines();
        InactiveLight.SetActive(true);
        PassLight.SetActive(false);
        StrikeLight.SetActive(false);
    }

    public void FlashStrike()
    {
        if (PassLight.activeSelf) return;
        StopAllCoroutines();
        InactiveLight.SetActive(true);
        StrikeLight.SetActive(false);
        if (gameObject.activeInHierarchy)
            StartCoroutine(StrikeFlash(1f));
    }

    protected IEnumerator StrikeFlash(float blinkTime)
    {
        StrikeLight.SetActive(true);
        InactiveLight.SetActive(false);
        yield return new WaitForSeconds(blinkTime);
        StrikeLight.SetActive(false);
        InactiveLight.SetActive(true);
    }
}
