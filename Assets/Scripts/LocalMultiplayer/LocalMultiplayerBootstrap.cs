using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    public class LocalMultiplayerBootstrap : MonoBehaviour
    {
        private static LocalMultiplayerBootstrap instance;

        public LocalPlayerManager PlayerManager { get; private set; }
        public MatchManager MatchManager { get; private set; }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SetupServices();
        }

        private void SetupServices()
        {
            if (PlayerManager == null)
            {
                PlayerManager = GetComponent<LocalPlayerManager>();
                if (PlayerManager == null)
                {
                    PlayerManager = gameObject.AddComponent<LocalPlayerManager>();
                }
            }

            if (MatchManager == null)
            {
                MatchManager = GetComponent<MatchManager>();
                if (MatchManager == null)
                {
                    MatchManager = gameObject.AddComponent<MatchManager>();
                }
            }

            MatchManager.PlayerManager = PlayerManager;
        }
    }
}
