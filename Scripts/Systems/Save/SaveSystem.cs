using Godot;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Save;
using System.IO;
using System.Text.Json;

namespace ProjetoDC.Scripts.Systems.Save
{
    public class SaveSystem
    {
        private const string SavePath = "save.json";


        public void SaveGame(SaveData save)
        {
            string json = JsonSerializer.Serialize(
                save,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            string path = ProjectSettings.GlobalizePath(
                "user://" + SavePath
            );

            File.WriteAllText(path, json);

            GD.Print("Jogo salvo!");
        }


        public SaveData LoadGame()
        {
            string path = ProjectSettings.GlobalizePath(
                "user://" + SavePath
            );

            if (!File.Exists(path))
            {
                GD.Print("Nenhum save encontrado.");
                return null;
            }


            string json = File.ReadAllText(path);

            SaveData save = JsonSerializer.Deserialize<SaveData>(json);

            GD.Print("Jogo carregado!");

            return save;
        }

        public bool HasSave()
        {
            string path = ProjectSettings.GlobalizePath(
                "user://" + SavePath
            );

            return File.Exists(path);
        }
    }
}