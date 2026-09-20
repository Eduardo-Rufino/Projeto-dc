using Godot;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Save;
using System;
using System.IO;
using System.Text.Json;

namespace ProjetoDC.Scripts.Systems.Save
{
    public class SaveSystem
    {
        private const string SavePath = "save.json";
        private const string BackupPath = "save.json.bak";

        public void SaveGame(SaveData save)
        {
            if (save == null)
            {
                GD.PrintErr("SaveGame chamado com SaveData nulo. Save cancelado para não sobrescrever o arquivo existente.");
                return;
            }

            string path = ProjectSettings.GlobalizePath("user://" + SavePath);
            string backupPath = ProjectSettings.GlobalizePath("user://" + BackupPath);
            string tempPath = path + ".tmp";

            try
            {
                string json = JsonSerializer.Serialize(
                    save,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                );

                // Escreve num arquivo temporário e só troca pelo definitivo no final,
                // pra nunca deixar o save.json pela metade se algo interromper no meio.
                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    File.Copy(path, backupPath, true);
                }

                File.Copy(tempPath, path, true);
                File.Delete(tempPath);

                GD.Print("Jogo salvo!");
            }
            catch (Exception e)
            {
                GD.PrintErr($"Falha ao salvar o jogo: {e.Message}");
            }
        }

        public SaveData LoadGame()
        {
            string path = ProjectSettings.GlobalizePath("user://" + SavePath);

            if (!File.Exists(path))
            {
                GD.Print("Nenhum save encontrado.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);

                SaveData save = JsonSerializer.Deserialize<SaveData>(json);

                if (save == null)
                {
                    GD.PrintErr("Save carregado como nulo. Arquivo preservado para inspeção, tentando backup.");

                    BackupCorruptSave(path);

                    return LoadBackup();
                }

                GD.Print("Jogo carregado!");

                return save;
            }
            catch (Exception e)
            {
                GD.PrintErr($"Falha ao carregar o save ({e.GetType().Name}: {e.Message}). Arquivo preservado para inspeção, tentando backup.");

                BackupCorruptSave(path);

                return LoadBackup();
            }
        }

        private SaveData LoadBackup()
        {
            string backupPath = ProjectSettings.GlobalizePath("user://" + BackupPath);

            if (!File.Exists(backupPath))
                return null;

            try
            {
                string json = File.ReadAllText(backupPath);

                SaveData save = JsonSerializer.Deserialize<SaveData>(json);

                if (save != null)
                {
                    GD.Print("Jogo carregado a partir do backup!");
                }

                return save;
            }
            catch (Exception e)
            {
                GD.PrintErr($"Backup também está corrompido: {e.Message}");

                return null;
            }
        }

        private void BackupCorruptSave(string path)
        {
            try
            {
                string corruptPath = path + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";

                File.Copy(path, corruptPath, true);

                GD.PrintErr($"Cópia do save corrompido guardada em: {corruptPath}");
            }
            catch (Exception e)
            {
                GD.PrintErr($"Não foi possível preservar o save corrompido: {e.Message}");
            }
        }

        /// <summary>Apaga o save e o backup (só o save.json, o .bak e um .tmp que tenha sobrado
        /// de uma gravação interrompida) - usado por "Novo Jogo" no menu principal. As cópias
        /// .corrupt-* de saves ilegíveis ficam, são só diagnóstico. Devolve false se algum
        /// arquivo não pôde ser apagado.</summary>
        public bool DeleteSave()
        {
            string path = ProjectSettings.GlobalizePath("user://" + SavePath);
            string backupPath = ProjectSettings.GlobalizePath("user://" + BackupPath);

            bool ok = true;

            foreach (var file in new[] { path, backupPath, path + ".tmp" })
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch (Exception e)
                {
                    ok = false;

                    GD.PrintErr($"Não foi possível apagar '{file}': {e.Message}");
                }
            }

            if (ok)
                GD.Print("Save apagado.");

            return ok;
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
