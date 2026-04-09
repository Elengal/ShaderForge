using System.IO;
using HarmonyLib;
using Verse;

namespace ShaderForge
{
    /// <summary>
    /// Сохранение настроек мода в Config/ShaderForge/Config.xml вместо Mod_ShaderForge_ShaderForgeMod.xml.
    /// При первом запуске копирует старый файл в новое место и удаляет его после проверки.
    /// Патч ДОЛЖЕН применяться ДО первого вызова GetSettings в конструкторе ShaderForgeMod.
    /// </summary>
    [HarmonyPatch(typeof(LoadedModManager))]
    [HarmonyPatch("GetSettingsFilename")]
    [HarmonyPatch(new[] { typeof(string), typeof(string) })]
    public static class LoadedModManager_GetSettingsFilename_Patch
    {
        private const string ModIdentifier = "ShaderForge";
        private const string ModHandleName = "ShaderForgeMod";
        private const string Subfolder = "ShaderForge";
        private const string ConfigFileName = "Config.xml";

        public static bool Prefix(string modIdentifier, string modHandleName, ref string __result)
        {
            if (modIdentifier != ModIdentifier || modHandleName != ModHandleName)
                return true;

            string configDir = GenFilePaths.ConfigFolderPath;
            string dir = Path.Combine(configDir, Subfolder);
            string newPath = Path.Combine(dir, ConfigFileName);
            string oldPath = Path.Combine(configDir, GenText.SanitizeFilename($"Mod_{ModIdentifier}_{ModHandleName}.xml"));

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(newPath) && File.Exists(oldPath))
            {
                try
                {
                    File.Copy(oldPath, newPath);
                    if (File.Exists(newPath))
                    {
                        try { File.Delete(oldPath); }
                        catch (System.Exception exDel)
                        {
                            Log.Warning($"ShaderForge: старый конфиг скопирован, но не удалось удалить: {exDel.Message}");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"ShaderForge: не удалось скопировать старый конфиг в {newPath}: {ex.Message}");
                }
            }

            __result = newPath;
            return false;
        }
    }
}
