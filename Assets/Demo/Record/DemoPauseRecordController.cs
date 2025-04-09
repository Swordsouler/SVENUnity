using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sven.Demo
{
    public class DemoPauseRecordController : DemoPauseController
    {
        [BoxGroup("View")] public TextMeshProUGUI triplesAmountText;
        [BoxGroup("View")] public Button saveQuitButton;
        [BoxGroup("View")] public GameObject sendingActivityIndicator;

        [BoxGroup("Controller")] public DemoGraphBuffer graphBuffer;

        private bool _isSending = false;

        public new void Awake()
        {
            base.Awake();
            InitializeButtons();
        }

        public void Start()
        {
            sendingActivityIndicator.SetActive(_isSending);
            triplesAmountText.text = graphBuffer.Graph.Triples.Count.ToString();
        }

        public new void TogglePause()
        {
            if (_isSending) return;
            base.TogglePause();
        }

        private void InitializeButtons()
        {
            if (saveQuitButton != null) saveQuitButton.onClick.AddListener(OnSaveQuitButtonClicked);
        }

        private async void OnSaveQuitButtonClicked()
        {
            if (_isSending) return;
            _isSending = true;
            saveQuitButton.gameObject.SetActive(false);
            sendingActivityIndicator.SetActive(true);

            await graphBuffer.SaveExperiment();
            SceneManager.LoadScene("Demo Menu", LoadSceneMode.Single);
        }

        public new void Update()
        {
            if (_isSending) return;
            base.Update();
            triplesAmountText.text = graphBuffer.Graph.Triples.Count.ToString();
        }
    }
}