using UnityEngine;

namespace Liuruan
{
    /// UI 音效＋全域靜音（PlayerPrefs 記住）。環境音與配樂是場景裡各自 loop 的 AudioSource。
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }
        const string MuteKey = "liuruan_mute";

        public AudioClip whoosh, correct, wrong;
        AudioSource sfx;

        public bool Muted { get; private set; }

        void Awake()
        {
            I = this;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            Muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            AudioListener.volume = Muted ? 0 : 1;
        }

        public static void Play(AudioClip clip, float volume = 0.8f)
        {
            if (I && clip) I.sfx.PlayOneShot(clip, volume);
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0 : 1;
            PlayerPrefs.SetInt(MuteKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
