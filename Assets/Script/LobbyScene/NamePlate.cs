using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NamePlate : MonoBehaviour
{
    public Text UI;
    public Text name;

    public void SetPlate(bool active, string data="")
    {
        UI.gameObject.SetActive(active);
        name.text = data;
    }
}
