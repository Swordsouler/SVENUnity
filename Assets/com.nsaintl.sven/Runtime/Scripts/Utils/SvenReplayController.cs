// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.GraphManagement;
using Sven.OwlTime;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sven.Utils
{
    /// <summary>
    /// Controller to manage the SvenReader.
    /// </summary>
    public class SvenReplayController : MonoBehaviour
    {
        /// <summary>
        /// GraphReader to read the graph.
        /// </summary>
        /*[SerializeField, OnValueChanged("ResetController")]
        private GraphReader _graphReader;
        /// <summary>
        /// GraphReader to read the graph.
        /// </summary>
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
        }*/

        /// <summary>
        /// Slider to control the time.
        /// </summary>
        [SerializeField]
        private Slider _timeSlider;

        /// <summary>
        /// Text to show the time.
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI _timeText;

        /// <summary>
        /// Toggle to play or pause the time.
        /// </summary>
        [SerializeField]
        private Toggle _playPauseButton;

        /// <summary>
        /// Sprites for the play and pause buttons.
        /// </summary>
        [SerializeField]
        private Sprite _playSprite;

        /// <summary>
        /// Sprites for the play and pause buttons.
        /// </summary>
        [SerializeField]
        private Sprite _pauseSprite;

        /// <summary>
        /// Button to step backward in time.
        /// </summary>
        [SerializeField]
        private Button _backwardButton;

        /// <summary>
        /// Button to step forward in time.
        /// </summary>
        [SerializeField]
        private Button _forwardButton;

        /// <summary>
        /// Dropdown to select the speed of the time.
        /// </summary>
        [SerializeField]
        private TMP_Dropdown _speedDropdown;

        /// <summary>
        /// Flag to check if the time is playing.
        /// </summary>
        private bool _isPlaying;
        /// <summary>
        /// Flag to check if the time is playing.
        /// </summary>
        private bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                // get first component in childs with image and change sprite
                _playPauseButton.GetComponentInChildren<Image>().sprite = _isPlaying ? _pauseSprite : _playSprite;
            }
        }

        private void Awake()
        {
            IsPlaying = false;
            Load();
        }

        private async void Load()
        {
            await GraphManager.LoadInstantsFromEndpoint();
            ResetController();
            OnSliderValueChanged(0);
        }

        private void OnSliderValueChanged(float value)
        {
            _ = GraphManager.RetrieveSceneFromEndpoint(GraphManager.SearchInstant(value));
            int hours = (int)value / 3600;
            int minutes = (int)value % 3600 / 60;
            float seconds = value % 60;

            if (hours > 0)
                _timeText.text = string.Format(CultureInfo.InvariantCulture, "{0}:{1:D2}:{2:D2}", hours, minutes, (int)seconds);
            else
                _timeText.text = string.Format(CultureInfo.InvariantCulture, "{0}:{1:D2}", minutes, (int)seconds);
        }

        /// <summary>
        /// Reset the controller to its default state.
        /// </summary>
        private void ResetController()
        {
            //if (GraphReader == null || !GraphReader.IsGraphLoaded) return;
            _timeSlider.minValue = 0;
            _timeSlider.maxValue = GraphManager.Duration;
            _timeSlider.value = 0;
            _timeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            _timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
            _playPauseButton.onValueChanged.RemoveListener(OnPlayValueChanged);
            _playPauseButton.onValueChanged.AddListener(OnPlayValueChanged);
            _backwardButton.onClick.RemoveListener(StepBackward);
            _backwardButton.onClick.AddListener(StepBackward);
            _forwardButton.onClick.RemoveListener(StepForward);
            _forwardButton.onClick.AddListener(StepForward);
        }

        private void OnPlayValueChanged(bool value)
        {
            IsPlaying = !IsPlaying;
        }

        private void Update()
        {
            float speed = _speedDropdown.value switch
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
                _timeSlider.value += Time.deltaTime * speed;
                if (_timeSlider.value >= _timeSlider.maxValue)
                {
                    IsPlaying = false;
                }
            }

            // keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Space)) IsPlaying = !IsPlaying;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) StepBackward();
            if (Input.GetKeyDown(KeyCode.RightArrow)) StepForward();
            if (Input.GetKeyDown(KeyCode.KeypadPlus)) _speedDropdown.value = Mathf.Clamp(_speedDropdown.value - 1, 0, _speedDropdown.options.Count - 1);
            if (Input.GetKeyDown(KeyCode.KeypadMinus)) _speedDropdown.value = Mathf.Clamp(_speedDropdown.value + 1, 0, _speedDropdown.options.Count - 1);
        }

        /// <summary>
        /// Step forward in time.
        /// </summary>
        private void StepForward()
        {
            Instant instant = GraphManager.NextInstant();
            if (instant != null) _timeSlider.value = (float)(instant.inXSDDateTime - GraphManager.StartedAt).TotalSeconds;
        }

        /// <summary>
        /// Step backward in time.
        /// </summary>
        private void StepBackward()
        {
            Instant instant = GraphManager.PreviousInstant();
            if (instant != null) _timeSlider.value = (float)(instant.inXSDDateTime - GraphManager.StartedAt).TotalSeconds;
        }
    }
}