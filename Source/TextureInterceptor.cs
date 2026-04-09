using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShaderForge
{
    /// <summary>
    /// ГЛАВНЫЙ ПЕРЕХВАТЧИК — перехватывает загрузку КАЖДОЙ текстуры в RimWorld.
    /// 
    /// ContentFinder<Texture2D>.Get() — ЕДИНСТВЕННАЯ дверь через которую
    /// проходят ВСЕ текстуры игры. Один патч = вся игра перерисована.
    /// 
    /// v2.0 — поддержка: цветокоррекция, погодные оверлеи, пер-биом стили,
    /// маски по def'ам, профили производительности.
    /// </summary>
    public static class TextureInterceptor
    {
        // Кэш уже обработанных текстур (не обрабатывать дважды)
        private static readonly Dictionary<int, bool> _processedCache = new Dictionary<int, bool>();
        private static readonly HashSet<string> _blacklist = new HashSet<string>();

        // Счётчик для логов
        private static int _processedCount = 0;
        private static int _skippedCount = 0;

        // Кэш def-масок (парсится один раз)
        private static string[] _cachedDefMaskList = null;
        private static string _cachedDefMaskText = null;
        private static bool _cachedDefMaskIsBlacklist = true;

        // Реестр обработанных текстур (для принудительного переприменения)
        private class TextureEntry
        {
            public string itemPath;
            public Texture2D original;
            public Texture2D processed;
        }
        private static readonly Dictionary<int, TextureEntry> _textureRegistry = new Dictionary<int, TextureEntry>();

        /// <summary>
        /// Добавляет путь в чёрный список (не перерисовывать).
        /// </summary>
        public static void AddToBlacklist(string path)
        {
            if (!string.IsNullOrEmpty(path))
                _blacklist.Add(path);
        }

        /// <summary>
        /// Очищает кэш обработанных текстур.
        /// </summary>
        public static void ClearCache()
        {
            _processedCache.Clear();
            _processedCount = 0;
            _skippedCount = 0;
        }

        /// <summary>
        /// Принудительно переприменяет настройки ко всем загруженным текстурам.
        /// Использует сохранённые оригиналы из реестра.
        /// </summary>
        public static int ReprocessAll()
        {
            if (_textureRegistry.Count == 0) return 0;

            var mod = ShaderForgeMod.Instance;
            if (mod == null) return 0;
            var settings = mod.GetSettingsInternal();
            if (settings == null || !settings.Enabled) return 0;

            int reprocessed = 0;
            int errors = 0;

            foreach (var kvp in _textureRegistry)
            {
                try
                {
                    var entry = kvp.Value;
                    if (entry.original == null || entry.processed == null) continue;

                    // Получить пиксели оригинала
                    Texture2D readable = MakeReadable(entry.original);
                    if (readable == null) { errors++; continue; }

                    Color[] pixels = readable.GetPixels();

                    // Определить материал
                    MaterialType material = MaterialAnalyzer.AnalyzeImage(pixels);

                    // Стиль
                    RenderStyle activeStyle = GetActiveStyle(entry.itemPath, settings);
                    float activeIntensity = GetActiveIntensity(entry.itemPath, settings);

                    if (activeStyle != RenderStyle.None)
                    {
                        pixels = ProceduralProcessor.Process(pixels, readable.width, readable.height,
                            material, activeStyle, activeIntensity);
                    }

                    // Цветокоррекция
                    if (HasColorCorrection(settings))
                    {
                        pixels = ProceduralProcessor.ApplyColorCorrection(pixels, settings.GetColorCorrection());
                    }

                    // Погода (с усилением для Heavy профиля)
                    if (settings.ShouldApplyWeather())
                    {
                        float effectiveWeatherIntensity = settings.WeatherIntensity;
                        if (settings.Performance == PerformanceProfile.High)
                            effectiveWeatherIntensity = Mathf.Min(1f, effectiveWeatherIntensity * 1.5f);
                        pixels = ProceduralProcessor.ApplyWeatherOverlay(pixels, readable.width, readable.height,
                            settings.Weather, effectiveWeatherIntensity);
                    }

                    // Применить к текстуре, которую использует игра
                    entry.processed.SetPixels(pixels);
                    entry.processed.Apply(false);

                    reprocessed++;
                }
                catch
                {
                    errors++;
                }
            }

            _processedCount = reprocessed;
            _skippedCount += errors;

            Log.Message($"ShaderForge v2: принудительно переработано {reprocessed} текстур ({errors} ошибок)");
            return reprocessed;
        }

        /// <summary>
        /// Статистика обработки.
        /// </summary>
        public static string GetStats()
        {
            return $"Обработано: {_processedCount} | Пропущено: {_skippedCount} | В кэше: {_processedCache.Count} | Реестр: {_textureRegistry.Count}";
        }

        /// <summary>
        /// ГЛАВНЫЙ МЕТОД — перехватывает текстуру и перерисовывает.
        /// Вызывается через Harmony Postfix после загрузки каждой текстуры.
        /// </summary>
        public static Texture2D InterceptTexture(string itemPath, Texture2D original)
        {
            if (original == null || string.IsNullOrEmpty(itemPath))
                return original;

            // Проверяем настройки мода
            var mod = ShaderForgeMod.Instance;
            if (mod == null) return original;
            var settings = mod.GetSettingsInternal();
            if (settings == null || !settings.Enabled)
                return original;

            // Проверяем чёрный список
            string pathLower = itemPath.ToLower();
            foreach (string bl in _blacklist)
            {
                if (pathLower.Contains(bl.ToLower()))
                {
                    _skippedCount++;
                    return original;
                }
            }

            // Кэш — не обрабатывать дважды
            int hash = itemPath.GetHashCode();
            if (_processedCache.ContainsKey(hash))
                return original;

            // Размер — не обрабатывать слишком большие / маленькие
            if (original.width > 2048 || original.height > 2048 || original.width < 8 || original.height < 8)
            {
                _skippedCount++;
                return original;
            }

            try
            {
                // Фильтрация по категории
                if (!ShouldProcess(itemPath, settings))
                {
                    _skippedCount++;
                    return original;
                }

                // Фильтрация по def-маскам
                if (settings.UseDefMasks && !CheckDefMask(itemPath, settings))
                {
                    _skippedCount++;
                    return original;
                }

                // Определяем стиль (пер-биом или основной)
                RenderStyle activeStyle = GetActiveStyle(itemPath, settings);
                float activeIntensity = GetActiveIntensity(itemPath, settings);

                if (activeStyle == RenderStyle.None && !settings.ShouldApplyWeather()
                    && !HasColorCorrection(settings))
                {
                    _skippedCount++;
                    return original;
                }

                // === ПРОЦЕСС ===

                // 1. Сделать текстуру читаемой
                Texture2D readable = MakeReadable(original);
                if (readable == null)
                {
                    _skippedCount++;
                    return original;
                }

                // 2. Получить пиксели
                Color[] pixels = readable.GetPixels();

                // 3. Определить материал
                MaterialType material = MaterialAnalyzer.AnalyzeImage(pixels);

                // 4. Перерисовать (стиль)
                if (activeStyle != RenderStyle.None)
                {
                    pixels = ProceduralProcessor.Process(pixels, readable.width, readable.height,
                        material, activeStyle, activeIntensity);
                }

                // 5. Цветокоррекция
                if (HasColorCorrection(settings))
                {
                    pixels = ProceduralProcessor.ApplyColorCorrection(pixels, settings.GetColorCorrection());
                }

                // 6. Погодный оверлей (усиление ×1.5 для Heavy профиля)
                if (settings.ShouldApplyWeather())
                {
                    float effectiveWeatherIntensity = settings.WeatherIntensity;
                    if (settings.Performance == PerformanceProfile.High)
                        effectiveWeatherIntensity = Mathf.Min(1f, effectiveWeatherIntensity * 1.5f);
                    pixels = ProceduralProcessor.ApplyWeatherOverlay(pixels, readable.width, readable.height,
                        settings.Weather, effectiveWeatherIntensity);
                }

                // 7. Применить
                readable.SetPixels(pixels);
                readable.Apply(false);

                // 8. Зареестрировать для переприменения
                _textureRegistry[hash] = new TextureEntry
                {
                    itemPath = itemPath,
                    original = original,
                    processed = readable
                };

                // 9. Закешировать
                _processedCache[hash] = true;
                _processedCount++;

                // Лог (первые 10)
                if (_processedCount <= 10)
                {
                    string extra = activeStyle != settings.Style ? $" [биом:{activeStyle}]" : "";
                    Log.Message($"ShaderForge v2: обработал '{itemPath}' [{material}]{extra} ({readable.width}x{readable.height})");
                }

                return readable;
            }
            catch (Exception ex)
            {
                if (_processedCount <= 3)
                {
                    Log.Warning($"ShaderForge v2: ошибка '{itemPath}': {ex.Message}");
                }
                _skippedCount++;
                return original;
            }
        }

        /// <summary>
        /// Проверяет нужно ли обрабатывать текстуру по пути.
        /// Фильтрует по категориям из настроек.
        /// </summary>
        private static bool ShouldProcess(string itemPath, ShaderForgeSettings settings)
        {
            string path = itemPath.ToLower();

            // Убираем ведущий слэш если есть, для единообразия
            path = path.TrimStart('/');

            if (path.StartsWith("ui/") || path.StartsWith("gui/"))
                return settings.ProcessUI;

            if (path.StartsWith("things/") || path.StartsWith("items/"))
                return settings.ProcessItems;

            if (path.StartsWith("terrain/"))
                return settings.ProcessTerrain;

            if (path.StartsWith("buildings/"))
                return settings.ProcessBuildings;

            if (path.StartsWith("pawns/") || path.Contains("/pawns/") || path.Contains("/pawn/"))
                return settings.ProcessPawns;

            if (path.StartsWith("plants/") || path.StartsWith("plant/"))
                return settings.ProcessPlants;

            return true;
        }

        /// <summary>
        /// Проверяет текстуру по маске def'ов.
        /// </summary>
        private static bool CheckDefMask(string itemPath, ShaderForgeSettings settings)
        {
            string[] maskList = GetDefMaskList(settings);
            if (maskList.Length == 0)
                return true; // Пустой список = пропускаем всё

            string pathLower = itemPath.ToLower();

            if (settings.DefMaskIsBlacklist)
            {
                // Blacklist — пропускаем если совпадает
                foreach (string mask in maskList)
                {
                    if (pathLower.Contains(mask.ToLower().Trim()))
                        return false;
                }
                return true;
            }
            else
            {
                // Whitelist — пропускаем только если совпадает
                foreach (string mask in maskList)
                {
                    if (pathLower.Contains(mask.ToLower().Trim()))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Получает кэшированный список def-масок.
        /// </summary>
        private static string[] GetDefMaskList(ShaderForgeSettings settings)
        {
            if (_cachedDefMaskText != settings.DefMaskText
                || _cachedDefMaskIsBlacklist != settings.DefMaskIsBlacklist)
            {
                _cachedDefMaskList = settings.GetDefMaskList();
                _cachedDefMaskText = settings.DefMaskText;
                _cachedDefMaskIsBlacklist = settings.DefMaskIsBlacklist;
            }
            return _cachedDefMaskList ?? new string[0];
        }

        /// <summary>
        /// Определяет активный стиль для текстуры (пер-биом или основной).
        /// </summary>
        private static RenderStyle GetActiveStyle(string itemPath, ShaderForgeSettings settings)
        {
            if (!settings.UsePerBiomeStyles || settings.BiomeOverrideStyle == RenderStyle.None)
                return settings.Style;

            string path = itemPath.ToLower();

            // Пер-биом стиль применяется только к террейну и растениям
            bool isTerrainOrPlant = path.Contains("/terrain/") || path.Contains("/plants/");
            if (!isTerrainOrPlant)
                return settings.Style;

            return settings.BiomeOverrideStyle != RenderStyle.None
                ? settings.BiomeOverrideStyle
                : settings.Style;
        }

        /// <summary>
        /// Определяет активную интенсивность для текстуры (пер-биом или основная).
        /// </summary>
        private static float GetActiveIntensity(string itemPath, ShaderForgeSettings settings)
        {
            if (!settings.UsePerBiomeStyles || settings.BiomeOverrideStyle == RenderStyle.None)
                return settings.Intensity;

            string path = itemPath.ToLower();
            bool isTerrainOrPlant = path.Contains("/terrain/") || path.Contains("/plants/");

            return isTerrainOrPlant ? settings.BiomeOverrideIntensity : settings.Intensity;
        }

        /// <summary>
        /// Проверяет, есть ли цветокоррекция в настройках.
        /// </summary>
        private static bool HasColorCorrection(ShaderForgeSettings settings)
        {
            return Mathf.Abs(settings.HueShift) > 0.1f
                || Mathf.Abs(settings.Saturation - 1f) > 0.01f
                || Mathf.Abs(settings.Brightness - 1f) > 0.01f
                || Mathf.Abs(settings.Contrast - 1f) > 0.01f
                || Mathf.Abs(settings.TintR - 1f) > 0.01f
                || Mathf.Abs(settings.TintG - 1f) > 0.01f
                || Mathf.Abs(settings.TintB - 1f) > 0.01f;
        }

        /// <summary>
        /// Делает текстуру читаемой для GetPixels/SetPixels.
        /// </summary>
        private static Texture2D MakeReadable(Texture2D source)
        {
            try
            {
                if (source.isReadable)
                    return source;

                var readable = TextureAtlasHelper.MakeReadableTextureInstance(source);
                return readable;
            }
            catch
            {
                try
                {
                    RenderTexture tmp = RenderTexture.GetTemporary(
                        source.width, source.height, 0,
                        RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

                    Graphics.Blit(source, tmp);

                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = tmp;

                    Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
                    copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                    copy.Apply(false);

                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(tmp);

                    return copy;
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    // ================================================================
    // HARMONY PATCH (StaticConstructorOnStartup — применяется раньше всего)
    // ================================================================

    /// <summary>
    /// Postfix патч на ContentFinder&lt;Texture2D&gt;.Get().
    /// Вызывается ПОСЛЕ загрузки каждой текстуры.
    /// StaticConstructorOnStartup гарантирует патч ДО конструктора мода.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ShaderForgePatch
    {
        static ShaderForgePatch()
        {
            var harmony = new Harmony("com.shaderforge.textureinterceptor");

            try
            {
                var original = typeof(ContentFinder<Texture2D>).GetMethod("Get",
                    new[] { typeof(string), typeof(bool) });

                if (original != null)
                {
                    var postfix = new HarmonyMethod(typeof(ShaderForgePatch), "Postfix");
                    harmony.Patch(original, postfix: postfix);
                    Log.Message("ShaderForge v2.0: патч на ContentFinder<Texture2D>.Get() установлен");
                }
                else
                {
                    Log.Error("ShaderForge v2.0: не удалось найти ContentFinder<Texture2D>.Get()!");
                }
            }
            catch (Exception ex)
            {
                Log.Error("ShaderForge v2.0: ошибка установки патча текстур: " + ex.Message);
            }
        }

        static void Postfix(string itemPath, ref Texture2D __result)
        {
            if (__result != null)
            {
                __result = TextureInterceptor.InterceptTexture(itemPath, __result);
            }
        }
    }
}
