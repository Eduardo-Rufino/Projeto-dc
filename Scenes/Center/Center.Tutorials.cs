using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.UI;

/// <summary>
/// Tutoriais automáticos do Center (ver TutorialPopup): boas-vindas no jogo novo e o tópico de cada
/// tela/função na primeira vez que ela é usada. Os Ids são os da TutorialScreen, que continua com a
/// lista completa no botão 📖.
/// </summary>
public partial class Center
{
    private TutorialPopup _tutorialPopup;

    private void SetupAutoTutorials()
    {
        _tutorialPopup = new TutorialPopup();
        AddChild(_tutorialPopup);

        // Só aparece com o Center na tela (não durante batalha/exploração), fora da animação de
        // evolução, com o aviso de morte já fechado e sem Carne/Remédio no meio do arrastar.
        _tutorialPopup.CanShow = () =>
            Visible
            && _canvasLayer.Visible
            && !_isProcessingEvolutionQueue
            && !_evolutionOverlay.Visible
            && !_digimonDeathScreen.Visible
            && !_isPlacingFood
            && !_isPlacingMedicine;

        var center = GameManager.Instance.Save.Center;

        // Boas-vindas: o básico pra não começar perdido.
        _tutorialPopup.AddRule(() => true, "primeiros_passos", "necessidades", "relogio", "ovos");

        // O primeiro Digimon nasceu: a partir daqui a idade conta.
        _tutorialPopup.AddRule(() => center.Digimons.Count > 0, "evolucao", "morte", "renomear");

        // Telas abertas pela primeira vez.
        _tutorialPopup.AddRule(() => _shopScreen.Visible, "areas_center");
        _tutorialPopup.AddRule(() => _inventoryScreen.Visible, "inventario");
        _tutorialPopup.AddRule(() => _battleTypeScreen.Visible, "campeonatos");
        _tutorialPopup.AddRule(() => _tournamentListScreen.Visible || _medalsScreen.Visible, "calendario");
        _tutorialPopup.AddRule(() => _teamSelectionScreen.Visible, "batalha", "vantagens_tipo");
        _tutorialPopup.AddRule(() => _evolutionGuideScreen.Visible || _evolutionCapacityScreen.Visible, "evolucao");
        _tutorialPopup.AddRule(() => _jogressPickScreen.Visible, "jogress");
        _tutorialPopup.AddRule(() => _baseEditorScreen.Visible, "editor_bases");
        _tutorialPopup.AddRule(() => _explorationListScreen.Visible, "exploracao");
        _tutorialPopup.AddRule(() => _encyclopediaScreen.Visible, "enciclopedia");
        _tutorialPopup.AddRule(() => _digimonDetailScreen.Visible, "passivas");

        // Acontecimentos.
        _tutorialPopup.AddRule(() => _digimonDeathScreen.Visible, "morte");
        _tutorialPopup.AddRule(() => center.RecruitedNpcIds.Count > 0, "npcs_recrutados");
    }
}
