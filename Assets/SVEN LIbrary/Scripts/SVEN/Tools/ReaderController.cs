using System;
using System.Collections.Generic;
using System.Globalization;
using NaughtyAttributes;
using OWLTime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SVEN.Tools
{
    public class ReaderController : MonoBehaviour
    {
        [SerializeField, OnValueChanged("ResetController")]
        private GraphReader _graphReader;
        private GraphReader GraphReader
        {
            get => _graphReader;
            set
            {
                if (_graphReader != null) _graphReader.OnGraphLoaded -= ResetController;
                _graphReader = value;
                if (_graphReader != null)
                {
                    _graphReader.OnGraphLoaded += ResetController;
                    ResetController();
                }
                //Debug.Log("GraphReader set");
            }
        }

        [SerializeField]
        private Slider slider;
        [SerializeField]
        private TextMeshProUGUI timeText;
        [SerializeField]
        private Toggle playPauseButton;
        [SerializeField]
        private Sprite playSprite;
        [SerializeField]
        private Sprite pauseSprite;

        [SerializeField]
        private Button backwardButton;
        [SerializeField]
        private Button forwardButton;


        private bool _isPlaying;
        private bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                // get first component in childs with image and change sprite
                playPauseButton.GetComponentInChildren<Image>().sprite = _isPlaying ? pauseSprite : playSprite;
            }
        }

        private void Awake()
        {
            GraphReader = _graphReader;
            IsPlaying = false;
        }

        private void OnSliderValueChanged(float value)
        {
            GraphReader.SearchAt(value);
            int hours = (int)value / 3600;
            int minutes = (int)value % 3600 / 60;
            float seconds = value % 60;

            if (hours > 0)
                timeText.text = string.Format(CultureInfo.InvariantCulture, "{0}:{1:D2}:{2:D2}", hours, minutes, (int)seconds);
            else
                timeText.text = string.Format(CultureInfo.InvariantCulture, "{0}:{1:D2}", minutes, (int)seconds);

        }

        /// <summary>
        /// Reset the controller to its default state.
        /// </summary>
        private void ResetController()
        {
            if (GraphReader == null || !GraphReader.IsGraphLoaded) return;
            slider.minValue = 0;
            slider.maxValue = GraphReader.Duration;
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            slider.onValueChanged.AddListener(OnSliderValueChanged);
            playPauseButton.onValueChanged.RemoveListener(OnPlayValueChanged);
            playPauseButton.onValueChanged.AddListener(OnPlayValueChanged);
            backwardButton.onClick.RemoveListener(StepBackward);
            backwardButton.onClick.AddListener(StepBackward);
            forwardButton.onClick.RemoveListener(StepForward);
            forwardButton.onClick.AddListener(StepForward);

            List<Instant> instants = GraphReader.Instants;
            DateTime StartedAt = GraphReader.StartedAt;
            // for each instant, draw a little line above the slider
            foreach (Instant instant in instants)
            {
                float x = (float)(instant.inXSDDateTime - StartedAt).TotalSeconds;
                GameObject line = new GameObject("Line");
                line.transform.SetParent(slider.transform);
                RectTransform rect = line.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(x / slider.maxValue, 1f);
                rect.anchorMax = new Vector2(x / slider.maxValue, 1f);
                rect.sizeDelta = new Vector2(2, 10);
                rect.anchoredPosition = new Vector2(0, 0);
                rect.pivot = new Vector2(0.5f, 0f);
                Image image = line.AddComponent<Image>();
                image.color = Color.gray;
            }
        }

        private void OnPlayValueChanged(bool value)
        {
            IsPlaying = value;
        }

        private void Update()
        {
            if (IsPlaying)
            {
                slider.value += Time.deltaTime;
                if (slider.value >= slider.maxValue)
                {
                    IsPlaying = false;
                }
            }

            // keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Space)) IsPlaying = !IsPlaying;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) StepBackward();
            if (Input.GetKeyDown(KeyCode.RightArrow)) StepForward();
        }

        private void OnDestroy()
        {
            if (GraphReader != null) GraphReader.OnGraphLoaded -= ResetController;
        }

        private void StepForward()
        {
            Instant instant = GraphReader.NextInstant();
            if (instant != null) slider.value = (float)(instant.inXSDDateTime - GraphReader.StartedAt).TotalSeconds;
        }

        private void StepBackward()
        {
            Instant instant = GraphReader.PreviousInstant();
            if (instant != null) slider.value = (float)(instant.inXSDDateTime - GraphReader.StartedAt).TotalSeconds;
        }
    }
}