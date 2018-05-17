using Nes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NESEmulator
{
    public class Framerate : MonoBehaviour
    {
        [SerializeField]
        private float _updateTime = 1f;
        [SerializeField]
        private TextMeshProUGUI _text;

        private float _nextUpdate;
        private long _lastFrameCount;

        private Ppu _ppu;

        private void Start()
        {
            _nextUpdate = _updateTime;
        }

        private void Update()
        {
            if (_ppu == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;

            _nextUpdate -= deltaTime;
            if (_nextUpdate > 0)
            {
                return;
            }

            var currentFrame = _ppu.TotalFrames;

            _text.text = $"{(int)((currentFrame - _lastFrameCount) / _updateTime)} FPS";

            _lastFrameCount = currentFrame;
            _nextUpdate = _updateTime;
        }

        public void StartFramerate(Ppu newPpu)
        {
            _ppu = newPpu;
        }
    }
}