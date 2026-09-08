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

            for (int i = 0; i < participants.Count; i++)
            {
                CurrentMatch.AddParticipant(participants[i]);
            }

            IsMatchActive = true;
            Winner = null;
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

        public void ResetForRematch()
        {
            Winner = null;
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
