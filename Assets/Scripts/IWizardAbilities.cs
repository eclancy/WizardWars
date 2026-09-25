// Lets the HUD ask a wizard which abilities are off cooldown without knowing which
// wizard it is. CharacterSelect adds the wizard component at runtime, so PlayerStats
// looks this interface up lazily rather than in Awake.
public interface IWizardAbilities
{
    // slot: 1 = basic, 2 = second, 3 = third. The ultimate is not included; it has its
    // own charge meter in the HUD.
    bool IsAbilityReady(int slot);
}
