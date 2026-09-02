using Godot;
using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Managers
{
    /// <summary>
    /// Autoload que toca a música de fundo do jogo, sempre em loop: tema do Center enquanto o
    /// jogador está no Center, tema de batalha enquanto uma luta está rolando - troca sozinha
    /// nas duas direções (ver GameManager.TeamBattleStarted/TeamBattleFinished), sem nenhuma
    /// tela precisar chamar isso na volta da batalha.
    /// </summary>
    public partial class MusicManager : Node
    {
        public static MusicManager Instance { get; private set; }

        private const string CenterMusicPath =
            "res://Assets/Audio/Music/Digimon World - Night Time in File City ~ LoFi Remix ♪.wav";

        private const string BattleMusicPath =
            "res://Assets/Audio/Music/Digimon World OST - Major Battle - TeffiniWynn (youtube).mp3";

        // Música de fundo não deve competir com efeitos sonoros - 0 dB (o padrão do
        // AudioStreamPlayer) tocava as duas faixas muito mais alto que o resto do jogo. Esse é
        // o volume de referência pro slider de 100% em SettingsScreen - o valor real aplicado
        // é esse + o ganho do slider (ver ApplyVolume).
        private const float BaseMusicVolumeDb = -12f;

        // Abaixo disso o slider é tratado como mudo (evita depender de Mathf.LinearToDb(0),
        // que tende a -infinito).
        private const float MutedThreshold = 0.001f;
        private const float MutedVolumeDb = -80f;

        private const string SettingsPath = "user://settings.cfg";

        private AudioStreamPlayer _player;
        private AudioStream _centerMusic;
        private AudioStream _battleMusic;

        // 0 (mudo) a 1 (volume de referência cheio) - persistido em user://settings.cfg,
        // separado do save do jogo (é preferência do jogador, não estado de gameplay).
        private float _volumeLinear = 1f;

        public float VolumeLinear => _volumeLinear;

        public override void _Ready()
        {
            Instance = this;

            // Telas bloqueantes (batalha, loja, etc.) pausam o resto do jogo via
            // GameManager.RequestPause - sem isso a música pararia junto.
            ProcessMode = ProcessModeEnum.Always;

            _player = new AudioStreamPlayer();
            AddChild(_player);

            LoadSettings();

            _centerMusic = LoadLoopingStream(CenterMusicPath);
            _battleMusic = LoadLoopingStream(BattleMusicPath);

            // GameManager pode ainda estar terminando sua inicialização.
            CallDeferred(nameof(ConnectToGameManager));
        }

        /// <summary>Chamado pelo slider de volume em SettingsScreen - <paramref name="linear"/>
        /// vai de 0 (mudo) a 1 (volume de referência cheio).</summary>
        public void SetVolume(float linear)
        {
            _volumeLinear = Mathf.Clamp(linear, 0f, 1f);

            ApplyVolume();
            SaveSettings();
        }

        private void ApplyVolume()
        {
            _player.VolumeDb = _volumeLinear <= MutedThreshold
                ? MutedVolumeDb
                : BaseMusicVolumeDb + Mathf.LinearToDb(_volumeLinear);
        }

        private void LoadSettings()
        {
            var config = new ConfigFile();

            if (config.Load(SettingsPath) == Error.Ok)
                _volumeLinear = Mathf.Clamp((float)config.GetValue("audio", "music_volume", 1f), 0f, 1f);

            ApplyVolume();
        }

        private void SaveSettings()
        {
            var config = new ConfigFile();

            // Ignora o resultado - se o arquivo ainda não existir (primeira vez salvando),
            // Load falha e config continua vazio, o que é exatamente o que queremos aqui.
            config.Load(SettingsPath);

            config.SetValue("audio", "music_volume", _volumeLinear);
            config.Save(SettingsPath);
        }

        private void ConnectToGameManager()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.TeamBattleStarted -= PlayBattleMusic;
            GameManager.Instance.TeamBattleStarted += PlayBattleMusic;

            GameManager.Instance.TeamBattleFinished -= OnTeamBattleFinished;
            GameManager.Instance.TeamBattleFinished += OnTeamBattleFinished;
        }

        private void OnTeamBattleFinished(BattleResult result)
        {
            PlayCenterMusic();
        }

        public void PlayCenterMusic() => Play(_centerMusic);

        public void PlayBattleMusic() => Play(_battleMusic);

        private void Play(AudioStream stream)
        {
            if (stream == null)
                return;

            // Já é a música tocando agora - não reinicia ela do começo à toa (ex.: se
            // PlayCenterMusic for chamado de novo enquanto o jogador já está no Center).
            if (_player.Stream == stream && _player.Playing)
                return;

            _player.Stream = stream;
            _player.Play();
        }

        /// <summary>Carrega um AudioStream e força loop diretamente na instância carregada, em
        /// vez de depender da configuração de import do arquivo (o .import só é gerado depois
        /// que o editor escaneia o asset pela primeira vez) - funciona tanto pra .wav
        /// (AudioStreamWav) quanto .mp3 (AudioStreamMP3).</summary>
        private static AudioStream LoadLoopingStream(string path)
        {
            var stream = GD.Load<AudioStream>(path);

            if (stream == null)
            {
                GD.PrintErr($"MusicManager: não foi possível carregar '{path}'.");
                return null;
            }

            switch (stream)
            {
                case AudioStreamWav wav:
                    // LoopEnd é em número de samples e vem 0 por padrão (nenhum loop foi
                    // configurado no .import do arquivo) - setar só o LoopMode sem isso cria
                    // uma região de loop de tamanho zero, que toca silêncio (ficava parecendo
                    // que a música nem tocava, era só um loop instantâneo travado no sample 0).
                    wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
                    wav.LoopBegin = 0;
                    wav.LoopEnd = (int)(wav.GetLength() * wav.MixRate);
                    break;

                case AudioStreamMP3 mp3:
                    mp3.Loop = true;
                    break;
            }

            return stream;
        }
    }
}
