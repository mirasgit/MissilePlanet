using UnityEngine;

namespace MissilePlanet
{
    /// <summary>
    /// Non-blocking onboarding. Tips are shown once per installation/save and never stop gameplay.
    /// </summary>
    public sealed class TutorialHints : MonoBehaviour
    {
        private MissilePlanetGame _game;
        private string _message;
        private float _hideAt;
        private int _lastEvaluatedWave = -1;

        public string CurrentMessage => Time.unscaledTime < _hideAt ? _message : string.Empty;
        public bool HasMessage => !string.IsNullOrEmpty(CurrentMessage);

        private void Awake()
        {
            _game = GetComponent<MissilePlanetGame>();
        }

        private void Update()
        {
            if (_game == null || !_game.IsStarted || _game.IsGameOver || _game.IsPaused || _game.IsShopOpen)
                return;

            if (_game.Wave == _lastEvaluatedWave)
                return;

            _lastEvaluatedWave = _game.Wave;
            EvaluateWave(_game.Wave);
        }

        private void EvaluateWave(int wave)
        {
            if (wave == 1) ShowOnce(0, "TAP EMPTY SPACE TO LAUNCH A MISSILE");
            else if (wave == 2) ShowOnce(1, "TAP THE PLANET TO RELEASE YOUR PULSE");
            else if (wave == 5) ShowOnce(2, "GLOWING CARRIER: DESTROY IT TO GAIN A PULSE CHARGE");
            else if (wave == 6) ShowOnce(3, "RED ASTEROIDS BLINK BEFORE THEY SPLIT — INTERCEPT EARLY");
            else if (wave == 10) ShowOnce(4, "BOSS ENGINES TAKE DAMAGE ONLY WHILE OVERHEATED");
            else if (wave == 11) ShowOnce(5, "GREEN ASTEROIDS NEED MULTIPLE HITS");
            else if (wave == 21) ShowOnce(6, "YELLOW DODGER IS FASTER THAN YOUR MISSILE — LEAD THE TARGET");
        }

        private void ShowOnce(int bit, string text)
        {
            if (GameSave.HasSeenTip(bit))
                return;

            _message = text;
            _hideAt = Time.unscaledTime + 6.5f;
            GameSave.MarkTipSeen(bit);
        }
    }
}
