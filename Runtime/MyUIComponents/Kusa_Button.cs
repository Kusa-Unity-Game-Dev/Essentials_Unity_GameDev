using UnityEngine;
using UnityEngine.UI;


[AddComponentMenu("KUSA_UI/Button2", 30)]
public class Kusa_Button : Button
{
    //[SerializeField]
    public string SoundId;

    protected override void Start()
    {
        base.Start();
        onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        SoundManager.PlaySound(SoundId);
    }
}

