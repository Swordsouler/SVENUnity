using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NaughtyAttributes;
using Sven.GraphManagement;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VDS.RDF;
using VDS.RDF.Query;

namespace Sven.Demo
{
    [Serializable]
    public class DemoMainMenuPage
    {
        public string title;
        public GameObject form;
        public List<Button> buttons = new();
    }

    public class DemoMainMenuController : MonoBehaviour
    {
        [BoxGroup("Model")] public List<DemoMainMenuPage> pages = new();

        [BoxGroup("View")] public TMP_InputField VENameInputField;
        [BoxGroup("View")] public TMP_Dropdown VENameDropdown;
        [BoxGroup("View")] public Button playButton, replayButton;
        [BoxGroup("View")] public TextMeshProUGUI subTitleText;
        [BoxGroup("View")] public GameObject dropdownActivityIndicator;
        [BoxGroup("View")] public Slider semantisationFrequencySlider;

        private DemoMainMenuPage _currentPage = null;
        private Dictionary<string, string> _dropdownOptions = new();

        private void Awake()
        {
            if (semantisationFrequencySlider != null) semantisationFrequencySlider.value = DemoManager.semantisationFrequency;
            InitializeButtons();
            InitializeDropdownAsync();
            // load main menu form
            SetPage(pages[0]);
        }

        private void InitializeButtons()
        {
            foreach (DemoMainMenuPage menuPage in pages)
                foreach (Button button in menuPage.buttons)
                    if (button != null) button.onClick.AddListener(() => SetPage(menuPage));
            if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClicked);
            if (replayButton != null) replayButton.onClick.AddListener(OnReplayButtonClicked);
        }

        public void RefreshExistentVEs()
        {
            InitializeDropdownAsync();
        }

        private async void InitializeDropdownAsync()
        {
            SetDropdownLoadingState(true);

            await LoadExistentVEs();

            SetDropdownLoadingState(false);
            UpdateDropdownSelection();
        }

        private void SetDropdownLoadingState(bool isLoading)
        {
            dropdownActivityIndicator.SetActive(isLoading);
            if (isLoading) VENameDropdown.ClearOptions();
        }

        private void UpdateDropdownSelection()
        {
            if (VENameDropdown.options.Count > 0)
            {
                string graphName = DemoManager.graphName;
                int index = VENameDropdown.options.FindIndex(option => _dropdownOptions.TryGetValue(option.text, out string name) && name == graphName);
                VENameDropdown.value = index != -1 ? index : 0;
                VENameDropdown.RefreshShownValue();
            }
        }

        private void SetPage(DemoMainMenuPage page)
        {
            if (_currentPage == page) return;
            _currentPage = page;
            foreach (DemoMainMenuPage menuPage in pages)
            {
                if (menuPage.form != null) menuPage.form.SetActive(menuPage == page);
                foreach (Button button in menuPage.buttons)
                    if (button != null) button.interactable = menuPage != page;
            }
            if (subTitleText != null) subTitleText.text = page.title;
        }

        private void OnReplayButtonClicked()
        {
            if (!_dropdownOptions.TryGetValue(VENameDropdown.options[VENameDropdown.value].text, out string graphName)) return;
            DemoManager.graphName = string.IsNullOrEmpty(graphName) ? "default" : graphName;
            SceneManager.LoadScene("Demo Replay", LoadSceneMode.Single);
        }

        private void OnPlayButtonClicked()
        {
            DemoManager.semantisationFrequency = (int)semantisationFrequencySlider.value;
            DemoManager.graphName = string.IsNullOrEmpty(VENameInputField.text) ? "default" : VENameInputField.text;
            SceneManager.LoadScene("Demo Record", LoadSceneMode.Single);
        }

        // Méthode pour charger le fichier SPARQL
        private async Task<string> LoadQueryFileAsync(string relativePath)
        {
            string queryFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // Utiliser UnityWebRequest pour WebGL
                UnityWebRequest request = UnityWebRequest.Get(queryFilePath);
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    Debug.LogError($"Erreur lors du chargement du fichier SPARQL : {request.error}");
                    return null;
                }
            }
            else
            {
                // Lecture locale pour les autres plateformes
                return System.IO.File.ReadAllText(queryFilePath);
            }
        }

        private async Task LoadExistentVEs()
        {
            try
            {
                HttpClient httpClient = new();
                SparqlQueryClient client = new(httpClient, DemoManager.EndpointUri);
                string query = await LoadQueryFileAsync("SPARQL/ListExistentVEs.sparql");
                Debug.Log($"Query: {query}");
                Debug.Log($"endpointUri: {DemoManager.EndpointUri}");

                SparqlResultSet results = await client.QueryWebGLWithResultSetAsync(query);

                Debug.Log($"Results: {results.ToString()}");

                VENameDropdown.options.Clear();
                _dropdownOptions.Clear();
                foreach (SparqlResult result in results.Cast<SparqlResult>())
                {
                    string optionContent = "";

                    if (result["minInstant"] is not ILiteralNode instantNode) continue;
                    if (result["graphName"] is not IUriNode graphNameNode) continue;
                    if (result["duration"] is not ILiteralNode durationNode) continue;

                    string graphName = graphNameNode.Uri.ToString().Split("/").LastOrDefault();

                    optionContent += $"[{FormatDate(instantNode.Value)}]";
                    optionContent += $" <b>{graphName}</b>";
                    optionContent += $" | {FormatDuration(durationNode.Value)}";

                    VENameDropdown.options.Add(new TMP_Dropdown.OptionData(optionContent));
                    _dropdownOptions.Add(optionContent, graphName);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading graph names: {e.Message}");
            }
        }

        // 2025-04-07T16:13:07.300+02:00 -> 07/04/2025 16:13:07 depending of the right utc offset
        private string FormatDate(string value)
        {
            try
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(value);
                DateTime localDateTime = dateTimeOffset.LocalDateTime;
                return localDateTime.ToString("dd/MM/yyyy HH:mm");
            }
            catch (FormatException)
            {
                Debug.LogError($"Format de date invalide : {value}");
                return value;
            }
        }

        // P0Y0M0DT1H1M11.900S -> 01h01m11s
        private string FormatDuration(string rawDuration)
        {
            if (rawDuration.Length < 10) return rawDuration;
            string[] parts = rawDuration.Split(new[] { 'P', 'Y', 'M', 'D', 'T', 'H', 'S' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return rawDuration;

            string hours = parts[^3].PadLeft(2, '0');
            string minutes = parts[^2].PadLeft(2, '0');
            string seconds = parts[^1].Split(".")[0].PadLeft(2, '0');

            return $"{(hours != "00" ? hours + "h" : "")}{(hours != "00" || minutes != "00" ? minutes + "m" : "")}{seconds}s";
        }
    }
}
