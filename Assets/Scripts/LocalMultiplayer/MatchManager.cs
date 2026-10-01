using System.Collections.Generic;
using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    public class MatchManager : MonoBehaviour
    {
        public LocalPlayerManager PlayerManager;

        public bool IsMatchActive { get; private set; }
        public string SelectedStage { get; private set; }
        public PlayerSlotState Winner { get; private set; }
        public MatchConfig CurrentMatch { get; private set; }
        public TeamMode SelectedTeamMode { get; private set; }
        // Set instead of Winner when a Teams match ends; Teams.NoTeam otherwise.
        public int WinningTeam { get; private set; } = Teams.NoTeam;

        private void Awake()
        {
            if (PlayerManager == null)
            {
                PlayerManager = GetComponent<LocalPlayerManager>();
            }

            SelectedStage = "Park";
            CurrentMatch = new MatchConfig();
        }

        public void SetSelectedStage(string stageName)
        {
            if (string.IsNullOrEmpty(stageName))
            {
                return;
            }

            SelectedStage = stageName;
        }

        public void SetTeamMode(TeamMode mode)
        {
            SelectedTeamMode = mode;
        }

        public bool TryStartMatch()
        {
            if (PlayerManager == null)
            {
                return false;
            }

            var participants = new List<PlayerSlotState>();

            for (int i = 0; i < PlayerManager.PlayerSlots.Count; i++)
            {
                var slot = PlayerManager.PlayerSlots[i];
                if (slot.State == PlayerSlotStateValue.Locked && !string.IsNullOrEmpty(slot.SelectedWizard))
                {
                    participants.Add(slot);
                }
            }

            if (participants.Count == 0)
            {
                return false;
            }

            CurrentMatch = new MatchConfig();
            CurrentMatch.StageName = SelectedStage;
            CurrentMatch.Mode = participants.Count == 1 ? MatchMode.SoloWithAiFallback : MatchMode.LocalMultiplayer;
            CurrentMatch.CountdownSeconds = 3;
            CurrentMatch.TeamMode = SelectedTeamMode;

            for (int i = 0; i < participants.Count; i++)
            {
                CurrentMatch.AddParticipant(participants[i]);
            }

            IsMatchActive = true;
            Winner = null;
            WinningTeam = Teams.NoTeam;
            return true;
        }

        public List<PlayerSlotState> GetActiveParticipants()
        {
            if (PlayerManager == null)
            {
                return new List<PlayerSlotState>();
            }

            var participants = new List<PlayerSlotState>();
            for (int i = 0; i < PlayerManager.PlayerSlots.Count; i++)
            {
                var slot = PlayerManager.PlayerSlots[i];
                if (slot != null && slot.State == PlayerSlotStateValue.Locked && !string.IsNullOrEmpty(slot.SelectedWizard))
                {
                    participants.Add(slot);
                }
            }

            return participants;
        }

        public void RegisterWinner(PlayerSlotState winningSlot)
        {
            Winner = winningSlot;
            IsMatchActive = false;
        }

        public void RegisterWinningTeam(int team)
        {
            WinningTeam = team;
            IsMatchActive = false;
        }

        public void ResetForRematch()
        {
            Winner = null;
            WinningTeam = Teams.NoTeam;
            IsMatchActive = false;
            CurrentMatch = new MatchConfig();
            SelectedStage = "Park";

            if (PlayerManager != null)
            {
                PlayerManager.ResetSelectionState();
            }
        }
    }
}
