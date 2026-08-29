using Godot;
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
        }

        private const string DigimonFolder = "res://Data/Digimon/";

        private Dictionary<int, DigimonData> digimons = new();

        private List<EvolutionData> evolutions = new();

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
                    GD.Print($"Evoluções carregadas de {fileName}:");
                    
                    foreach (var evo in evolutionList)
                    {
                        string fromName = GetDigimonName(evo.FromDigimonId);
                        string toName = GetDigimonName(evo.ToDigimonId);
                        GD.Print($"  → {fromName} evolui para {toName} (Nível {evo.RequiredLevel})");
                    }
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
            var evoList = evolutions.Where(e => e.FromDigimonId == digimonId).ToList();
            
            string digimonName = GetDigimonName(digimonId);
            GD.Print($"Evoluções possíveis para {digimonName}: {evoList.Count}");
            
            foreach (var evo in evoList)
            {
                string toName = GetDigimonName(evo.ToDigimonId);
            }
            
            return evoList;
        }

        /// <summary>
        /// Recupera o nome do digimon pelo ID, usado apenas para logs/legibilidade.
        /// </summary>
        private string GetDigimonName(int id)
        {
            var digimon = GetDigimon(id);
            return digimon?.Name ?? $"Desconhecido (ID {id})";
        }
    }
}
