using UnityEngine;

public enum EBGMType
{
    City,
    Combat
}

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource _bgmAudio;
    [SerializeField]
    private AudioClip[] _bgmClips;

    [SerializeField]
    private AudioSource _sfxAudio;
    [SerializeField]
    private AudioClip[] _sfxClips;


    void Start()
    {
        _bgmAudio.clip = _bgmClips[0];
        _bgmAudio.loop = true;
        _bgmAudio.Play();
    }

    public void PlayBGM(EBGMType p_bgmType)
    {
        switch (p_bgmType)
        {
            case EBGMType.City:
                _bgmAudio.clip = _bgmClips[0];
                _bgmAudio.Play();
                break;
            case EBGMType.Combat:
                _bgmAudio.clip = _bgmClips[0];
                _bgmAudio.Play();
                break;
            default:
                break;
        }
    }

    public void PlaySFX(int index)
    {
        _sfxAudio.PlayOneShot(_sfxClips[index]);
    }
}
