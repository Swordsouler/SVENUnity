using System;
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
        [BoxGroup("View")] public Slider sensitivitySlider;

        [BoxGroup("Controller")] public DemoGraphBuffer graphBuffer;
        [BoxGroup("Controller")] public DemoCharacterController demoCharacterController;

        private bool _isSending = false;

        public new void Awake()
        {
            base.Awake();
            InitializeButtons();
        }

        public void Start()
        {
            if (sendingActivityIndicator != null) sendingActivityIndicator.SetActive(_isSending);
            if (triplesAmountText != null) triplesAmountText.text = graphBuffer.Graph.Triples.Count.ToString();
        }

        public override void TogglePause()
        {
            if (_isSending) return;
            base.TogglePause();
            if (demoCharacterController != null) demoCharacterController.enabled = !demoCharacterController.enabled;
            Cursor.lockState = demoCharacterController.enabled && demoCharacterController.lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
        }

        private void InitializeButtons()
        {
            if (saveQuitButton != null) saveQuitButton.onClick.AddListener(OnSaveQuitButtonClicked);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderValueChanged);
        }

        private void OnSensitivitySliderValueChanged(float arg0)
        {
            if (demoCharacterController != null) demoCharacterController.mouseSensitivity = arg0;
        }

        private async void OnSaveQuitButtonClicked()
        {
            if (_isSending) return;
            _isSending = true;
            if (saveQuitButton != null) saveQuitButton.gameObject.SetActive(false);
            if (sendingActivityIndicator != null) sendingActivityIndicator.SetActive(true);

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