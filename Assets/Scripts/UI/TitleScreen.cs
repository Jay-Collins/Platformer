using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private string _gameplayScene = "SampleScene";
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TMP_Text _playLabel;
        [SerializeField] private CanvasGroup _fade;
        [SerializeField] private Transform[] _stars;
        private Vector3[] _starPositions;
        private bool _loading;

        public bool IsLoading => _loading;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
            _playButton.onClick.AddListener(Play);
            _quitButton.onClick.AddListener(Quit);
            _quitButton.gameObject.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);
            _starPositions = new Vector3[_stars.Length];
            for (int i = 0; i < _stars.Length; i++) _starPositions[i] = _stars[i].localPosition;
        }

        private void Start() => _playButton.Select();

        private void Update()
        {
            // UI handles Enter/Space on the selected button, plus pointer clicks.
            // Restore selection after clicking the background for keyboard navigation.
            if (!_loading && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
                Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))
                _playButton.Select();

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].localPosition = _starPositions[i] + Vector3.up * (Mathf.Sin(Time.unscaledTime * 1.4f + i * 2f) * 0.12f);
                _stars[i].Rotate(Vector3.up, 22f * Time.unscaledDeltaTime, Space.World);
            }
        }

        public void Play()
        {
            if (_loading) return;
            if (!Application.CanStreamedLevelBeLoaded(_gameplayScene))
            {
                Debug.LogError("The gameplay scene must be enabled in Build Settings.", this);
                return;
            }
            _loading = true;
            _playButton.interactable = _quitButton.interactable = false;
            _playLabel.text = "LOADING...";
            StartCoroutine(LoadGame());
        }

        private IEnumerator LoadGame()
        {
            _fade.blocksRaycasts = true;
            var operation = SceneManager.LoadSceneAsync(_gameplayScene);
            operation.allowSceneActivation = false;
            for (float elapsed = 0f; elapsed < 0.35f; elapsed += Time.unscaledDeltaTime)
            {
                _fade.alpha = Mathf.Clamp01(elapsed / 0.35f);
                yield return null;
            }
            _fade.alpha = 1f;
            operation.allowSceneActivation = true;
        }

        public void Quit()
        {
            if (_loading) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
