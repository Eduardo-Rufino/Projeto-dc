using Godot;
using ProjetoDC.Scripts.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoDC.Scripts.Managers
{
    public partial class DatabaseManager : Node
    {
        public static DatabaseManager Instance { get; private set; }

        public override void _Ready()
        {
            Instance = this;
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

                fileName = dir.GetNext();
            }

            GD.Print("Digimons carregados: " + digimons.Count);
        }

        public DigimonData GetDigimon(int id)
        {
            if (digimons.TryGetValue(id, out var digimon))
                return digimon;

            GD.PrintErr("Digimon não encontrado: " + id);
            return null;
        }

        public IEnumerable<DigimonData> GetAllDigimons()
        {
            return digimons.Values.OrderBy(d => d.Id);
        }

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

        private string GetDigimonName(int id)
        {
            var digimon = GetDigimon(id);
            return digimon?.Name ?? $"Desconhecido (ID {id})";
        }

    }
}
