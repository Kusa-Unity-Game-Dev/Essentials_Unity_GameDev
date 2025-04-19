using UnityEngine;
using UnityEngine.UI;

public class ButtonHelper : MonoBehaviour
{
    private Button m_button;
    public string SoundId;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_button = GetComponent<Button>();
        m_button.onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        SoundManager.PlaySound(SoundId);
    }


}
