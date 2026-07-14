using TMPro;
using UnityEngine;
using CarFactoryIdle.Core;

public class UIManager : MonoBehaviour
{
    public GameRoot gameRoot;

    public TMP_Text cashText;

    void Start()
    {
        Debug.Log("===== UI MANAGER =====");

        Debug.Log(gameRoot);

        Debug.Log(gameRoot.State);

        Debug.Log(gameRoot.State.wallet.cash);

        cashText.text = "$" + gameRoot.State.wallet.cash;
        //RefreshCash();
    }

    public void RefreshCash()
    {
        cashText.text = "$" + gameRoot.State.wallet.cash.ToString();
    }
}