using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
    public static Chat instance;

	public InputField SendInput;
	public RectTransform ChatContent;
	public Text ChatText;
	public ScrollRect ChatScrollRect;

	private void Awake() {
		instance = this;
		SetupOnSubmit();
	}

	public void ShowMessage(string data)
	{
		ChatText.text += ChatText.text == "" ? data : "\n" + data;
		
		Fit(ChatText.GetComponent<RectTransform>());
		Fit(ChatContent);
		Invoke("ScrollDelay", 0.03f);
	}

	private void SetupOnSubmit()
    {
        // SendInput의 onEndEdit 이벤트에 Client.Instance.OnSendButton 연결
        if (SendInput != null)
        {
            SendInput.onEndEdit.RemoveAllListeners();
            
            SendInput.onEndEdit.AddListener(delegate
            {
                if (!string.IsNullOrWhiteSpace(SendInput.text) && Client.Instance != null)
                {
                    Client.Instance.OnSendButton(SendInput);
                }
            });
        }
    }


	void Fit(RectTransform Rect) => LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);

	void ScrollDelay() => ChatScrollRect.verticalScrollbar.value = 0;
}
