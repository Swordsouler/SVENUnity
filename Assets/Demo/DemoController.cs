using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sven.Demo
{
    public class DemoController : MonoBehaviour
    {
        public TMP_InputField graphNameInputField;
        public TMP_Dropdown graphNameDropdown;
        public Button readingModeButton, writingModeButton, playButton, replayButton;
        public List<Button> backButtons = new();
        public GameObject mainMenuForm, readingModeForm, writingModeForm;

        private void Awake()
        {
            readingModeButton.onClick.AddListener(OnReadingModeButtonClicked);
            writingModeButton.onClick.AddListener(OnWritingModeButtonClicked);
            playButton.onClick.AddListener(OnPlayButtonClicked);
            replayButton.onClick.AddListener(OnReplayButtonClicked);
            foreach (Button button in backButtons) button.onClick.AddListener(OnBackButtonClicked);
            graphNameInputField.onValueChanged.AddListener(OnNamedGraphInputFieldChanged);

            OnBackButtonClicked();
        }

        private void OnNamedGraphInputFieldChanged(string arg0)
        {
            DemoManager.graphName = string.IsNullOrEmpty(arg0) ? "default" : arg0;
        }

        private void OnBackButtonClicked()
        {
            mainMenuForm.SetActive(true);
            writingModeForm.SetActive(false);
            readingModeForm.SetActive(false);
        }

        private void OnWritingModeButtonClicked()
        {
            mainMenuForm.SetActive(false);
            writingModeForm.SetActive(true);
            readingModeForm.SetActive(false);
        }

        private void OnReadingModeButtonClicked()
        {
            mainMenuForm.SetActive(false);
            writingModeForm.SetActive(false);
            readingModeForm.SetActive(true);
        }

        private void OnReplayButtonClicked()
        {
            SceneManager.LoadScene("Demo Reading", LoadSceneMode.Single);
        }

        private void OnPlayButtonClicked()
        {
            SceneManager.LoadScene("Demo Writing", LoadSceneMode.Single);
        }
    }
}
