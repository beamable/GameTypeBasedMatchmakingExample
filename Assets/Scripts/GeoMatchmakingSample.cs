using System;
using System.Collections.Generic;
using System.Text;
using Beamable;
using Beamable.Experimental.Api.Matchmaking;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MatchmakingExample
{
    public class GeoMatchmakingSample : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private TextMeshProUGUI _status;
        [SerializeField] private TMP_Dropdown _regionDropdown;
        [SerializeField] private Button _startMatchmakingButton;
        [SerializeField] private Button _stopMatchmakingButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private List<GeoGameTypeRef> _gameTypesRefs;
#pragma warning restore CS0649

        private IBeamableAPI _api;

        private readonly List<GeoGameType> _gameTypes = new List<GeoGameType>();
        private GeoGameTypeRef _selectedGameTypeRef;
        private MatchmakingState _currentState;
        private MatchmakingHandle _currentHandle;

        private async void Start()
        {
            _regionDropdown.interactable =
                _startMatchmakingButton.interactable =
                    _stopMatchmakingButton.interactable = false;

            _api = await Beamable.API.Instance;

            foreach (GeoGameTypeRef gameTypeRef in _gameTypesRefs)
            {
                GeoGameType geoGameType = await _api.ContentService.GetContent(gameTypeRef);
                _gameTypes.Add(geoGameType);
            }

            _regionDropdown.options = SetupOptions();
            _regionDropdown.onValueChanged.AddListener(OnRegionChanged);

            _startMatchmakingButton.onClick.RemoveAllListeners();
            _startMatchmakingButton.onClick.AddListener(StartMatchmaking);

            _stopMatchmakingButton.onClick.RemoveAllListeners();
            _stopMatchmakingButton.onClick.AddListener(StopMatchmaking);

            _exitButton.onClick.RemoveAllListeners();
            _exitButton.onClick.AddListener(Exit);

            SetDefaults();
            Refresh();
        }

        private void SetDefaults()
        {
            int defaultRegionId = 0;
            _regionDropdown.SetValueWithoutNotify(defaultRegionId);
            _regionDropdown.RefreshShownValue();
            OnRegionChanged(defaultRegionId);

            ChangeStatus(MatchmakingState.Cancelled);
        }

        private void ChangeStatus(MatchmakingState state)
        {
            _currentState = state;
            _status.text = DebugMessage();
        }

        private string DebugMessage()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"Current status: {_currentState}");

            if (_currentState != MatchmakingState.Cancelled &&
                _currentState != MatchmakingState.Timeout &&
                _currentHandle != null)
            {
                builder.AppendLine($"Ticket id: {_currentHandle.Tickets[0].ticketId}");
            }

            if (_currentState == MatchmakingState.Ready && _currentHandle != null)
            {
                Match match = _currentHandle.Match;

                if (match != null)
                {
                    builder.AppendLine($"Match id: {match.matchId}");

                    foreach (Team team in match.teams)
                    {
                        builder.AppendLine($"Team name: {team.name}");
                        builder.Append("Players: ");

                        foreach (string player in team.players)
                        {
                            builder.Append($"{player}");

                            if (player == _api.User.id.ToString())
                            {
                                builder.Append(" <- (You)");
                            }

                            builder.Append(" ");
                        }

                        builder.AppendLine();
                    }
                }
            }

            return builder.ToString();
        }

        private void Refresh()
        {
            _startMatchmakingButton.interactable = _currentState == MatchmakingState.Cancelled ||
                                                   _currentState == MatchmakingState.Timeout;
            _stopMatchmakingButton.interactable = _currentState == MatchmakingState.Searching;
            
            _regionDropdown.interactable =
                (_currentState == MatchmakingState.Cancelled || _currentState == MatchmakingState.Timeout) &&
                _regionDropdown.options.Count > 0;
        }

        private void StartMatchmaking()
        {
            _api.Experimental.MatchmakingService.StartMatchmaking(_selectedGameTypeRef.Id, MatchmakingHandle,
                MatchmakingHandle, MatchmakingHandle,
                TimeSpan.FromSeconds(10)).Then(MatchmakingHandle);
        }

        private void StopMatchmaking()
        {
            _currentHandle.Cancel().Then(MatchmakingHandle);
        }

        private void MatchmakingHandle(MatchmakingHandle handle)
        {
            _currentHandle = handle;
            ChangeStatus(handle.State);
            Refresh();
        }

        private void OnRegionChanged(int regionId)
        {
            _selectedGameTypeRef = _gameTypesRefs[regionId];
        }

        private List<TMP_Dropdown.OptionData> SetupOptions()
        {
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            foreach (GeoGameType geoGameType in _gameTypes)
            {
                TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(BuildName(geoGameType));
                options.Add(optionData);
            }

            return options;
        }

        private string BuildName(GeoGameType ggt)
        {
            if (ggt.teams.Count < 2)
            {
                return ggt.RegionName;
            }

            return $"{ggt.teams[0].maxPlayers} vs. {ggt.teams[1].maxPlayers} - {ggt.RegionName}";
        }

        private void Exit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}