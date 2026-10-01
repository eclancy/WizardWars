using System.Collections.Generic;
using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    public enum MatchMode
    {
        LocalMultiplayer,
        SoloWithAiFallback
    }

    // Free-for-All is the default. Teams turns off friendly fire and ends the match
    // when only one team has anyone left standing.
    public enum TeamMode
    {
        FreeForAll,
        Teams
    }

    public class MatchConfig
    {
        public string StageName = "Park";
        public MatchMode Mode = MatchMode.LocalMultiplayer;
        public TeamMode TeamMode = TeamMode.FreeForAll;
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
