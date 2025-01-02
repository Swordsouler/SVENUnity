using System;
using System.Collections.Generic;
using System.Globalization;
using NaughtyAttributes;
using OWLTime;
using Sven.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SVEN.GraphReader;

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
        private Slider timeSlider;
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

        [SerializeField]
        private TMP_Dropdown speedDropdown;

        [SerializeField]
        private UILineRenderer contentLine;


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
            timeSlider.minValue = 0;
            timeSlider.maxValue = GraphReader.Duration;
            timeSlider.value = 0;
            timeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
            playPauseButton.onValueChanged.RemoveListener(OnPlayValueChanged);
            playPauseButton.onValueChanged.AddListener(OnPlayValueChanged);
            backwardButton.onClick.RemoveListener(StepBackward);
            backwardButton.onClick.AddListener(StepBackward);
            forwardButton.onClick.RemoveListener(StepForward);
            forwardButton.onClick.AddListener(StepForward);

            DrawContentLine();
        }

        /// <summary>
        /// Draw the content line above the slider.
        /// </summary>
        private void DrawContentLine()
        {
            int meanContentModifier = GraphReader.MeanContentModifier;
            List<InstantDescription> instants = GraphReader.Instants;
            DateTime StartedAt = GraphReader.StartedAt;

            // Define the threshold for the time gap (in seconds)
            float timeThreshold = 1.0f; // Adjust this value as needed

            // Get the RectTransform of the slider
            RectTransform sliderRect = timeSlider.GetComponent<RectTransform>();

            // Calculate the positions for the LineRenderer
            List<Vector2> positions = new();
            for (int i = 0; i < instants.Count; i++)
            {
                InstantDescription instant = instants[i];
                float x = (float)(instant.inXSDDateTime - StartedAt).TotalSeconds;
                float importance = Mathf.Clamp01((float)instant.ContentModifier / meanContentModifier);
                float height = Mathf.Lerp(10, 50, importance); // Adjust the range as needed

                // Calculate the position relative to the slider's dimensions and position
                float normalizedX = x / timeSlider.maxValue;
                float posX = normalizedX * sliderRect.rect.width; // Calculate the x position
                float posY = height + sliderRect.rect.height / 2; // Calculate the y position above the slider

                // Add the current position
                positions.Add(new Vector2(posX, posY));

                // Check the time gap with the next instant
                if (i < instants.Count - 1)
                {
                    float nextX = (float)(instants[i + 1].inXSDDateTime - StartedAt).TotalSeconds;
                    if (nextX - x > timeThreshold)
                    {
                        // Add a point at 0 if the gap exceeds the threshold
                        positions.Add(new Vector2(posX, 0));
                        float nextNormalizedX = nextX / timeSlider.maxValue;
                        float nextPosX = nextNormalizedX * sliderRect.rect.width;
                        positions.Add(new Vector2(nextPosX, 0));
                    }
                }
            }

            // Assign the positions to the LineRenderer
            contentLine.points = positions.ToArray();
            contentLine.SetAllDirty();
        }

        private void OnPlayValueChanged(bool value)
        {
            IsPlaying = !IsPlaying;
        }

        private void Update()
        {
            float speed = speedDropdown.value switch
            {
                0 => 8,
                1 => 4,
                2 => 2,
                3 => 1,
                4 => 0.75f,
                5 => 0.5f,
                6 => 0.25f,
                _ => 1
            };
            if (IsPlaying)
            {
                timeSlider.value += Time.deltaTime * speed;
                if (timeSlider.value >= timeSlider.maxValue)
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

        /// <summary>
        /// Step forward in time.
        /// </summary>
        private void StepForward()
        {
            Instant instant = GraphReader.NextInstant();
            if (instant != null) timeSlider.value = (float)(instant.inXSDDateTime - GraphReader.StartedAt).TotalSeconds;
        }

        /// <summary>
        /// Step backward in time.
        /// </summary>
        private void StepBackward()
        {
            Instant instant = GraphReader.PreviousInstant();
            if (instant != null) timeSlider.value = (float)(instant.inXSDDateTime - GraphReader.StartedAt).TotalSeconds;
        }
    }
}