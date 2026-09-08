using System.Collections.Generic;
using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    public enum MatchMode
    {
        LocalMultiplayer,
        SoloWithAiFallback
    }

    public class MatchConfig
    {
        public string StageName = "Park";
        public MatchMode Mode = MatchMode.LocalMultiplayer;
        public int CountdownSeconds = 3;
        public List<PlayerSlotState> Participants = new List<PlayerSlotState>();
        public bool AutoAssignAiFallback;

        public MatchConfig()
        {
            Participants = new List<PlayerSlotState>();
        }

        public void AddParticipant(PlayerSlotState slot)
        {
            if (slot == null)
            {
                return;
            }

            if (!Participants.Contains(slot))
            {
                Participants.Add(slot);
            }
        }
    }
}
