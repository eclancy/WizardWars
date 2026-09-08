using System.Collections.Generic;
using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    public static class LocalMultiplayerRuntime
    {
        private static LocalPlayerManager cachedPlayerManager;
        private static MatchManager cachedMatchManager;

        public static LocalPlayerManager PlayerManager
        {
            get
            {
                if (cachedPlayerManager == null)
                {
                    var bootstrap = Object.FindObjectOfType<LocalMultiplayerBootstrap>();
                    if (bootstrap != null)
                    {
                        cachedPlayerManager = bootstrap.PlayerManager;
                    }
                }

                return cachedPlayerManager;
            }
        }

        public static MatchManager MatchManager
        {
            get
            {
                if (cachedMatchManager == null)
                {
                    var bootstrap = Object.FindObjectOfType<LocalMultiplayerBootstrap>();
                    if (bootstrap != null)
                    {
                        cachedMatchManager = bootstrap.MatchManager;
                    }
                }

                return cachedMatchManager;
            }
        }

        public static List<PlayerSlotState> GetJoinedSlots()
        {
            var result = new List<PlayerSlotState>();
            if (PlayerManager == null)
            {
                return result;
            }

            foreach (var slot in PlayerManager.PlayerSlots)
            {
                if (slot != null && slot.State != PlayerSlotStateValue.Empty)
                {
                    result.Add(slot);
                }
            }

            return result;
        }

        public static PlayerSlotState GetPlayerSlot(int slotIndex)
        {
            if (PlayerManager == null)
            {
                return null;
            }

            return PlayerManager.GetSlot(slotIndex);
        }

        public static bool TryStartMatch(string stageName)
        {
            if (MatchManager == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(stageName))
            {
                MatchManager.SetSelectedStage(stageName);
            }

            return MatchManager.TryStartMatch();
        }

        public static List<PlayerSlotState> GetLockedPlayers()
        {
            if (MatchManager == null)
            {
                return new List<PlayerSlotState>();
            }

            return MatchManager.GetActiveParticipants();
        }
    }
}
