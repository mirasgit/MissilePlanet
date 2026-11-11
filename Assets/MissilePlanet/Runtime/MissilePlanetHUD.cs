using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MissilePlanet
{
    public sealed class MissilePlanetHUD : MonoBehaviour
    {
        private enum MenuPage { Main, Cosmetics, CoreStore, Settings }
        private enum CosmeticCategory { None, Planets, Rockets, Explosions, Asteroids, Space }

        [SerializeField] private MissilePlanetGame _game;

        private MenuPage _menuPage = MenuPage.Main;
        private bool _coreStoreOpenedFromGameplay;
        private bool _settingsOpenedFromGameplay;
        private TutorialHints _tutorial;
        private CosmeticCategory _cosmeticCategory = CosmeticCategory.None;
        private int _cosmeticScrollIndex;
        private float _cosmeticTouchScrollAccumulator;

#if ENABLE_INPUT_SYSTEM
        // Event-driven pointer capture is more robust under Device Simulator than
        // polling only primaryTouch.wasPressedThisFrame during rapid clicking.
        private InputAction _touchPressAction;
        private InputAction _mousePressAction;
        private InputAction _backAction;
        private bool _backRequested;
        private bool _hasQueuedPointerDown;
        private Vector2 _queuedPointerPosition;
        private int _lastQueuedFrame = -1;
        private double _lastQueuedTime = -10d;
#endif

        private GUIStyle _label;
        private GUIStyle _small;
        private GUIStyle _value;
        private GUIStyle _title;
        private GUIStyle _button;
        private GUIStyle _buttonSmall;
        private GUIStyle _description;
        private GUIStyle _ammo;
        private Texture2D _panel;
        private Texture2D _panelDark;
        private Texture2D _accent;
        private Texture2D _overlay;
        private Texture2D _shieldIcon;
        private Texture2D _coinIcon;
        private Texture2D _coreIcon;

        private Rect _pauseRect;
        private Rect _pulseRect;
        private Rect _coreChipRect;
        private bool _hasPauseRect;
        private bool _hasPulseRect;
        private bool _hasCoreChipRect;

        private void Awake()
        {
            _tutorial = GetComponent<TutorialHints>();
        }

        public void SetGame(MissilePlanetGame game)
        {
            _game = game;
            if (_tutorial == null) _tutorial = GetComponent<TutorialHints>();
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _touchPressAction = new InputAction("MissilePlanet Touch Press", InputActionType.PassThrough, "<Touchscreen>/primaryTouch/press");
            _mousePressAction = new InputAction("MissilePlanet Mouse Press", InputActionType.PassThrough, "<Mouse>/leftButton");
            _backAction = new InputAction("MissilePlanet Back", InputActionType.Button, "<Keyboard>/escape");

            _touchPressAction.performed += OnTouchPress;
            _mousePressAction.performed += OnMousePress;
            _backAction.performed += OnBackPress;
            _touchPressAction.Enable();
            _mousePressAction.Enable();
            _backAction.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (_touchPressAction != null)
            {
                _touchPressAction.performed -= OnTouchPress;
                _touchPressAction.Disable();
                _touchPressAction.Dispose();
                _touchPressAction = null;
            }

            if (_mousePressAction != null)
            {
                _mousePressAction.performed -= OnMousePress;
                _mousePressAction.Disable();
                _mousePressAction.Dispose();
                _mousePressAction = null;
            }

            if (_backAction != null)
            {
                _backAction.performed -= OnBackPress;
                _backAction.Disable();
                _backAction.Dispose();
                _backAction = null;
            }

            _backRequested = false;
            _hasQueuedPointerDown = false;
#endif
        }

#if ENABLE_INPUT_SYSTEM

        private void OnBackPress(InputAction.CallbackContext context)
        {
            _backRequested = true;
        }

        private void OnTouchPress(InputAction.CallbackContext context)
        {
            if (context.ReadValue<float>() < 0.5f || Touchscreen.current == null)
                return;

            QueuePointerDown(Touchscreen.current.primaryTouch.position.ReadValue(), context.time);
        }

        private void OnMousePress(InputAction.CallbackContext context)
        {
            if (context.ReadValue<float>() < 0.5f || Mouse.current == null)
                return;

            Vector2 position = Mouse.current.position.ReadValue();

#if UNITY_EDITOR
            // Device Simulator translates the editor mouse into a virtual touch.
            // If that virtual touch ever gets stuck during rapid clicking, the real
            // mouse event still acts as a recovery path, while we keep simulator
            // coordinates when they are available.
            if (Touchscreen.current != null)
            {
                Vector2 simulatedTouchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                if (simulatedTouchPosition.sqrMagnitude > 0.01f)
                    position = simulatedTouchPosition;
            }
#endif

            QueuePointerDown(position, context.time);
        }

        private void QueuePointerDown(Vector2 position, double eventTime)
        {
            // Device Simulator can report the same physical click once as mouse and
            // once as touch. Collapse only those near-simultaneous duplicates.
            bool duplicateSameFrame = _lastQueuedFrame == Time.frameCount;
            bool duplicateVeryClose = eventTime - _lastQueuedTime >= 0d && eventTime - _lastQueuedTime < 0.012d;

            if ((duplicateSameFrame || duplicateVeryClose) && _hasQueuedPointerDown)
            {
                _queuedPointerPosition = position;
                return;
            }

            // Keep at most one pending tap. Under an input burst we intentionally
            // keep the newest tap instead of building a backlog that executes later.
            _queuedPointerPosition = position;
            _hasQueuedPointerDown = true;
            _lastQueuedFrame = Time.frameCount;
            _lastQueuedTime = eventTime;
        }
#endif

        private void Update()
        {
            if (_game == null)
                return;

            HandleCosmeticContinuousScroll();

#if ENABLE_INPUT_SYSTEM
            if (_backRequested)
            {
                _backRequested = false;
                HandleBackPressed();
                return;
            }

            if (!_hasQueuedPointerDown)
                return;

            Vector2 screenPosition = _queuedPointerPosition;
            _hasQueuedPointerDown = false;
            HandlePointerDown(screenPosition);
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackPressed();
                return;
            }

            if (!TryReadPointerDownLegacy(out Vector2 screenPosition))
                return;
            HandlePointerDown(screenPosition);
#endif
        }


        private void HandleBackPressed()
        {
            if (_game == null) return;

            if (_menuPage == MenuPage.Settings)
            {
                _menuPage = MenuPage.Main;
                if (_settingsOpenedFromGameplay)
                {
                    _settingsOpenedFromGameplay = false;
                    // The game stays paused; the pause overlay becomes visible again.
                }
                return;
            }

            if (_menuPage == MenuPage.Cosmetics && !_game.IsStarted)
            {
                if (_cosmeticCategory != CosmeticCategory.None)
                {
                    _cosmeticCategory = CosmeticCategory.None;
                    _cosmeticScrollIndex = 0;
                }
                else
                {
                    _menuPage = MenuPage.Main;
                }
                return;
            }

            if (_menuPage == MenuPage.CoreStore)
            {
                if (_coreStoreOpenedFromGameplay)
                {
                    _coreStoreOpenedFromGameplay = false;
                    _menuPage = MenuPage.Main;
                    if (_game.IsPaused)
                        _game.ResumeGame();
                }
                else
                {
                    _menuPage = MenuPage.Main;
                }
                return;
            }

            if (_game.IsPaused)
            {
                _game.ResumeGame();
                return;
            }

            if (_game.IsStarted && !_game.IsGameOver && !_game.IsShopOpen)
            {
                _game.PauseGame();
                return;
            }

#if !UNITY_EDITOR
            if (!_game.IsStarted && _menuPage == MenuPage.Main)
                Application.Quit();
#endif
        }

        private void HandleCosmeticContinuousScroll()
        {
            if (_menuPage != MenuPage.Cosmetics || _game.IsStarted || _cosmeticCategory == CosmeticCategory.None)
            {
                _cosmeticTouchScrollAccumulator = 0f;
                return;
            }

            int itemCount = GetCosmeticItemCount(_cosmeticCategory);
            int maxScroll = Mathf.Max(0, itemCount - 5);
            if (maxScroll <= 0)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                float wheel = Mouse.current.scroll.ReadValue().y;
                if (wheel > 0.1f) _cosmeticScrollIndex = Mathf.Max(0, _cosmeticScrollIndex - 1);
                else if (wheel < -0.1f) _cosmeticScrollIndex = Mathf.Min(maxScroll, _cosmeticScrollIndex + 1);
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                _cosmeticTouchScrollAccumulator += Touchscreen.current.primaryTouch.delta.ReadValue().y;
                float threshold = Mathf.Max(45f, Screen.height * 0.035f);
                if (_cosmeticTouchScrollAccumulator > threshold)
                {
                    _cosmeticScrollIndex = Mathf.Min(maxScroll, _cosmeticScrollIndex + 1);
                    _cosmeticTouchScrollAccumulator = 0f;
                }
                else if (_cosmeticTouchScrollAccumulator < -threshold)
                {
                    _cosmeticScrollIndex = Mathf.Max(0, _cosmeticScrollIndex - 1);
                    _cosmeticTouchScrollAccumulator = 0f;
                }
            }
#else
            float wheel = Input.mouseScrollDelta.y;
            if (wheel > 0.1f) _cosmeticScrollIndex = Mathf.Max(0, _cosmeticScrollIndex - 1);
            else if (wheel < -0.1f) _cosmeticScrollIndex = Mathf.Min(maxScroll, _cosmeticScrollIndex + 1);

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                _cosmeticTouchScrollAccumulator += Input.GetTouch(0).deltaPosition.y;
                float threshold = Mathf.Max(45f, Screen.height * 0.035f);
                if (_cosmeticTouchScrollAccumulator > threshold)
                {
                    _cosmeticScrollIndex = Mathf.Min(maxScroll, _cosmeticScrollIndex + 1);
                    _cosmeticTouchScrollAccumulator = 0f;
                }
                else if (_cosmeticTouchScrollAccumulator < -threshold)
                {
                    _cosmeticScrollIndex = Mathf.Max(0, _cosmeticScrollIndex - 1);
                    _cosmeticTouchScrollAccumulator = 0f;
                }
            }
#endif
        }

        public bool IsPauseButtonScreenPoint(Vector2 screenPosition) => ContainsScreenPoint(_hasPauseRect, _pauseRect, screenPosition);
        public bool IsPulseIconScreenPoint(Vector2 screenPosition) => ContainsScreenPoint(_hasPulseRect, _pulseRect, screenPosition);
        public bool IsBlockingScreenPoint(Vector2 screenPosition)
        {
            if (IsPauseButtonScreenPoint(screenPosition) || IsPulseIconScreenPoint(screenPosition))
                return true;
            return ContainsScreenPoint(_hasCoreChipRect, _coreChipRect, screenPosition);
        }

        private static bool ContainsScreenPoint(bool enabled, Rect guiRect, Vector2 screenPosition)
        {
            if (!enabled) return false;
            Vector2 gui = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return guiRect.Contains(gui);
        }

        private void HandlePointerDown(Vector2 screenPosition)
        {
            Vector2 p = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            Rect safe = SafeRect();

            if (_menuPage == MenuPage.CoreStore)
            {
                HandleCoreStoreInput(p, safe);
                return;
            }

            if (_menuPage == MenuPage.Settings)
            {
                HandleSettingsInput(p, safe);
                return;
            }

            if (!_game.IsStarted)
            {
                if (_menuPage == MenuPage.Cosmetics)
                {
                    HandleCosmeticsInput(p, safe);
                    return;
                }

                if (GetCoreChipRect(safe).Contains(p))
                {
                    _menuPage = MenuPage.CoreStore;
                    _coreStoreOpenedFromGameplay = false;
                    return;
                }
                if (GetMainPlayRect(safe).Contains(p)) { _game.StartGame(); return; }
                if (GetMainCosmeticsRect(safe).Contains(p)) { _cosmeticCategory = CosmeticCategory.None; _cosmeticScrollIndex = 0; _menuPage = MenuPage.Cosmetics; return; }
                if (GetMainSettingsRect(safe).Contains(p)) { _settingsOpenedFromGameplay = false; _menuPage = MenuPage.Settings; return; }
                if (GetMainAdCoreRect(safe).Contains(p)) { _game.RequestRewardedCore(); return; }
                return;
            }

            if (_game.IsGameOver)
            {
                if (GetCoreChipRect(safe).Contains(p))
                {
                    _menuPage = MenuPage.CoreStore;
                    _coreStoreOpenedFromGameplay = false;
                    return;
                }
                if (GetGameOverAdReviveRect(safe).Contains(p)) { _game.RequestAdRevive(); return; }
                if (GetGameOverCoreReviveRect(safe).Contains(p)) { _game.TryCoreRevive(); return; }
                if (GetGameOverRestartRect(safe).Contains(p)) { _game.RestartGame(); return; }
                if (GetGameOverMenuRect(safe).Contains(p)) { _game.ReturnToMainMenu(); return; }
                return;
            }

            if (_game.IsShopOpen)
            {
                HandleRunShopInput(p, safe);
                return;
            }

            if (_game.IsPaused)
            {
                if (GetResumeRect(safe).Contains(p) || GetPauseRect(safe).Contains(p))
                {
                    _game.ResumeGame();
                    return;
                }
                if (GetPauseSettingsRect(safe).Contains(p))
                {
                    _settingsOpenedFromGameplay = true;
                    _menuPage = MenuPage.Settings;
                    return;
                }
                if (GetPauseMenuRect(safe).Contains(p))
                {
                    _game.ReturnToMainMenu();
                    return;
                }
                return;
            }

            if (GetCoreChipRect(safe).Contains(p))
            {
                _game.PauseGame();
                _menuPage = MenuPage.CoreStore;
                _coreStoreOpenedFromGameplay = true;
                return;
            }
            if (GetPauseRect(safe).Contains(p))
            {
                _game.PauseGame();
                return;
            }

            // UI gets first refusal. Only an unconsumed tap reaches combat.
            _game.HandleGameplayPointer(screenPosition);
        }

        private void HandleSettingsInput(Vector2 p, Rect safe)
        {
            GetSettingsRects(safe, out Rect sfx, out Rect music, out Rect haptics, out Rect shake, out Rect fps, out Rect back);
            if (sfx.Contains(p)) { GameSettings.SfxEnabled = !GameSettings.SfxEnabled; return; }
            if (music.Contains(p)) { GameSettings.MusicEnabled = !GameSettings.MusicEnabled; return; }
            if (haptics.Contains(p)) { GameSettings.HapticsEnabled = !GameSettings.HapticsEnabled; return; }
            if (shake.Contains(p)) { GameSettings.ScreenShakeEnabled = !GameSettings.ScreenShakeEnabled; return; }
            if (fps.Contains(p)) { GameSettings.CycleTargetFps(); return; }
            if (!back.Contains(p)) return;

            _menuPage = MenuPage.Main;
            if (_settingsOpenedFromGameplay)
                _settingsOpenedFromGameplay = false;
        }

        private void HandleRunShopInput(Vector2 p, Rect safe)
        {
            GetRunShopRects(safe, out Rect missiles, out Rect power, out Rect shield, out Rect guardian, out Rect cannon, out Rect cont);
            if (missiles.Contains(p)) { _game.TryBuyMissileUpgrade(); return; }
            if (power.Contains(p)) { _game.TryBuyMissilePowerUpgrade(); return; }
            if (shield.Contains(p)) { _game.TryBuyShieldCharge(); return; }
            if (guardian.Contains(p)) { _game.TryBuyGuardianHelper(); return; }
            if (cannon.Contains(p)) { _game.TryBuyCannonHelper(); return; }
            if (cont.Contains(p)) _game.CloseShop();
        }

        private void HandleCosmeticsInput(Vector2 p, Rect safe)
        {
            GetCosmeticRows(safe, out Rect r1, out Rect r2, out Rect r3, out Rect r4, out Rect r5);
            Rect[] rows = { r1, r2, r3, r4, r5 };

            if (_cosmeticCategory == CosmeticCategory.None)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    if (!rows[i].Contains(p))
                        continue;

                    _cosmeticCategory = (CosmeticCategory)(i + 1);
                    _cosmeticScrollIndex = 0;
                    return;
                }

                GetCosmeticNavRects(safe, out _, out _, out Rect backToMenu);
                if (backToMenu.Contains(p))
                {
                    _cosmeticCategory = CosmeticCategory.None;
                    _cosmeticScrollIndex = 0;
                    _menuPage = MenuPage.Main;
                }
                return;
            }

            int itemCount = GetCosmeticItemCount(_cosmeticCategory);
            for (int visible = 0; visible < rows.Length; visible++)
            {
                int itemIndex = _cosmeticScrollIndex + visible;
                if (itemIndex >= itemCount)
                    break;

                if (GetShopBuyRect(rows[visible]).Contains(p))
                {
                    ActivateCosmeticItem(_cosmeticCategory, itemIndex);
                    return;
                }
            }

            GetCosmeticNavRects(safe, out Rect up, out Rect down, out Rect back);
            if (up.Contains(p))
            {
                _cosmeticScrollIndex = Mathf.Max(0, _cosmeticScrollIndex - 1);
                return;
            }
            if (down.Contains(p))
            {
                int maxScroll = Mathf.Max(0, itemCount - rows.Length);
                _cosmeticScrollIndex = Mathf.Min(maxScroll, _cosmeticScrollIndex + 1);
                return;
            }
            if (back.Contains(p))
            {
                _cosmeticCategory = CosmeticCategory.None;
                _cosmeticScrollIndex = 0;
            }
        }

        private int GetCosmeticItemCount(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.Planets: return 4;
                case CosmeticCategory.Rockets: return 4;
                case CosmeticCategory.Explosions: return 4;
                case CosmeticCategory.Asteroids: return 2;
                case CosmeticCategory.Space: return 4;
                default: return 0;
            }
        }

        private void ActivateCosmeticItem(CosmeticCategory category, int itemIndex)
        {
            switch (category)
            {
                case CosmeticCategory.Planets:
                    if (itemIndex == 0) _game.EquipClassicPlanet();
                    else if (itemIndex == 1) _game.BuyOrEquipMars();
                    else if (itemIndex == 2) _game.BuyOrEquipVenus();
                    else if (itemIndex == 3) _game.BuyOrEquipNeptune();
                    break;
                case CosmeticCategory.Rockets:
                    if (itemIndex == 0) _game.EquipClassicRockets();
                    else if (itemIndex == 1) _game.BuyOrEquipInvokerRockets();
                    else if (itemIndex == 2) _game.BuyOrEquipSolarRockets();
                    else if (itemIndex == 3) _game.BuyOrEquipCryoRockets();
                    break;
                case CosmeticCategory.Explosions:
                    if (itemIndex == 0) _game.EquipClassicExplosion();
                    else if (itemIndex == 1) _game.BuyOrEquipVioletExplosion();
                    else if (itemIndex == 2) _game.BuyOrEquipSolarExplosion();
                    else if (itemIndex == 3) _game.BuyOrEquipEmeraldExplosion();
                    break;
                case CosmeticCategory.Asteroids:
                    if (itemIndex == 0) _game.EquipClassicAsteroids();
                    else if (itemIndex == 1) _game.BuyOrEquipKamikazeAsteroids();
                    break;
                case CosmeticCategory.Space:
                    if (itemIndex == 0) _game.EquipClassicSpace();
                    else if (itemIndex == 1) _game.BuyOrEquipPurpleSpace();
                    else if (itemIndex == 2) _game.BuyOrEquipCrimsonSpace();
                    else if (itemIndex == 3) _game.BuyOrEquipFrozenSpace();
                    break;
            }
        }

        private void HandleCoreStoreInput(Vector2 p, Rect safe)
        {
            GetCoreStoreRects(safe, out Rect pack1, out Rect pack2, out Rect pack3, out Rect back);
            if (pack1.Contains(p)) { _game.PurchaseCorePack(0); return; }
            if (pack2.Contains(p)) { _game.PurchaseCorePack(1); return; }
            if (pack3.Contains(p)) { _game.PurchaseCorePack(2); return; }
            if (!back.Contains(p)) return;

            _menuPage = MenuPage.Main;
            if (_coreStoreOpenedFromGameplay)
            {
                _coreStoreOpenedFromGameplay = false;
                if (_game.IsPaused)
                    _game.ResumeGame();
            }
        }

        private static bool TryReadPointerDownLegacy(out Vector2 position)
        {
#if !ENABLE_INPUT_SYSTEM
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                position = Input.GetTouch(0).position;
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }
#endif
            position = default;
            return false;
        }

        private void OnGUI()
        {
            if (_game == null) return;
            EnsureStyles();
            Rect safe = SafeRect();
            _hasPauseRect = _hasPulseRect = _hasCoreChipRect = false;

            if (_menuPage == MenuPage.Settings)
            {
                DrawSettings(safe);
                return;
            }

            if (_menuPage == MenuPage.CoreStore)
            {
                DrawCoreStore(safe);
                return;
            }

            if (!_game.IsStarted)
            {
                if (_menuPage == MenuPage.Cosmetics)
                    DrawCosmetics(safe);
                else
                    DrawMainMenu(safe);
                return;
            }

            if (_game.IsGameOver)
            {
                DrawCoreChip(safe);
                DrawGameOver(safe);
                return;
            }

            if (_game.IsShopOpen)
            {
                DrawTopStats(safe);
                DrawEconomyChips(safe, false);
                DrawRunShop(safe);
                return;
            }

            DrawTopStats(safe);
            DrawPauseButton(safe);
            DrawEconomyChips(safe, true);
            DrawAmmoInsidePlanet();
            if (_game.PlanetPulseAvailable) DrawPulseIcon(safe);
            if (!_game.IsPaused) DrawTutorialHint(safe);

            if (_game.IsPaused)
                DrawPauseOverlay(safe);
        }

        private void DrawMainMenu(Rect safe)
        {
            DrawCoreChip(safe);
            float w = Mathf.Min(safe.width * 0.82f, 760f);
            float h = Mathf.Min(safe.height * 0.56f, 820f);
            Rect panel = new Rect(safe.center.x - w / 2f, safe.y + safe.height * 0.16f, w, h);
            Panel(panel);
            GUI.Label(new Rect(panel.x, panel.y + h * 0.08f, w, h * 0.16f), "MISSILE PLANET", _title);
            GUI.Label(new Rect(panel.x, panel.y + h * 0.24f, w, h * 0.09f), $"BEST  {_game.BestScore:000000}", _value);
            BoxButton(GetMainPlayRect(safe), "PLAY", true);
            BoxButton(GetMainCosmeticsRect(safe), "COSMETICS", true);
            BoxButton(GetMainSettingsRect(safe), "SETTINGS", true);
            BoxButton(GetMainAdCoreRect(safe), _game.RewardedAdAvailable ? "+1 CORE  •  WATCH AD" : "REWARDED SDK NOT CONNECTED", _game.RewardedAdAvailable);
            GUI.Label(new Rect(panel.x, panel.yMax - h * 0.055f, w, h * 0.04f), "DEFEND  •  ADAPT  •  SURVIVE", _small);
        }

        private void DrawTopStats(Rect safe)
        {
            float pad = Mathf.Max(12f, Screen.width * 0.024f);
            float gap = Mathf.Max(7f, Screen.width * 0.012f);
            float pause = PauseSize();
            float h = Mathf.Clamp(Screen.height * 0.061f, 74f, 118f);
            float available = safe.width - pad * 2f - pause - gap * 3f;
            float cw = available / 3f;
            float y = safe.y + pad;
            DrawStat(new Rect(safe.x + pad, y, cw, h), "SCORE", _game.Score.ToString("000000"));
            DrawStat(new Rect(safe.x + pad + cw + gap, y, cw, h), "WAVE", _game.Wave.ToString("00"));
            DrawStat(new Rect(safe.x + pad + (cw + gap) * 2f, y, cw, h), "BEST", _game.BestScore.ToString("000000"));
        }

        private void DrawStat(Rect r, string label, string value)
        {
            GUI.DrawTexture(r, _panel, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, Mathf.Max(2f, Screen.height * 0.0016f)), _accent);
            GUI.Label(new Rect(r.x, r.y + r.height * 0.10f, r.width, r.height * 0.30f), label, _small);
            GUI.Label(new Rect(r.x, r.y + r.height * 0.36f, r.width, r.height * 0.52f), value, _value);
        }

        private void DrawEconomyChips(Rect safe, bool allowCoreStore)
        {
            float pad = Mathf.Max(12f, Screen.width * 0.024f);
            float y = safe.y + Mathf.Clamp(Screen.height * 0.061f, 74f, 118f) + pad * 1.45f;
            float h = Mathf.Clamp(Screen.height * 0.034f, 42f, 62f);
            float w = Mathf.Clamp(Screen.width * 0.23f, 130f, 235f);
            Rect coins = new Rect(safe.x + pad, y, w, h);
            GUI.DrawTexture(coins, _panelDark, ScaleMode.StretchToFill);
            GUI.Label(coins, $"●  {_game.Coins}", _small);

            DrawCoreChip(safe, y, allowCoreStore);
        }

        private void DrawCoreChip(Rect safe, float overrideY = -1f, bool clickable = true)
        {
            Rect r = GetCoreChipRect(safe, overrideY);
            _coreChipRect = r;
            _hasCoreChipRect = clickable;
            GUI.DrawTexture(r, _panelDark, ScaleMode.StretchToFill);
            GUI.Label(new Rect(r.x + r.width * 0.05f, r.y, r.width * 0.72f, r.height), $"◆  {_game.Cores}", _small);
            GUI.Label(new Rect(r.x + r.width * 0.74f, r.y, r.width * 0.24f, r.height), "+", _value);
        }

        private void DrawPauseButton(Rect safe)
        {
            _pauseRect = GetPauseRect(safe);
            _hasPauseRect = true;
            BoxButton(_pauseRect, "II", true, true);
        }

        private void DrawAmmoInsidePlanet()
        {
            Camera camera = _game.GameCamera;
            if (camera == null) return;
            Vector3 s = camera.WorldToScreenPoint(_game.PlanetPosition);
            if (s.z < 0f) return;
            float w = Mathf.Clamp(Screen.width * 0.22f, 110f, 260f);
            float h = Mathf.Clamp(Screen.height * 0.07f, 80f, 140f);
            Rect r = new Rect(s.x - w / 2f, Screen.height - s.y - h / 2f, w, h);
            GUI.Label(r, _game.DebugInfiniteMissiles ? "∞" : _game.Ammo.ToString(), _ammo);
        }

        private void DrawPulseIcon(Rect safe)
        {
            float size = Mathf.Clamp(Screen.width * 0.105f, 60f, 116f);
            float pad = Mathf.Max(16f, Screen.width * 0.028f);
            _pulseRect = new Rect(safe.xMax - pad - size, safe.yMax - pad - size, size, size);
            _hasPulseRect = true;
            GUI.DrawTexture(_pulseRect, _panelDark, ScaleMode.StretchToFill);
            float inset = size * 0.15f;
            GUI.DrawTexture(new Rect(_pulseRect.x + inset, _pulseRect.y + inset, size - 2f * inset, size - 2f * inset), _shieldIcon, ScaleMode.ScaleToFit, true);
            if (_game.PlanetPulseCharges > 1)
                GUI.Label(new Rect(_pulseRect.x, _pulseRect.y + size * 0.56f, size, size * 0.35f), $"x{_game.PlanetPulseCharges}", _small);
        }

        private void DrawPauseOverlay(Rect safe)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlay);
            Rect panel = CenterPanel(safe, 0.72f, 0.38f, 650f, 620f);
            Panel(panel);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.10f, panel.width, panel.height * 0.24f), "PAUSED", _title);
            BoxButton(GetResumeRect(safe), "RESUME", true);
            BoxButton(GetPauseSettingsRect(safe), "SETTINGS", true);
            BoxButton(GetPauseMenuRect(safe), "MAIN MENU", true);
        }

        private void DrawGameOver(Rect safe)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlay);
            Rect panel = CenterPanel(safe, 0.86f, 0.55f, 820f, 860f);
            Panel(panel);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.06f, panel.width, panel.height * 0.13f), "PLANET LOST", _title);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.20f, panel.width, panel.height * 0.08f), $"SCORE {_game.Score:000000}     BEST {_game.BestScore:000000}", _value);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.29f, panel.width, panel.height * 0.07f), $"COINS  {_game.Coins}     CORES  {_game.Cores}", _small);

            BoxButton(GetGameOverAdReviveRect(safe), _game.CanUseAdRevive ? "WATCH AD  •  REVIVE" : "AD REVIVE USED", _game.CanUseAdRevive);
            bool canCore = _game.Cores >= _game.CurrentCoreReviveCost;
            BoxButton(GetGameOverCoreReviveRect(safe), $"REVIVE  •  ◆{_game.CurrentCoreReviveCost}", canCore);
            BoxButton(GetGameOverRestartRect(safe), "RESTART", true);
            BoxButton(GetGameOverMenuRect(safe), "MAIN MENU", true);
        }

        private void DrawRunShop(Rect safe)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlay);
            Rect panel = CenterPanel(safe, 0.94f, 0.84f, 900f, 1320f);
            Panel(panel);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.025f, panel.width, panel.height * 0.075f), "ORBITAL SHOP", _title);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.092f, panel.width, panel.height * 0.045f), $"COINS  ● {_game.Coins}", _value);

            GetRunShopRows(safe, out Rect r1, out Rect r2, out Rect r3, out Rect r4, out Rect r5);
            DrawShopRow(r1, "MISSILE SUPPLY", $"+3 ammo at the start of every future wave • LV {_game.MissileUpgradeLevel}/{_game.MissileUpgradeMaxLevel}", _game.CanUpgradeMissiles ? $"BUY ●{_game.MissileUpgradeCost}" : "MAX", _game.CanUpgradeMissiles && _game.Coins >= _game.MissileUpgradeCost);
            DrawShopRow(r2, "BLAST POWER", $"Bigger radius + slightly longer blast • LV {_game.MissilePowerLevel}/{_game.MissilePowerMaxLevel}", _game.CanUpgradeMissilePower ? $"BUY ●{_game.MissilePowerUpgradeCost}" : "MAX", _game.CanUpgradeMissilePower && _game.Coins >= _game.MissilePowerUpgradeCost);
            DrawShopRow(r3, "PULSE CHARGE", "+1 planet pulse charge", _game.PlanetPulseCharges < _game.MaxPulseCharges ? $"BUY ●{_game.ShieldChargeCost}" : "FULL", _game.PlanetPulseCharges < _game.MaxPulseCharges && _game.Coins >= _game.ShieldChargeCost);

            string guardianAction = _game.CanBuyGuardian ? $"BUY ●{_game.GuardianCost}" : (_game.GuardianCount >= _game.GuardianMaxCount ? "MAX" : $"SLOTS W{_game.NextGuardianSlotUnlockWave}");
            DrawShopRow(r4, "ORBITAL GUARDIAN", $"Body-blocks asteroids • {_game.GuardianCount}/{_game.GuardianCurrentSlotCap} active slots", guardianAction, _game.CanBuyGuardian && _game.Coins >= _game.GuardianCost);

            string cannonAction = _game.CanBuyCannon ? $"BUY ●{_game.CannonCost}" : (_game.CannonCount >= _game.CannonMaxCount ? "MAX" : $"SLOTS W{_game.NextCannonSlotUnlockWave}");
            DrawShopRow(r5, "ORBITAL CANNON", $"Auto-fires; collision destroys that unit • {_game.CannonCount}/{_game.CannonCurrentSlotCap} active slots", cannonAction, _game.CanBuyCannon && _game.Coins >= _game.CannonCost);
            BoxButton(GetRunShopContinueRect(safe), "CONTINUE", true);
        }

        private void DrawShopRow(Rect r, string name, string desc, string action, bool enabled)
        {
            GUI.DrawTexture(r, _panelDark, ScaleMode.StretchToFill);
            GUI.Label(new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.08f, r.width * 0.58f, r.height * 0.38f), name, _value);
            GUI.Label(new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.43f, r.width * 0.59f, r.height * 0.47f), desc, _description);
            BoxButton(GetShopBuyRect(r), action, enabled, true);
        }

        private void DrawCosmetics(Rect safe)
        {
            DrawCoreChip(safe);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlay);
            Rect panel = CenterPanel(safe, 0.92f, 0.82f, 860f, 1280f);
            Panel(panel);

            string heading = _cosmeticCategory == CosmeticCategory.None
                ? "COSMETICS"
                : CosmeticCategoryTitle(_cosmeticCategory);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.020f, panel.width, panel.height * 0.070f), heading, _title);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.083f, panel.width, panel.height * 0.045f),
                _cosmeticCategory == CosmeticCategory.None
                    ? $"CORES  ◆ {_game.Cores}    •    CHOOSE A CATEGORY"
                    : $"CORES  ◆ {_game.Cores}    •    TAP CHECKBOX TO EQUIP / UNEQUIP",
                _small);

            GetCosmeticRows(safe, out Rect r1, out Rect r2, out Rect r3, out Rect r4, out Rect r5);
            Rect[] rows = { r1, r2, r3, r4, r5 };

            if (_cosmeticCategory == CosmeticCategory.None)
            {
                DrawCategoryRow(r1, "PLANET SKINS", "Earth / Mars / Venus / Neptune");
                DrawCategoryRow(r2, "ROCKET SKINS", "Classic / Tri-Arcana / Solar / Cryo");
                DrawCategoryRow(r3, "EXPLOSION EFFECTS", "Classic / Violet / Solar / Emerald");
                DrawCategoryRow(r4, "ASTEROID SKINS", "Classic asteroids / Kamikaze Fleet");
                DrawCategoryRow(r5, "SPACE THEMES", "Classic / Purple / Crimson / Frozen");

                GetCosmeticNavRects(safe, out _, out _, out Rect backToMenu);
                BoxButton(backToMenu, "BACK", true, true);
                return;
            }

            int itemCount = GetCosmeticItemCount(_cosmeticCategory);
            for (int visible = 0; visible < rows.Length; visible++)
            {
                int itemIndex = _cosmeticScrollIndex + visible;
                if (itemIndex < itemCount)
                    DrawCosmeticItem(_cosmeticCategory, itemIndex, rows[visible]);
            }

            GetCosmeticNavRects(safe, out Rect up, out Rect down, out Rect back);
            int maxScroll = Mathf.Max(0, itemCount - rows.Length);
            BoxButton(up, "↑", _cosmeticScrollIndex > 0, true);
            BoxButton(down, "↓", _cosmeticScrollIndex < maxScroll, true);
            BoxButton(back, "CATEGORIES", true, true);
        }

        private static string CosmeticCategoryTitle(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.Planets: return "PLANET SKINS";
                case CosmeticCategory.Rockets: return "ROCKET SKINS";
                case CosmeticCategory.Explosions: return "EXPLOSION EFFECTS";
                case CosmeticCategory.Asteroids: return "ASTEROID SKINS";
                case CosmeticCategory.Space: return "SPACE THEMES";
                default: return "COSMETICS";
            }
        }

        private void DrawCategoryRow(Rect r, string title, string desc)
        {
            GUI.DrawTexture(r, _panelDark, ScaleMode.StretchToFill);
            GUI.Label(new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.11f, r.width * 0.68f, r.height * 0.32f), title, _value);
            GUI.Label(new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.47f, r.width * 0.68f, r.height * 0.34f), desc, _description);
            BoxButton(GetShopBuyRect(r), "OPEN", true, true);
        }

        private void DrawCosmeticItem(CosmeticCategory category, int index, Rect row)
        {
            switch (category)
            {
                case CosmeticCategory.Planets:
                    if (index == 0) DrawCosmeticRow(row, "EARTH", "Classic blue-green planet", true, _game.EquippedPlanet == PlanetCosmetic.Earth, 0);
                    else if (index == 1) DrawCosmeticRow(row, "MARS", "Warm red planet palette", MetaProgression.IsOwned(PlanetCosmetic.Mars), _game.EquippedPlanet == PlanetCosmetic.Mars, _game.MarsSkinCost);
                    else if (index == 2) DrawCosmeticRow(row, "VENUS", "Golden cloud-world palette", MetaProgression.IsOwned(PlanetCosmetic.Venus), _game.EquippedPlanet == PlanetCosmetic.Venus, _game.VenusSkinCost);
                    else if (index == 3) DrawCosmeticRow(row, "NEPTUNE", "Deep-blue ice giant palette", MetaProgression.IsOwned(PlanetCosmetic.Neptune), _game.EquippedPlanet == PlanetCosmetic.Neptune, _game.NeptuneSkinCost);
                    break;
                case CosmeticCategory.Rockets:
                    if (index == 0) DrawCosmeticRow(row, "CLASSIC", "Default ice-blue missile", true, _game.EquippedRocket == RocketCosmetic.Classic, 0);
                    else if (index == 1) DrawCosmeticRow(row, "TRI-ARCANA", "Three-color arcane rocket pack", MetaProgression.IsOwned(RocketCosmetic.InvokerOrbs), _game.EquippedRocket == RocketCosmetic.InvokerOrbs, _game.InvokerRocketPackCost);
                    else if (index == 2) DrawCosmeticRow(row, "SOLAR LANCE", "Gold / orange / white rockets", MetaProgression.IsOwned(RocketCosmetic.SolarLance), _game.EquippedRocket == RocketCosmetic.SolarLance, _game.SolarRocketThemeCost);
                    else if (index == 3) DrawCosmeticRow(row, "CRYO PULSE", "Cyan / blue / frost-white rockets", MetaProgression.IsOwned(RocketCosmetic.CryoPulse), _game.EquippedRocket == RocketCosmetic.CryoPulse, _game.CryoRocketThemeCost);
                    break;
                case CosmeticCategory.Explosions:
                    if (index == 0) DrawCosmeticRow(row, "CLASSIC", "Default cyan interception explosion", true, _game.EquippedExplosion == ExplosionCosmetic.Classic, 0);
                    else if (index == 1) DrawCosmeticRow(row, "VIOLET NOVA", "Purple interception explosions", MetaProgression.IsOwned(ExplosionCosmetic.VioletNova), _game.EquippedExplosion == ExplosionCosmetic.VioletNova, _game.VioletExplosionCost);
                    else if (index == 2) DrawCosmeticRow(row, "SOLAR BURST", "Hot orange-gold explosions", MetaProgression.IsOwned(ExplosionCosmetic.SolarBurst), _game.EquippedExplosion == ExplosionCosmetic.SolarBurst, _game.SolarExplosionCost);
                    else if (index == 3) DrawCosmeticRow(row, "EMERALD SHOCK", "Bright green interception explosions", MetaProgression.IsOwned(ExplosionCosmetic.EmeraldShock), _game.EquippedExplosion == ExplosionCosmetic.EmeraldShock, _game.EmeraldExplosionCost);
                    break;
                case CosmeticCategory.Asteroids:
                    if (index == 0) DrawCosmeticRow(row, "CLASSIC", "Original asteroid silhouettes", true, !_game.UseKamikazeAsteroids, 0);
                    else if (index == 1) DrawCosmeticRow(row, "KAMIKAZE FLEET", "Enemy ships replace asteroid silhouettes", MetaProgression.IsOwned(AsteroidCosmetic.KamikazeShips), _game.UseKamikazeAsteroids, _game.KamikazeAsteroidCost);
                    break;
                case CosmeticCategory.Space:
                    if (index == 0) DrawCosmeticRow(row, "CLASSIC", "Deep-black space + pale stars", true, _game.EquippedSpace == SpaceCosmetic.Classic, 0);
                    else if (index == 1) DrawCosmeticRow(row, "PURPLE SPACE", "Dark violet sky + blue stars", MetaProgression.IsOwned(SpaceCosmetic.PurpleNebula), _game.EquippedSpace == SpaceCosmetic.PurpleNebula, _game.PurpleSpaceCost);
                    else if (index == 2) DrawCosmeticRow(row, "CRIMSON VOID", "Black-red space + warm stars", MetaProgression.IsOwned(SpaceCosmetic.CrimsonVoid), _game.EquippedSpace == SpaceCosmetic.CrimsonVoid, _game.CrimsonSpaceCost);
                    else if (index == 3) DrawCosmeticRow(row, "FROZEN NIGHT", "Deep navy space + cyan stars", MetaProgression.IsOwned(SpaceCosmetic.FrozenNight), _game.EquippedSpace == SpaceCosmetic.FrozenNight, _game.FrozenSpaceCost);
                    break;
            }
        }

        private void DrawCosmeticPlaceholder(Rect r, string title, string desc)
        {
            GUI.DrawTexture(r, _panelDark, ScaleMode.StretchToFill);
            GUI.Label(new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.08f, r.width * 0.90f, r.height * 0.34f), title, _value);
            GUI.Label(new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.43f, r.width * 0.90f, r.height * 0.45f), desc, _description);
        }

        private void DrawCosmeticRow(Rect r, string title, string desc, bool owned, bool equipped, int cost)
        {
            GUI.DrawTexture(r, _panelDark, ScaleMode.StretchToFill);
            GUI.Label(new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.07f, r.width * 0.58f, r.height * 0.34f), title, _value);
            GUI.Label(new Rect(r.x + r.width * 0.035f, r.y + r.height * 0.43f, r.width * 0.58f, r.height * 0.45f), desc, _description);
            Rect actionRect = GetShopBuyRect(r);
            if (!owned)
            {
                BoxButton(actionRect, $"BUY ◆{cost}", _game.Cores >= cost, true);
                return;
            }

            DrawCheckbox(actionRect, equipped);
        }

        private void DrawCheckbox(Rect r, bool enabled)
        {
            GUI.DrawTexture(r, _panel, ScaleMode.StretchToFill);
            float size = Mathf.Min(r.height * 0.62f, r.width * 0.36f);
            Rect box = new Rect(r.center.x - size * 0.5f, r.center.y - size * 0.5f, size, size);
            GUI.Box(box, string.Empty, _buttonSmall);
            if (enabled)
                GUI.Label(box, "✓", _value);
        }

        private void DrawSettings(Rect safe)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlay);
            Rect panel = CenterPanel(safe, 0.84f, 0.68f, 780f, 1020f);
            Panel(panel);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.05f, panel.width, panel.height * 0.11f), "SETTINGS", _title);

            GetSettingsRects(safe, out Rect sfx, out Rect music, out Rect haptics, out Rect shake, out Rect fps, out Rect back);
            BoxButton(sfx, $"SFX   {(GameSettings.SfxEnabled ? "ON" : "OFF")}", true);
            BoxButton(music, $"MUSIC   {(GameSettings.MusicEnabled ? "ON" : "OFF")}", true);
            BoxButton(haptics, $"VIBRATION   {(GameSettings.HapticsEnabled ? "ON" : "OFF")}", true);
            BoxButton(shake, $"SCREEN SHAKE   {(GameSettings.ScreenShakeEnabled ? "ON" : "OFF")}", true);
            BoxButton(fps, $"FRAME RATE   {GameSettings.TargetFps} FPS", true);
            GUI.Label(new Rect(panel.x + panel.width * 0.08f, panel.yMax - panel.height * 0.18f, panel.width * 0.84f, panel.height * 0.07f), "30 FPS saves battery • 60 recommended • 120 for high-refresh devices", _description);
            BoxButton(back, "BACK", true);
        }

        private void DrawTutorialHint(Rect safe)
        {
            if (_tutorial == null) _tutorial = GetComponent<TutorialHints>();
            if (_tutorial == null || !_tutorial.HasMessage) return;

            string message = _tutorial.CurrentMessage;
            float w = Mathf.Min(safe.width * 0.86f, 780f);
            float h = Mathf.Clamp(Screen.height * 0.055f, 66f, 104f);
            float top = safe.y + Mathf.Clamp(Screen.height * 0.061f, 74f, 118f) + Mathf.Clamp(Screen.height * 0.052f, 60f, 92f);
            Rect r = new Rect(safe.center.x - w * 0.5f, top, w, h);
            GUI.DrawTexture(r, _panelDark, ScaleMode.StretchToFill);
            GUI.Label(new Rect(r.x + w * 0.04f, r.y, w * 0.92f, h), message, _buttonSmall);
        }

        private void DrawCoreStore(Rect safe)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlay);
            Rect panel = CenterPanel(safe, 0.84f, 0.56f, 780f, 850f);
            Panel(panel);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.05f, panel.width, panel.height * 0.12f), "CORE STORE", _title);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.17f, panel.width, panel.height * 0.07f), $"BALANCE  ◆ {_game.Cores}", _value);
#if UNITY_EDITOR
            GUI.Label(new Rect(panel.x, panel.y + panel.height * 0.235f, panel.width, panel.height * 0.055f), "EDITOR MOCK: purchases are simulated", _small);
#endif
            GetCoreStoreRects(safe, out Rect p1, out Rect p2, out Rect p3, out Rect back);
            BoxButton(p1, $"◆ 5     {_game.GetCorePackPrice(0)}", _game.CorePurchasesAvailable);
            BoxButton(p2, $"◆ 18    {_game.GetCorePackPrice(1)}", _game.CorePurchasesAvailable);
            BoxButton(p3, $"◆ 50    {_game.GetCorePackPrice(2)}", _game.CorePurchasesAvailable);
            BoxButton(back, "BACK", true);
        }

        private Rect GetCoreChipRect(Rect safe, float overrideY = -1f)
        {
            float pad = Mathf.Max(12f, Screen.width * 0.024f);
            float h = Mathf.Clamp(Screen.height * 0.034f, 42f, 62f);
            float w = Mathf.Clamp(Screen.width * 0.24f, 140f, 245f);
            float y = overrideY >= 0f ? overrideY : safe.y + pad;
            return new Rect(safe.xMax - pad - w, y, w, h);
        }

        private Rect GetMainPlayRect(Rect safe) => MainMenuButton(safe, 0);
        private Rect GetMainCosmeticsRect(Rect safe) => MainMenuButton(safe, 1);
        private Rect GetMainSettingsRect(Rect safe) => MainMenuButton(safe, 2);
        private Rect GetMainAdCoreRect(Rect safe) => MainMenuButton(safe, 3);
        private Rect MainMenuButton(Rect safe, int index)
        {
            float w = Mathf.Min(safe.width * 0.82f, 760f);
            float h = Mathf.Min(safe.height * 0.56f, 820f);
            Rect panel = new Rect(safe.center.x - w / 2f, safe.y + safe.height * 0.16f, w, h);
            float bw = panel.width * 0.66f;
            float bh = Mathf.Clamp(Screen.height * 0.048f, 58f, 88f);
            float gap = bh * 0.18f;
            float y0 = panel.y + panel.height * 0.38f;
            return new Rect(panel.center.x - bw / 2f, y0 + index * (bh + gap), bw, bh);
        }

        private float PauseSize() => Mathf.Clamp(Screen.width * 0.082f, 58f, 92f);
        private Rect GetPauseRect(Rect safe)
        {
            float pad = Mathf.Max(12f, Screen.width * 0.024f);
            float size = PauseSize();
            float statH = Mathf.Clamp(Screen.height * 0.061f, 74f, 118f);
            return new Rect(safe.xMax - pad - size, safe.y + pad + (statH - size) / 2f, size, size);
        }

        private Rect GetResumeRect(Rect safe) => PauseOverlayButton(safe, 0);
        private Rect GetPauseSettingsRect(Rect safe) => PauseOverlayButton(safe, 1);
        private Rect GetPauseMenuRect(Rect safe) => PauseOverlayButton(safe, 2);

        private Rect PauseOverlayButton(Rect safe, int index)
        {
            Rect panel = CenterPanel(safe, 0.72f, 0.38f, 650f, 620f);
            float bw = panel.width * 0.64f;
            float bh = Mathf.Clamp(Screen.height * 0.046f, 56f, 84f);
            float gap = bh * 0.15f;
            float y0 = panel.y + panel.height * 0.38f;
            return new Rect(panel.center.x - bw / 2f, y0 + index * (bh + gap), bw, bh);
        }

        private Rect GetGameOverAdReviveRect(Rect safe) => GameOverButton(safe, 0);
        private Rect GetGameOverCoreReviveRect(Rect safe) => GameOverButton(safe, 1);
        private Rect GetGameOverRestartRect(Rect safe) => GameOverButton(safe, 2);
        private Rect GetGameOverMenuRect(Rect safe) => GameOverButton(safe, 3);
        private Rect GameOverButton(Rect safe, int index)
        {
            Rect panel = CenterPanel(safe, 0.86f, 0.55f, 820f, 860f);
            float bw = panel.width * 0.72f;
            float bh = Mathf.Clamp(Screen.height * 0.044f, 54f, 82f);
            float gap = bh * 0.15f;
            float y0 = panel.y + panel.height * 0.40f;
            return new Rect(panel.center.x - bw / 2f, y0 + index * (bh + gap), bw, bh);
        }

        private void GetRunShopRects(Rect safe, out Rect missile, out Rect power, out Rect shield, out Rect guardian, out Rect cannon, out Rect cont)
        {
            GetRunShopRows(safe, out Rect r1, out Rect r2, out Rect r3, out Rect r4, out Rect r5);
            missile = GetShopBuyRect(r1);
            power = GetShopBuyRect(r2);
            shield = GetShopBuyRect(r3);
            guardian = GetShopBuyRect(r4);
            cannon = GetShopBuyRect(r5);
            cont = GetRunShopContinueRect(safe);
        }

        private void GetRunShopRows(Rect safe, out Rect r1, out Rect r2, out Rect r3, out Rect r4, out Rect r5)
        {
            Rect panel = CenterPanel(safe, 0.94f, 0.84f, 900f, 1320f);
            float side = panel.width * 0.038f;
            float top = panel.y + panel.height * 0.15f;
            float gap = panel.height * 0.012f;
            float rh = panel.height * 0.110f;
            float rw = panel.width - side * 2f;
            r1 = new Rect(panel.x + side, top, rw, rh); top += rh + gap;
            r2 = new Rect(panel.x + side, top, rw, rh); top += rh + gap;
            r3 = new Rect(panel.x + side, top, rw, rh); top += rh + gap;
            r4 = new Rect(panel.x + side, top, rw, rh); top += rh + gap;
            r5 = new Rect(panel.x + side, top, rw, rh);
        }

        private Rect GetRunShopContinueRect(Rect safe)
        {
            Rect panel = CenterPanel(safe, 0.94f, 0.84f, 900f, 1320f);
            float bw = panel.width * 0.58f;
            float bh = Mathf.Clamp(Screen.height * 0.043f, 54f, 80f);
            return new Rect(panel.center.x - bw / 2f, panel.yMax - bh - panel.height * 0.018f, bw, bh);
        }

        private static Rect GetShopBuyRect(Rect row)
        {
            float w = row.width * 0.31f;
            return new Rect(row.xMax - w - row.width * 0.03f, row.y + row.height * 0.20f, w, row.height * 0.60f);
        }

        private void GetCosmeticRows(Rect safe, out Rect r1, out Rect r2, out Rect r3, out Rect r4, out Rect r5)
        {
            Rect panel = CenterPanel(safe, 0.92f, 0.82f, 860f, 1280f);
            float side = panel.width * 0.04f;
            float top = panel.y + panel.height * 0.145f;
            float gap = panel.height * 0.010f;
            float rh = panel.height * 0.115f;
            float rw = panel.width - side * 2f;
            r1 = new Rect(panel.x + side, top, rw, rh); top += rh + gap;
            r2 = new Rect(panel.x + side, top, rw, rh); top += rh + gap;
            r3 = new Rect(panel.x + side, top, rw, rh); top += rh + gap;
            r4 = new Rect(panel.x + side, top, rw, rh); top += rh + gap;
            r5 = new Rect(panel.x + side, top, rw, rh);
        }

        private void GetCosmeticNavRects(Rect safe, out Rect prev, out Rect next, out Rect back)
        {
            Rect panel = CenterPanel(safe, 0.92f, 0.82f, 860f, 1280f);
            float bh = Mathf.Clamp(Screen.height * 0.040f, 50f, 74f);
            float smallW = panel.width * 0.16f;
            float backW = panel.width * 0.36f;
            float y = panel.yMax - bh - panel.height * 0.020f;
            prev = new Rect(panel.x + panel.width * 0.04f, y, smallW, bh);
            next = new Rect(prev.xMax + panel.width * 0.025f, y, smallW, bh);
            back = new Rect(panel.xMax - backW - panel.width * 0.04f, y, backW, bh);
        }

        private void GetSettingsRects(Rect safe, out Rect sfx, out Rect music, out Rect haptics, out Rect shake, out Rect fps, out Rect back)
        {
            Rect panel = CenterPanel(safe, 0.84f, 0.68f, 780f, 1020f);
            float bw = panel.width * 0.70f;
            float bh = Mathf.Clamp(Screen.height * 0.046f, 56f, 84f);
            float gap = bh * 0.16f;
            float y0 = panel.y + panel.height * 0.20f;
            sfx = new Rect(panel.center.x - bw / 2f, y0, bw, bh);
            music = new Rect(panel.center.x - bw / 2f, y0 + (bh + gap), bw, bh);
            haptics = new Rect(panel.center.x - bw / 2f, y0 + (bh + gap) * 2f, bw, bh);
            shake = new Rect(panel.center.x - bw / 2f, y0 + (bh + gap) * 3f, bw, bh);
            fps = new Rect(panel.center.x - bw / 2f, y0 + (bh + gap) * 4f, bw, bh);
            float backW = panel.width * 0.46f;
            back = new Rect(panel.center.x - backW / 2f, panel.yMax - bh - panel.height * 0.035f, backW, bh);
        }

        private void GetCoreStoreRects(Rect safe, out Rect p1, out Rect p2, out Rect p3, out Rect back)
        {
            Rect panel = CenterPanel(safe, 0.84f, 0.56f, 780f, 850f);
            float bw = panel.width * 0.68f;
            float bh = Mathf.Clamp(Screen.height * 0.050f, 60f, 90f);
            float gap = bh * 0.22f;
            float y = panel.y + panel.height * 0.35f;
            p1 = new Rect(panel.center.x - bw / 2f, y, bw, bh);
            p2 = new Rect(panel.center.x - bw / 2f, y + bh + gap, bw, bh);
            p3 = new Rect(panel.center.x - bw / 2f, y + (bh + gap) * 2f, bw, bh);
            float backW = panel.width * 0.45f;
            back = new Rect(panel.center.x - backW / 2f, panel.yMax - bh - panel.height * 0.06f, backW, bh);
        }

        private static Rect SafeRect()
        {
            Rect s = Screen.safeArea;
            return new Rect(s.x, Screen.height - s.y - s.height, s.width, s.height);
        }

        private static Rect CenterPanel(Rect safe, float widthFactor, float heightFactor, float maxW, float maxH)
        {
            float w = Mathf.Min(safe.width * widthFactor, maxW);
            float h = Mathf.Min(safe.height * heightFactor, maxH);
            return new Rect(safe.center.x - w / 2f, safe.center.y - h / 2f, w, h);
        }

        private void Panel(Rect r)
        {
            GUI.DrawTexture(r, _panel, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, Mathf.Max(3f, Screen.height * 0.002f)), _accent);
        }

        private void BoxButton(Rect r, string text, bool enabled, bool small = false)
        {
            Color old = GUI.color;
            if (!enabled) GUI.color = new Color(1f, 1f, 1f, 0.38f);
            GUI.Box(r, text, small ? _buttonSmall : _button);
            GUI.color = old;
        }

        private void EnsureStyles()
        {
            if (_label != null) return;
            int small = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.022f), 14, 24);
            int val = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.034f), 20, 40);
            int title = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.058f), 30, 66);
            int button = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.034f), 20, 38);
            int ammo = Mathf.Clamp(Mathf.RoundToInt(Screen.width * 0.078f), 38, 88);

            _label = new GUIStyle(GUI.skin.label) { fontSize = val, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _small = new GUIStyle(GUI.skin.label) { fontSize = small, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.66f, 0.82f, 0.93f) } };
            _value = new GUIStyle(_label) { fontSize = val };
            _title = new GUIStyle(_label) { fontSize = title };
            _button = new GUIStyle(GUI.skin.button) { fontSize = button, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, hover = { textColor = Color.white }, active = { textColor = Color.white } };
            _buttonSmall = new GUIStyle(_button) { fontSize = Mathf.Max(13, button - 7), wordWrap = true };
            _description = new GUIStyle(_small) { alignment = TextAnchor.MiddleLeft, wordWrap = true, fontStyle = FontStyle.Normal, fontSize = Mathf.Max(12, small - 1), clipping = TextClipping.Clip };
            _ammo = new GUIStyle(_label) { fontSize = ammo, normal = { textColor = new Color(0.86f, 0.97f, 1f) } };

            _panel = Solid(new Color(0.025f, 0.055f, 0.09f, 0.90f));
            _panelDark = Solid(new Color(0.015f, 0.035f, 0.060f, 0.92f));
            _accent = Solid(new Color(0.26f, 0.88f, 1f, 0.92f));
            _overlay = Solid(new Color(0.003f, 0.008f, 0.018f, 0.76f));
            _shieldIcon = CreateShield(64);
            _coinIcon = Circle(32, new Color(1f, 0.78f, 0.15f, 1f));
            _coreIcon = CreateDiamond(32, new Color(0.45f, 0.90f, 1f, 1f));
        }

        private static Texture2D Solid(Color c)
        {
            Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false); t.SetPixel(0, 0, c); t.Apply(); t.hideFlags = HideFlags.HideAndDontSave; return t;
        }

        private static Texture2D Circle(int size, Color c)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false); Color[] px = new Color[size * size]; float m = (size - 1) / 2f; float rr = size * 0.47f; rr *= rr;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) { float dx = x - m, dy = y - m; px[y * size + x] = dx * dx + dy * dy <= rr ? c : Color.clear; }
            t.SetPixels(px); t.Apply(); t.hideFlags = HideFlags.HideAndDontSave; return t;
        }

        private static Texture2D CreateDiamond(int size, Color c)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false); Color[] px = new Color[size * size]; float m = (size - 1) / 2f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) { float d = Mathf.Abs(x - m) + Mathf.Abs(y - m); px[y * size + x] = d <= m * 0.88f ? c : Color.clear; }
            t.SetPixels(px); t.Apply(); t.hideFlags = HideFlags.HideAndDontSave; return t;
        }

        private static Texture2D CreateShield(int size)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false); Color[] px = new Color[size * size]; Color c = new Color(0.25f, 0.92f, 1f, 0.95f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f; float ny = (y + 0.5f) / size * 2f - 1f;
                bool inside = ny < 0.82f && ny > -0.78f && Mathf.Abs(nx) < (0.70f - Mathf.Max(0f, -ny) * 0.42f);
                bool inner = ny < 0.63f && ny > -0.54f && Mathf.Abs(nx) < (0.51f - Mathf.Max(0f, -ny) * 0.34f);
                px[y * size + x] = inside && !inner ? c : Color.clear;
            }
            t.SetPixels(px); t.Apply(); t.hideFlags = HideFlags.HideAndDontSave; return t;
        }
    }
}
