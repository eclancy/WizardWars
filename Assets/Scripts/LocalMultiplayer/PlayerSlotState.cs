using System;
using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    public enum PlayerSlotStateValue
    {
        Empty = 0,
        Joined = 1,
        Ready = 2,
        Locked = 3
    }

    [Serializable]
    public class PlayerSlotState
    {
        public int SlotIndex;
        public string DeviceId;
        public string PlayerName;
        public PlayerSlotStateValue State;
        public string SelectedWizard;
        public bool IsHuman;
        public bool IsAlive;
        public Color PlayerColor = Color.white;
        public Transform SpawnPoint;
        public GameObject RuntimeController;

        public void Reset()
        {
            DeviceId = string.Empty;
            PlayerName = string.Empty;
            State = PlayerSlotStateValue.Empty;
            SelectedWizard = string.Empty;
            IsHuman = false;
            IsAlive = false;
            PlayerColor = Color.white;
            SpawnPoint = null;
            RuntimeController = null;
        }
    }
}
