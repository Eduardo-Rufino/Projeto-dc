using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoDC.Scripts.Managers
{
    /// <summary>
    /// Gerencia o carregamento e acesso aos dados estáticos de Digimons e evoluções.
    /// Lê arquivos JSON de pastas específicas e mantém dicionários/listas em memória.
    /// </summary>
    public partial class DatabaseManager : Node
    {
        public static DatabaseManager Instance { get; private set; }

        public override void _Ready()
        {
            Instance = this;

            LoadDigimons();
            LoadEvolutions();
            LoadItems();
            LoadTournaments();
            LoadNpcs();
            LoadQuests();
            LoadExplorationMaps();
            LoadPassives();
            LoadSets();
            LoadJogressRecipes();
        }

        private const string DigimonFolder = "res://Data/Digimon/";
        private const string ItemFolder = "res://Data/Itens/";
        private const string TournamentFolder = "res://Data/Tournaments/";
        private const string NpcFolder = "res://Data/NPCs/";
        private const string QuestFolder = "res://Data/Quests/";
        private const string MapFolder = "res://Data/Maps/";
        private const string PassiveFolder = "res://Data/Passives/";
        private const string SetFolder = "res://Data/Sets/";
        private const string JogressFolder = "res://Data/Jogress/";

        private Dictionary<int, DigimonData> digimons = new();

        private List<EvolutionData> evolutions = new();

        private Dictionary<int, ItemData> items = new();

        private Dictionary<int, TournamentData> tournaments = new();

        private Dictionary<int, NpcData> npcs = new();

        private Dictionary<int, QuestData> quests = new();

        private Dictionary<int, ExplorationMapData> explorationMaps = new();

        private Dictionary<PassiveType, PassiveData> passives = new();

        private Dictionary<int, DigimonSetData> sets = new();

        private Dictionary<int, JogressData> jogressRecipes = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        static DatabaseManager()
        {
            JsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        /// Carrega os arquivos JSON de digimons da pasta configurada e popula o dicionário interno.
        /// Faz verificações básicas (arquivo aberto, extensão .json, IDs duplicados).
        /// </summary>
        public void LoadDigimons()
        {
            string path = DigimonFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de digimons não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();
                try
                {

                    DigimonData? digimon =
                        JsonSerializer.Deserialize<DigimonData>(json, JsonOptions);
                    if (digimons.ContainsKey(digimon.Id))
                    {
                        GD.PrintErr("ID duplicado: " + digimon.Id);
                    }
                    else
                    {
                        digimons.Add(digimon.Id, digimon);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                    fileName = dir.GetNext();
                    continue;

                }
                

                fileName = dir.GetNext();
            }

            GD.Print("Digimons carregados: " + digimons.Count);
        }

        /// <summary>
        /// Retorna o <see cref="DigimonData"/> pelo ID, ou null se não encontrado.
        /// </summary>
        public DigimonData GetDigimon(int id)
        {
            if (digimons.TryGetValue(id, out var digimon))
                return digimon;

            GD.PrintErr("Digimon não encontrado: " + id);
            return null;
        }

        /// <summary>
        /// Retorna todos os Digimons carregados ordenados por ID.
        /// </summary>
        public IEnumerable<DigimonData> GetAllDigimons()
        {
            return digimons.Values.OrderBy(d => d.Id);
        }

        /// <summary>
        /// Carrega arquivos de evoluções (listas) da pasta de evoluções e popula a lista interna.
        /// </summary>
        public void LoadEvolutions()
        {
            string path = "res://Data/Evolutions/";

            using var dir = DirAccess.Open(path);

            if(dir == null)
            {
                GD.PrintErr("Pasta de evolução nao encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while(fileName != "")
            {
                if(fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json")){
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if(file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                List<EvolutionData> evolutionList =
                    JsonSerializer.Deserialize<List<EvolutionData>>(json, JsonOptions);

                if (evolutionList != null)
                {
                    evolutions.AddRange(evolutionList);
                }

                fileName = dir.GetNext();
            }

            GD.Print($"Total de evoluções carregadas: {evolutions.Count}");
        }

        /// <summary>
        /// Retorna a lista de evoluções originadas de um digimon específico.
        /// </summary>
        public List<EvolutionData> GetEvolutionsFrom(int digimonId)
        {
            return evolutions.Where(e => e.FromDigimonId == digimonId).ToList();
        }

        /// <summary>
        /// Carrega os arquivos JSON de itens (Data/Itens/*.json) da mesma forma que
        /// LoadDigimons - um arquivo por item.
        /// </summary>
        public void LoadItems()
        {
            string path = ItemFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de itens não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                try
                {
                    ItemData item = JsonSerializer.Deserialize<ItemData>(json, JsonOptions);

                    if (items.ContainsKey(item.Id))
                    {
                        GD.PrintErr("ID de item duplicado: " + item.Id);
                    }
                    else
                    {
                        items.Add(item.Id, item);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                }

                fileName = dir.GetNext();
            }

            GD.Print("Itens carregados: " + items.Count);
        }

        /// <summary>Retorna o <see cref="ItemData"/> pelo ID, ou null se não encontrado.</summary>
        public ItemData GetItem(int id)
        {
            if (items.TryGetValue(id, out var item))
                return item;

            GD.PrintErr("Item não encontrado: " + id);
            return null;
        }

        /// <summary>Retorna todos os itens carregados, ordenados por ID.</summary>
        public IEnumerable<ItemData> GetAllItems()
        {
            return items.Values.OrderBy(i => i.Id);
        }

        /// <summary>
        /// Carrega os arquivos JSON de campeonatos (Data/Tournaments/*.json), mesmo padrão
        /// de LoadDigimons/LoadItems - um arquivo por campeonato.
        /// </summary>
        public void LoadTournaments()
        {
            string path = TournamentFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de campeonatos não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                try
                {
                    TournamentData tournament = JsonSerializer.Deserialize<TournamentData>(json, JsonOptions);

                    if (tournaments.ContainsKey(tournament.Id))
                    {
                        GD.PrintErr("ID de campeonato duplicado: " + tournament.Id);
                    }
                    else
                    {
                        tournaments.Add(tournament.Id, tournament);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                }

                fileName = dir.GetNext();
            }

            GD.Print("Campeonatos carregados: " + tournaments.Count);
        }

        /// <summary>Retorna o <see cref="TournamentData"/> pelo ID, ou null se não encontrado.</summary>
        public TournamentData GetTournament(int id)
        {
            if (tournaments.TryGetValue(id, out var tournament))
                return tournament;

            GD.PrintErr("Campeonato não encontrado: " + id);
            return null;
        }

        /// <summary>Retorna todos os campeonatos carregados, ordenados por ID.</summary>
        public IEnumerable<TournamentData> GetAllTournaments()
        {
            return tournaments.Values.OrderBy(t => t.Id);
        }

        /// <summary>
        /// Carrega os arquivos JSON de NPCs (Data/NPCs/*.json), mesmo padrão de
        /// LoadDigimons/LoadItems/LoadTournaments - um arquivo por NPC.
        /// </summary>
        public void LoadNpcs()
        {
            string path = NpcFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de NPCs não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                try
                {
                    NpcData npc = JsonSerializer.Deserialize<NpcData>(json, JsonOptions);

                    if (npcs.ContainsKey(npc.Id))
                    {
                        GD.PrintErr("ID de NPC duplicado: " + npc.Id);
                    }
                    else
                    {
                        npcs.Add(npc.Id, npc);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                }

                fileName = dir.GetNext();
            }

            GD.Print("NPCs carregados: " + npcs.Count);
        }

        /// <summary>Retorna o <see cref="NpcData"/> pelo ID, ou null se não encontrado.</summary>
        public NpcData GetNpc(int id)
        {
            if (npcs.TryGetValue(id, out var npc))
                return npc;

            GD.PrintErr("NPC não encontrado: " + id);
            return null;
        }

        /// <summary>Retorna todos os NPCs carregados, ordenados por ID.</summary>
        public IEnumerable<NpcData> GetAllNpcs()
        {
            return npcs.Values.OrderBy(n => n.Id);
        }

        /// <summary>
        /// Carrega os arquivos JSON de quests (Data/Quests/*.json), mesmo padrão de
        /// LoadDigimons/LoadItems/LoadTournaments - um arquivo por quest.
        /// </summary>
        public void LoadQuests()
        {
            string path = QuestFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de quests não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                try
                {
                    QuestData quest = JsonSerializer.Deserialize<QuestData>(json, JsonOptions);

                    if (quests.ContainsKey(quest.Id))
                    {
                        GD.PrintErr("ID de quest duplicado: " + quest.Id);
                    }
                    else
                    {
                        quests.Add(quest.Id, quest);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                }

                fileName = dir.GetNext();
            }

            GD.Print("Quests carregadas: " + quests.Count);
        }

        /// <summary>Retorna o <see cref="QuestData"/> pelo ID, ou null se não encontrado.</summary>
        public QuestData GetQuest(int id)
        {
            if (quests.TryGetValue(id, out var quest))
                return quest;

            GD.PrintErr("Quest não encontrada: " + id);
            return null;
        }

        /// <summary>Retorna todas as quests carregadas, ordenadas por ID.</summary>
        public IEnumerable<QuestData> GetAllQuests()
        {
            return quests.Values.OrderBy(q => q.Id);
        }

        /// <summary>
        /// Carrega os arquivos JSON de áreas de exploração (Data/Maps/*.json), mesmo padrão
        /// de LoadDigimons/LoadItems/LoadTournaments - um arquivo por área.
        /// </summary>
        public void LoadExplorationMaps()
        {
            string path = MapFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de áreas de exploração não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                try
                {
                    ExplorationMapData map = JsonSerializer.Deserialize<ExplorationMapData>(json, JsonOptions);

                    if (explorationMaps.ContainsKey(map.Id))
                    {
                        GD.PrintErr("ID de área de exploração duplicado: " + map.Id);
                    }
                    else
                    {
                        explorationMaps.Add(map.Id, map);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                }

                fileName = dir.GetNext();
            }

            GD.Print("Áreas de exploração carregadas: " + explorationMaps.Count);
        }

        /// <summary>Retorna o <see cref="ExplorationMapData"/> pelo ID, ou null se não
        /// encontrado.</summary>
        public ExplorationMapData GetExplorationMap(int id)
        {
            if (explorationMaps.TryGetValue(id, out var map))
                return map;

            GD.PrintErr("Área de exploração não encontrada: " + id);
            return null;
        }

        /// <summary>Retorna todas as áreas de exploração carregadas, ordenadas por ID.</summary>
        public IEnumerable<ExplorationMapData> GetAllExplorationMaps()
        {
            return explorationMaps.Values.OrderBy(m => m.Id);
        }

        /// <summary>
        /// Carrega os arquivos JSON de passivas (Data/Passives/*.json, ver PASSIVAS_SPEC.md
        /// na raiz do projeto), mesmo padrão de LoadDigimons/LoadItems/LoadNpcs - um arquivo
        /// por passiva.
        /// </summary>
        public void LoadPassives()
        {
            string path = PassiveFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de passivas não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                try
                {
                    PassiveData passive = JsonSerializer.Deserialize<PassiveData>(json, JsonOptions);

                    if (passives.ContainsKey(passive.Id))
                    {
                        GD.PrintErr("ID de passiva duplicado: " + passive.Id);
                    }
                    else
                    {
                        passives.Add(passive.Id, passive);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                }

                fileName = dir.GetNext();
            }

            GD.Print("Passivas carregadas: " + passives.Count);
        }

        /// <summary>Retorna o <see cref="PassiveData"/> pelo ID, ou null se não encontrado.</summary>
        public PassiveData GetPassive(PassiveType id)
        {
            if (passives.TryGetValue(id, out var passive))
                return passive;

            GD.PrintErr("Passiva não encontrada: " + id);
            return null;
        }

        /// <summary>Retorna todas as passivas carregadas, ordenadas por ID.</summary>
        public IEnumerable<PassiveData> GetAllPassives()
        {
            return passives.Values.OrderBy(p => p.Id);
        }

        /// <summary>Carrega as receitas de Jogress (ver JogressData/JogressSystem) - um
        /// arquivo por receita, mesmo padrão de LoadPassives (objeto único, não lista),
        /// já que cada receita é autocontida. Chaveado pelo Digimon resultante
        /// (ToDigimonId) - cada forma só deveria ter uma receita de Jogress.</summary>
        public void LoadJogressRecipes()
        {
            string path = JogressFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de receitas de Jogress não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                try
                {
                    JogressData recipe = JsonSerializer.Deserialize<JogressData>(json, JsonOptions);

                    if (jogressRecipes.ContainsKey(recipe.ToDigimonId))
                    {
                        GD.PrintErr("Receita de Jogress duplicada pro Digimon: " + recipe.ToDigimonId);
                    }
                    else
                    {
                        jogressRecipes.Add(recipe.ToDigimonId, recipe);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                }

                fileName = dir.GetNext();
            }

            GD.Print("Receitas de Jogress carregadas: " + jogressRecipes.Count);
        }

        /// <summary>Receita cujo par de origem (desordenado) bate com idA/idB, ou null se
        /// nenhuma combinação de Jogress existe pra esses dois Digimons.</summary>
        public JogressData GetJogressRecipe(int idA, int idB)
        {
            return jogressRecipes.Values.FirstOrDefault(r => r.MatchesPair(idA, idB));
        }

        /// <summary>Todas as receitas de Jogress onde digimonId é um dos dois Digimons de
        /// origem (A ou B) - usado pelo Guia de Evolução pra mostrar "esse Digimon também
        /// pode virar X via Jogress, com um parceiro Y" junto das evoluções normais.</summary>
        public IEnumerable<JogressData> GetJogressRecipesInvolving(int digimonId)
        {
            return jogressRecipes.Values.Where(r => r.DigimonAId == digimonId || r.DigimonBId == digimonId);
        }

        /// <summary>Retorna todas as receitas de Jogress carregadas, ordenadas pelo
        /// Digimon resultante.</summary>
        public IEnumerable<JogressData> GetAllJogressRecipes()
        {
            return jogressRecipes.Values.OrderBy(r => r.ToDigimonId);
        }

        /// <summary>Carrega os "conjuntos" de Digimon (ver DigimonSetData) - grupos temáticos
        /// que dão bônus permanente ao Center quando toda a lista já foi descoberta pelo
        /// menos uma vez (EncyclopediaScreen mostra a aba de Conjuntos, GameManager decide o
        /// bônus).</summary>
        public void LoadSets()
        {
            string path = SetFolder;

            using var dir = DirAccess.Open(path);

            if (dir == null)
            {
                GD.PrintErr("Pasta de conjuntos não encontrada!");
                return;
            }

            dir.ListDirBegin();

            string fileName = dir.GetNext();

            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                if (!fileName.EndsWith(".json"))
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = path + fileName;

                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PrintErr("Erro ao abrir: " + fullPath);
                    fileName = dir.GetNext();
                    continue;
                }

                string json = file.GetAsText();

                try
                {
                    DigimonSetData set = JsonSerializer.Deserialize<DigimonSetData>(json, JsonOptions);

                    if (sets.ContainsKey(set.Id))
                    {
                        GD.PrintErr("ID de conjunto duplicado: " + set.Id);
                    }
                    else
                    {
                        sets.Add(set.Id, set);
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"Erro ao desserializar {fileName}: {ex.Message}");
                }

                fileName = dir.GetNext();
            }

            GD.Print("Conjuntos carregados: " + sets.Count);
        }

        /// <summary>Retorna todos os conjuntos carregados, ordenados por ID.</summary>
        public IEnumerable<DigimonSetData> GetAllSets()
        {
            return sets.Values.OrderBy(s => s.Id);
        }
    }
}
