using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sven.Demo
{
    public class DemoPauseReplayController : DemoPauseController
    {
        [BoxGroup("View")] public Button quitButton;

        public new void Awake()
        {
            base.Awake();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        private void OnQuitButtonClicked()
        {
            SceneManager.LoadScene("Demo Menu", LoadSceneMode.Single);
        }
    }
}