using System;
using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    public class LegacyGlobalStateBridge : MonoBehaviour
    {
        private const string GlobalHandlerObjectName = "GlobalHandler";

        [SerializeField]
        private LocalPlayerManager playerManager;

        [SerializeField]
        private MatchManager matchManager;

        private Component legacyGlobalHandler;

        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (!initialized)
            {
                Initialize();
                return;
            }

            SyncLegacyState();
        }

        private void Initialize()
        {
            if (playerManager == null)
            {
                playerManager = GetComponent<LocalPlayerManager>();
            }

            if (playerManager == null)
            {
                playerManager = gameObject.AddComponent<LocalPlayerManager>();
            }

            if (matchManager == null)
            {
                matchManager = GetComponent<MatchManager>();
            }

            if (matchManager == null)
            {
                matchManager = gameObject.AddComponent<MatchManager>();
            }

            matchManager.PlayerManager = playerManager;

            var globalHandlerObject = GameObject.Find(GlobalHandlerObjectName);
            if (globalHandlerObject != null)
            {
                legacyGlobalHandler = globalHandlerObject.GetComponent("GlobalHandler");
            }

            initialized = true;
        }

        private void SyncLegacyState()
        {
            if (legacyGlobalHandler == null || playerManager == null)
            {
                return;
            }

            var player1Joined = GetLegacyBool("player1Joined");
            var player2Joined = GetLegacyBool("player2Joined");
            var player3Joined = GetLegacyBool("player3Joined");
            var player4Joined = GetLegacyBool("player4Joined");

            var player1Locked = GetLegacyBool("player1Locked");
            var player2Locked = GetLegacyBool("player2Locked");
            var player3Locked = GetLegacyBool("player3Locked");
            var player4Locked = GetLegacyBool("player4Locked");

            ApplySlotState(0, player1Joined, player1Locked, GetLegacyString("tempPlayer1"));
            ApplySlotState(1, player2Joined, player2Locked, GetLegacyString("tempPlayer2"));
            ApplySlotState(2, player3Joined, player3Locked, GetLegacyString("tempPlayer3"));
            ApplySlotState(3, player4Joined, player4Locked, GetLegacyString("tempPlayer4"));

            // Teams mode and each player's team are chosen on Character Select and
            // held by GlobalHandler; mirror them so the two state layers agree.
            matchManager.SetTeamMode(GetLegacyBool("teamsMode") ? TeamMode.Teams : TeamMode.FreeForAll);
            ApplySlotTeam(0, player1Joined, GetLegacyInt("player1Team"));
            ApplySlotTeam(1, player2Joined, GetLegacyInt("player2Team"));
            ApplySlotTeam(2, player3Joined, GetLegacyInt("player3Team"));
            ApplySlotTeam(3, player4Joined, GetLegacyInt("player4Team"));

            var selectedStage = GetLegacyInt("selectedStage");
            if (selectedStage > 0)
            {
                matchManager.SetSelectedStage("Park");
            }
        }

        public void RegisterLegacySlot(int slotIndex, bool joined, bool locked, string selectedWizard, string playerName)
        {
            if (playerManager == null)
            {
                return;
            }

            var slot = playerManager.GetSlot(slotIndex);
            if (slot == null)
            {
                return;
            }

            if (!joined)
            {
                if (slot.State != PlayerSlotStateValue.Empty)
                {
                    slot.Reset();
                }
                return;
            }

            slot.DeviceId = string.IsNullOrEmpty(slot.DeviceId) ? "legacy-slot-" + slotIndex : slot.DeviceId;
            slot.PlayerName = string.IsNullOrEmpty(playerName) ? "Player" + (slotIndex + 1) : playerName;
            slot.State = locked ? PlayerSlotStateValue.Locked : PlayerSlotStateValue.Joined;
            slot.IsHuman = true;
            slot.SelectedWizard = string.IsNullOrEmpty(selectedWizard) ? slot.SelectedWizard : selectedWizard;
        }

        public int GetJoinedPlayerCount()
        {
            if (playerManager == null)
            {
                return 0;
            }

            return playerManager.GetJoinedPlayerCount();
        }

        public bool StartMatchForLegacyStage()
        {
            if (matchManager == null)
            {
                return false;
            }

            if (legacyGlobalHandler != null)
            {
                var selectedStageField = legacyGlobalHandler.GetType().GetField("selectedStage");
                if (selectedStageField != null)
                {
                    var selectedStage = Convert.ToInt32(selectedStageField.GetValue(legacyGlobalHandler));
                    SetSelectedStage(selectedStage);
                }
            }

            return matchManager.TryStartMatch();
        }

        public void RegisterWinner(string playerName)
        {
            if (matchManager == null || string.IsNullOrEmpty(playerName) || playerManager == null)
            {
                return;
            }

            foreach (var slot in playerManager.PlayerSlots)
            {
                if (slot != null && slot.PlayerName == playerName)
                {
                    matchManager.RegisterWinner(slot);
                    return;
                }
            }
        }

        public void RegisterWinningTeam(int team)
        {
            if (matchManager != null)
            {
                matchManager.RegisterWinningTeam(team);
            }
        }

        public void ResetRuntimeState()
        {
            if (matchManager != null)
            {
                matchManager.ResetForRematch();
            }

            if (playerManager != null)
            {
                playerManager.ResetSelectionState();
            }
        }

        public void SetSelectedStage(int stageNumber)
        {
            if (matchManager == null)
            {
                return;
            }

            if (stageNumber == 1)
            {
                matchManager.SetSelectedStage("Park");
            }
            else if (stageNumber == 2)
            {
                matchManager.SetSelectedStage("Park");
            }
            else if (stageNumber == 3)
            {
                matchManager.SetSelectedStage("Park");
            }
            else if (stageNumber == 4)
            {
                matchManager.SetSelectedStage("Park");
            }
        }

        private void ApplySlotState(int slotIndex, bool joined, bool locked, string wizardName)
        {
            RegisterLegacySlot(slotIndex, joined, locked, wizardName, "Player" + (slotIndex + 1));
        }

        private void ApplySlotTeam(int slotIndex, bool joined, int team)
        {
            var slot = playerManager.GetSlot(slotIndex);
            if (slot != null)
            {
                slot.Team = joined ? team : Teams.NoTeam;
            }
        }

        private bool GetLegacyBool(string fieldName)
        {
            if (legacyGlobalHandler == null)
            {
                return false;
            }

            var field = legacyGlobalHandler.GetType().GetField(fieldName);
            if (field == null)
            {
                return false;
            }

            var value = field.GetValue(legacyGlobalHandler);
            return value != null && (bool)value;
        }

        private string GetLegacyString(string fieldName)
        {
            if (legacyGlobalHandler == null)
            {
                return string.Empty;
            }

            var field = legacyGlobalHandler.GetType().GetField(fieldName);
            if (field == null)
            {
                return string.Empty;
            }

            var value = field.GetValue(legacyGlobalHandler);
            return value == null ? string.Empty : value.ToString();
        }

        private int GetLegacyInt(string fieldName)
        {
            if (legacyGlobalHandler == null)
            {
                return 0;
            }

            var field = legacyGlobalHandler.GetType().GetField(fieldName);
            if (field == null)
            {
                return 0;
            }

            var value = field.GetValue(legacyGlobalHandler);
            return value == null ? 0 : Convert.ToInt32(value);
        }
    }
}
