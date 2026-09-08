using System.Collections.Generic;
using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    public class LocalPlayerManager : MonoBehaviour
    {
        [SerializeField]
        private int maxSlots = 4;

        private readonly List<PlayerSlotState> playerSlots = new List<PlayerSlotState>();

        public int MaxSlots
        {
            get { return maxSlots; }
        }

        public List<PlayerSlotState> PlayerSlots
        {
            get { return playerSlots; }
        }

        private void Awake()
        {
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            playerSlots.Clear();

            for (int i = 0; i < maxSlots; i++)
            {
                var slot = new PlayerSlotState();
                slot.SlotIndex = i;
                slot.State = PlayerSlotStateValue.Empty;
                slot.Reset();
                playerSlots.Add(slot);
            }
        }

        public PlayerSlotState GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerSlots.Count)
            {
                return null;
            }

            return playerSlots[slotIndex];
        }

        public PlayerSlotState JoinPlayer(string deviceId, string playerName = "Player")
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return null;
            }

            for (int i = 0; i < playerSlots.Count; i++)
            {
                var slot = playerSlots[i];
                if (slot.State == PlayerSlotStateValue.Empty || slot.DeviceId == deviceId)
                {
                    slot.DeviceId = deviceId;
                    slot.PlayerName = string.IsNullOrEmpty(playerName) ? "Player" + (i + 1) : playerName;
                    slot.State = PlayerSlotStateValue.Joined;
                    slot.IsHuman = true;
                    return slot;
                }
            }

            return null;
        }

        public bool LeavePlayer(int slotIndex)
        {
            var slot = GetSlot(slotIndex);
            if (slot == null)
            {
                return false;
            }

            slot.Reset();
            return true;
        }

        public bool SetReady(int slotIndex, bool ready)
        {
            var slot = GetSlot(slotIndex);
            if (slot == null || slot.State == PlayerSlotStateValue.Empty)
            {
                return false;
            }

            slot.State = ready ? PlayerSlotStateValue.Ready : PlayerSlotStateValue.Joined;
            return true;
        }

        public bool LockSelection(int slotIndex, string wizardName)
        {
            var slot = GetSlot(slotIndex);
            if (slot == null || slot.State == PlayerSlotStateValue.Empty)
            {
                return false;
            }

            slot.SelectedWizard = wizardName;
            slot.State = PlayerSlotStateValue.Locked;
            return true;
        }

        public int GetJoinedPlayerCount()
        {
            var count = 0;

            for (int i = 0; i < playerSlots.Count; i++)
            {
                if (playerSlots[i].State != PlayerSlotStateValue.Empty)
                {
                    count++;
                }
            }

            return count;
        }

        public bool CanStartMatch()
        {
            for (int i = 0; i < playerSlots.Count; i++)
            {
                var slot = playerSlots[i];
                if (slot.State == PlayerSlotStateValue.Empty)
                {
                    continue;
                }

                if (slot.State == PlayerSlotStateValue.Locked && !string.IsNullOrEmpty(slot.SelectedWizard))
                {
                    return true;
                }
            }

            return false;
        }

        public void ResetSelectionState()
        {
            for (int i = 0; i < playerSlots.Count; i++)
            {
                var slot = playerSlots[i];
                if (slot.State == PlayerSlotStateValue.Empty)
                {
                    continue;
                }

                slot.State = PlayerSlotStateValue.Joined;
                slot.SelectedWizard = string.Empty;
                slot.IsAlive = false;
                slot.RuntimeController = null;
            }
        }
    }
}
