using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;

namespace Sven.Demo
{
    public class DemoController : MonoBehaviour
    {
        [Serializable]
        public class DemoMenuPage
        {
            public string title;
            public GameObject form;
            public List<Button> buttons = new();
        }
        [BoxGroup("Model")] public List<DemoMenuPage> pages = new();

        [BoxGroup("View")] public TMP_InputField graphNameInputField;
        [BoxGroup("View")] public TMP_Dropdown graphNameDropdown;
        [BoxGroup("View")] public Button playButton, replayButton;
        [BoxGroup("View")] public TextMeshProUGUI subTitleText;
        [BoxGroup("View")] public GameObject dropdownLoadingIndicator;

        private DemoMenuPage _currentPage = null;

        private void Awake()
        {
            InitializeButtons();
            InitializeDropdownAsync();
            // load main menu form
            SetPage(pages[0]);
        }

        private void InitializeButtons()
        {
            foreach (DemoMenuPage menuPage in pages)
                foreach (Button button in menuPage.buttons)
                    if (button != null) button.onClick.AddListener(() => SetPage(menuPage));
            if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClicked);
            if (replayButton != null) replayButton.onClick.AddListener(OnReplayButtonClicked);
        }

        private async void InitializeDropdownAsync()
        {
            SetDropdownLoadingState(true);

            await LoadExistentGraphs();

            SetDropdownLoadingState(false);
            UpdateDropdownSelection();
        }

        private void SetDropdownLoadingState(bool isLoading)
        {
            dropdownLoadingIndicator.SetActive(isLoading);
            if (isLoading) graphNameDropdown.ClearOptions();
        }

        private void UpdateDropdownSelection()
        {
            if (graphNameDropdown.options.Count > 0)
            {
                graphNameDropdown.value = 0;
                graphNameDropdown.RefreshShownValue();
            }
        }

        private void SetPage(DemoMenuPage page)
        {
            if (_currentPage == page) return;
            _currentPage = page;
            foreach (DemoMenuPage menuPage in pages)
                if (menuPage.form != null) menuPage.form.SetActive(menuPage == page);
            if (subTitleText != null) subTitleText.text = page.title;
        }

        private void OnReplayButtonClicked()
        {
            DemoManager.graphName = string.IsNullOrEmpty(graphNameDropdown.options[graphNameDropdown.value].text) ? "default" : graphNameDropdown.options[graphNameDropdown.value].text;
            SceneManager.LoadScene("Demo Replay", LoadSceneMode.Single);
        }

        private void OnPlayButtonClicked()
        {
            DemoManager.graphName = string.IsNullOrEmpty(graphNameInputField.text) ? "default" : graphNameInputField.text;
            SceneManager.LoadScene("Demo Record", LoadSceneMode.Single);
        }

        private async Task LoadExistentGraphs()
        {
            try
            {
                HttpClient httpClient = new();
                SparqlQueryClient client = new(httpClient, DemoManager.EndpointUri);
                string queryFilePath = System.IO.Path.Combine(Application.dataPath, "Demo", "ListExistentGraphs.sparql");
                string query = System.IO.File.ReadAllText(queryFilePath);

                SparqlResultSet results = await client.QueryWithResultSetAsync(query).ConfigureAwait(false);

                graphNameDropdown.options.Clear();
                foreach (SparqlResult result in results.Cast<SparqlResult>())
                {
                    string optionContent = "";

                    if (result["minInstant"] is ILiteralNode instantNode)
                        optionContent += $"[{FormatDate(instantNode.Value)}]";

                    if (result["graphName"] is IUriNode graphNameNode)
                        optionContent += $" <b>{graphNameNode.Uri.ToString().Split("/").LastOrDefault()}</b>";

                    if (result["duration"] is ILiteralNode durationNode)
                        optionContent += $" | {FormatDuration(durationNode.Value)}</b>";

                    graphNameDropdown.options.Add(new TMP_Dropdown.OptionData(optionContent));
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
