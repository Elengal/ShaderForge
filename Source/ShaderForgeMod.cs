using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShaderForge
{
    /// <summary>
    /// Настройки ShaderForge — что перерисовывать и как.
    /// v2.0 — пресеты, профили, цветокоррекция, погода, пер-биом, маски, preview.
    /// </summary>
    public class ShaderForgeSettings : ModSettings
    {
        // === ОСНОВНОЕ ===
        public bool Enabled = true;
        public RenderStyle Style = RenderStyle.BattleWorn;
        public float Intensity = 0.5f;

        // === ФИЛЬТРЫ ПО КАТЕГОРИЯМ ===
        public bool ProcessUI = false;
        public bool ProcessItems = true;
        public bool ProcessTerrain = true;
        public bool ProcessBuildings = true;
        public bool ProcessPawns = false;
        public bool ProcessPlants = true;

        // === ПРОФИЛЬ ПРОИЗВОДИТЕЛЬНОСТИ (v2.0) ===
        public PerformanceProfile Performance = PerformanceProfile.Low;

        // === ЦВЕТОКОРРЕКЦИЯ (v2.0) ===
        public float HueShift = 0f;
        public float Saturation = 1f;
        public float Brightness = 1f;
        public float Contrast = 1f;
        public float TintR = 1f;
        public float TintG = 1f;
        public float TintB = 1f;

        // === ПОГОДНЫЙ ОВЕРЛЕЙ (v2.0, статический) ===
        public WeatherOverlayType Weather = WeatherOverlayType.None;
        public float WeatherIntensity = 0.3f;

        // === ПЕР-БИОМ СТИЛИ (v2.0) ===
        public bool UsePerBiomeStyles = false;
        public BiomeCategory SelectedBiome = BiomeCategory.Auto;
        public RenderStyle BiomeOverrideStyle = RenderStyle.None;
        public float BiomeOverrideIntensity = 0.5f;

        // === МАСКИ ПО DEF'АМ (v2.0) ===
        public bool UseDefMasks = false;
        public bool DefMaskIsBlacklist = true; // true = blacklist, false = whitelist
        public string DefMaskText = "";       // через запятую

        // === ПРЕСЕТЫ (v2.0) ===
        public string CurrentPreset = "Custom";

        // === ПРЕВЬЮ (v2.0) ===
        public bool PreviewEnabled = true;

        // ============================================================
        // СЕРИАЛИЗАЦИЯ
        // ============================================================

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref Enabled, "SF2_Enabled", true);
            Scribe_Values.Look(ref Style, "SF2_Style", RenderStyle.BattleWorn);
            Scribe_Values.Look(ref Intensity, "SF2_Intensity", 0.5f);

            Scribe_Values.Look(ref ProcessUI, "SF2_ProcessUI", false);
            Scribe_Values.Look(ref ProcessItems, "SF2_ProcessItems", true);
            Scribe_Values.Look(ref ProcessTerrain, "SF2_ProcessTerrain", true);
            Scribe_Values.Look(ref ProcessBuildings, "SF2_ProcessBuildings", true);
            Scribe_Values.Look(ref ProcessPawns, "SF2_ProcessPawns", false);
            Scribe_Values.Look(ref ProcessPlants, "SF2_ProcessPlants", true);

            Scribe_Values.Look(ref Performance, "SF2_Performance", PerformanceProfile.Low);

            Scribe_Values.Look(ref HueShift, "SF2_HueShift", 0f);
            Scribe_Values.Look(ref Saturation, "SF2_Saturation", 1f);
            Scribe_Values.Look(ref Brightness, "SF2_Brightness", 1f);
            Scribe_Values.Look(ref Contrast, "SF2_Contrast", 1f);
            Scribe_Values.Look(ref TintR, "SF2_TintR", 1f);
            Scribe_Values.Look(ref TintG, "SF2_TintG", 1f);
            Scribe_Values.Look(ref TintB, "SF2_TintB", 1f);

            Scribe_Values.Look(ref Weather, "SF2_Weather", WeatherOverlayType.None);
            Scribe_Values.Look(ref WeatherIntensity, "SF2_WeatherIntensity", 0.3f);

            Scribe_Values.Look(ref UsePerBiomeStyles, "SF2_UsePerBiomeStyles", false);
            Scribe_Values.Look(ref SelectedBiome, "SF2_SelectedBiome", BiomeCategory.Auto);
            Scribe_Values.Look(ref BiomeOverrideStyle, "SF2_BiomeOverrideStyle", RenderStyle.None);
            Scribe_Values.Look(ref BiomeOverrideIntensity, "SF2_BiomeOverrideIntensity", 0.5f);

            Scribe_Values.Look(ref UseDefMasks, "SF2_UseDefMasks", false);
            Scribe_Values.Look(ref DefMaskIsBlacklist, "SF2_DefMaskIsBlacklist", true);
            Scribe_Values.Look(ref DefMaskText, "SF2_DefMaskText", "");

            Scribe_Values.Look(ref CurrentPreset, "SF2_CurrentPreset", "Custom");
            Scribe_Values.Look(ref PreviewEnabled, "SF2_PreviewEnabled", true);
        }

        // ============================================================
        // УТИЛИТЫ
        // ============================================================

        /// <summary>
        /// Возвращает настройки цветокоррекции как объект.
        /// </summary>
        public ColorCorrectionSettings GetColorCorrection()
        {
            return new ColorCorrectionSettings
            {
                HueShift = HueShift,
                Saturation = Saturation,
                Brightness = Brightness,
                Contrast = Contrast,
                TintColor = new Color(TintR, TintG, TintB, 1f)
            };
        }

        /// <summary>
        /// Проверяет, нужно ли применять погоду при текущем профиле.
        /// </summary>
        public bool ShouldApplyWeather()
        {
            if (Weather == WeatherOverlayType.None) return false;
            return Performance >= PerformanceProfile.Medium;
        }

        /// <summary>
        /// Возвращает список имён def'ов из текстового поля.
        /// </summary>
        public string[] GetDefMaskList()
        {
            if (string.IsNullOrWhiteSpace(DefMaskText))
                return new string[0];
            return DefMaskText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Применяет пресет к настройкам.
        /// </summary>
        public void ApplyPreset(ShaderForgePreset preset)
        {
            Style = preset.Style;
            Intensity = preset.Intensity;
            HueShift = preset.HueShift;
            Saturation = preset.Saturation;
            Brightness = preset.Brightness;
            Contrast = preset.Contrast;
            TintR = preset.TintR;
            TintG = preset.TintG;
            TintB = preset.TintB;
            Weather = preset.Weather;
            WeatherIntensity = preset.WeatherIntensity;
            Performance = preset.Performance;
            CurrentPreset = preset.Name;
        }

        /// <summary>
        /// Сбрасывает все настройки к значениям по умолчанию.
        /// </summary>
        public void ResetToDefaults()
        {
            Enabled = true;
            Style = RenderStyle.BattleWorn;
            Intensity = 0.5f;
            ProcessUI = false;
            ProcessItems = true;
            ProcessTerrain = true;
            ProcessBuildings = true;
            ProcessPawns = false;
            ProcessPlants = true;
            Performance = PerformanceProfile.Low;
            HueShift = 0f;
            Saturation = 1f;
            Brightness = 1f;
            Contrast = 1f;
            TintR = 1f;
            TintG = 1f;
            TintB = 1f;
            Weather = WeatherOverlayType.None;
            WeatherIntensity = 0.3f;
            UsePerBiomeStyles = false;
            SelectedBiome = BiomeCategory.Auto;
            BiomeOverrideStyle = RenderStyle.None;
            BiomeOverrideIntensity = 0.5f;
            UseDefMasks = false;
            DefMaskIsBlacklist = true;
            DefMaskText = "";
            CurrentPreset = "Custom";
            PreviewEnabled = true;
        }
    }

    // ================================================================
    // ПРЕСЕТЫ
    // ================================================================

    /// <summary>
    /// Пресет — предустановленная комбинация настроек.
    /// </summary>
    public class ShaderForgePreset
    {
        public string Name;
        public string Description;
        public RenderStyle Style;
        public float Intensity;
        public float HueShift;
        public float Saturation;
        public float Brightness;
        public float Contrast;
        public float TintR, TintG, TintB;
        public WeatherOverlayType Weather;
        public float WeatherIntensity;
        public PerformanceProfile Performance;

        public ShaderForgePreset(string name, string description, RenderStyle style,
            float intensity = 0.5f, float hueShift = 0f, float saturation = 1f,
            float brightness = 1f, float contrast = 1f,
            float tintR = 1f, float tintG = 1f, float tintB = 1f,
            WeatherOverlayType weather = WeatherOverlayType.None,
            float weatherIntensity = 0.3f,
            PerformanceProfile performance = PerformanceProfile.Low)
        {
            Name = name;
            Description = description;
            Style = style;
            Intensity = intensity;
            HueShift = hueShift;
            Saturation = saturation;
            Brightness = brightness;
            Contrast = contrast;
            TintR = tintR;
            TintG = tintG;
            TintB = tintB;
            Weather = weather;
            WeatherIntensity = weatherIntensity;
            Performance = performance;
        }
    }

    // ================================================================
    // ГЛАВНЫЙ КЛАСС МОДА
    // ================================================================

    /// <summary>
    /// Главный класс мода — точка входа.
    /// v2.0 — полная переработка UI, пресеты, все настройки.
    /// </summary>
    public class ShaderForgeMod : Mod
    {
        public static ShaderForgeMod Instance { get; private set; }
        public ShaderForgeSettings GetSettingsInternal() => (ShaderForgeSettings)base.GetSettings<ShaderForgeSettings>();

        // Встроенные пресеты
        public static readonly ShaderForgePreset[] Presets = {
            new ShaderForgePreset("Custom", "Пользовательские настройки", RenderStyle.BattleWorn, 0.5f),
            new ShaderForgePreset("Ядерная Зима", "Замороженный мир после ядерной войны",
                RenderStyle.Frozen, 0.7f, saturation: 0.7f, brightness: 0.9f, contrast: 1.1f,
                tintR: 0.8f, tintG: 0.85f, tintB: 1.0f,
                weather: WeatherOverlayType.Snow, weatherIntensity: 0.4f),
            new ShaderForgePreset("Токсичная Пустошь", "Радиоактивные пустоши с кислотными дождями",
                RenderStyle.Wasteland, 0.6f, hueShift: 10f, saturation: 0.8f, brightness: 0.95f, contrast: 1.05f,
                tintR: 0.9f, tintG: 1.0f, tintB: 0.85f,
                weather: WeatherOverlayType.Ash, weatherIntensity: 0.25f),
            new ShaderForgePreset("Древние Руины", "Забытый храм в джунглях",
                RenderStyle.Ancient, 0.65f, hueShift: -5f, saturation: 1.1f, brightness: 0.9f, contrast: 1.1f,
                tintR: 0.95f, tintG: 1.0f, tintB: 0.9f,
                weather: WeatherOverlayType.Rain, weatherIntensity: 0.2f),
            new ShaderForgePreset("Магический Мир", "Фэнтези с магическим свечением",
                RenderStyle.Mystic, 0.5f, hueShift: 20f, saturation: 1.2f, brightness: 1.05f, contrast: 1.15f,
                tintR: 0.85f, tintG: 0.9f, tintB: 1.1f,
                weather: WeatherOverlayType.Fog, weatherIntensity: 0.2f),
            new ShaderForgePreset("Пыльный Переход", "Выжженная солнцем пустыня",
                RenderStyle.Desert, 0.6f, hueShift: 15f, saturation: 0.75f, brightness: 1.1f, contrast: 1.05f,
                tintR: 1.05f, tintG: 0.95f, tintB: 0.8f,
                weather: WeatherOverlayType.Dust, weatherIntensity: 0.35f),
            new ShaderForgePreset("Обожжённая Земля", "После прохода огненной бури",
                RenderStyle.Scorched, 0.7f, saturation: 0.6f, brightness: 0.85f, contrast: 1.2f,
                tintR: 1.0f, tintG: 0.85f, tintB: 0.8f,
                weather: WeatherOverlayType.Ash, weatherIntensity: 0.3f),
            new ShaderForgePreset("Заражённый Лес", "Мутации и ядовитая растительность",
                RenderStyle.Toxic, 0.5f, hueShift: -10f, saturation: 1.3f, brightness: 0.95f, contrast: 1.1f,
                tintR: 0.8f, tintG: 1.05f, tintB: 0.85f,
                weather: WeatherOverlayType.Rain, weatherIntensity: 0.25f),
            new ShaderForgePreset("Заброшенный Мир", "Заросшие руины цивилизации",
                RenderStyle.Overgrown, 0.55f, saturation: 1.1f, brightness: 0.95f, contrast: 1.0f,
                tintR: 0.9f, tintG: 1.0f, tintB: 0.9f,
                weather: WeatherOverlayType.Rain, weatherIntensity: 0.15f),
        };

        // UI состояние
        private Vector2 _scrollPos;
        private Texture2D _previewTexture;
        private float _lastPreviewUpdate;
        private bool _previewNeedsUpdate = true;

        // Строки для UI
        private static readonly string[] StyleLabels = {
            "Нет",
            "Боевой (грязь, потёртости)",
            "Ржавый (окисление, выцветание)",
            "Пустыня (песок, жара)",
            "Заснеженный (иней, лёд)",
            "Древний (мох, трещины)",
            "Кровавый (кровь, грязь)",
            "Заводской (чистый, блеск)",
            "Мистический (магия, руны)",
            "Токсичный (яд, радиация)",
            "Обожжённый (огонь, пепел)",
            "Заросший (мох, лианы)",
            "Пустошь (постапокалипсис)"
        };

        private static readonly string[] PerformanceLabels = {
            "Low (слабое железо, только статика)",
            "Medium (среднее, + погода/цвет)",
            "High (мощное, все эффекты)"
        };

        private static readonly string[] WeatherLabels = {
            "Нет",
            "Снег (хлопья, иней)",
            "Пыль (песок, буря)",
            "Пепел (дым, копоть)",
            "Дождь (потёки, мокрые пятна)",
            "Туман (бледность, дымка)"
        };

        private static readonly string[] BiomeLabels = {
            "Авто (по текущему биому)",
            "Умеренный лес",
            "Тайга / Борельная",
            "Тундра",
            "Ледяной щит",
            "Пустыня",
            "Экстремальная пустыня",
            "Тропический лес",
            "Умеренное болото",
            "Холодное болото"
        };

        public ShaderForgeMod(ModContentPack content) : base(content)
        {
            Instance = this;

            // Патч на кастомный путь конфига (атрибуты на классе LoadedModManager_GetSettingsFilename_Patch)
            // Применяем ДО первого GetSettings, иначе игра прочитает из стандартного файла
            var harmony = new Harmony("com.shaderforge.config");
            try
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("ShaderForge v2.0: патч пути конфига применён");
            }
            catch (Exception ex)
            {
                Log.Error("ShaderForge v2.0: ошибка патча конфига: " + ex.Message);
            }

            // Настройки загрузятся из Config/ShaderForge/Config.xml благодаря патчу выше
            var s = GetSettingsInternal();
        }

        public override string SettingsCategory() => "ShaderForge v2";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var settings = GetSettingsInternal();
            var listing = new Listing_Standard();

            // Скролл
            Rect viewRect = new Rect(0f, 0f, inRect.width - 30f, 920f);
            _scrollPos = GUI.BeginScrollView(inRect, _scrollPos, viewRect);

            listing.Begin(viewRect);

            // ============================================================
            // ЗАГОЛОВОК
            // ============================================================
            Text.Font = GameFont.Medium;
            listing.Label("ShaderForge v2.0 — Процедурные Текстуры");
            Text.Font = GameFont.Small;
            listing.Label("Один патч — вся игра перерисована. 12 стилей, пресеты, погода, цвет.");
            listing.Gap(8f);

            // ============================================================
            // ВКЛЮЧЕНИЕ
            // ============================================================
            listing.CheckboxLabeled("Включить ShaderForge", ref settings.Enabled,
                "Процедурная перерисовка ВСЕХ текстур. Работает на все моды автоматически.");

            if (!settings.Enabled)
            {
                listing.End();
                GUI.EndScrollView();
                return;
            }

            listing.Gap(6f);

            // ============================================================
            // ПРЕСЕТЫ
            // ============================================================
            DrawSectionHeader(listing, "ПРЕСЕТЫ");
            if (listing.ButtonTextLabeledPct("Пресет", settings.CurrentPreset, 0.55f,
                TextAnchor.MiddleLeft, null, null, null))
            {
                var opts = new List<FloatMenuOption>();
                foreach (var preset in Presets)
                {
                    var p = preset;
                    opts.Add(new FloatMenuOption($"{p.Name} — {p.Description}", () =>
                    {
                        settings.ApplyPreset(p);
                        settings.Write();
                        TextureInterceptor.ClearCache();
                        _previewNeedsUpdate = true;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            listing.Gap(6f);

            // ============================================================
            // СТИЛЬ + ИНТЕНСИВНОСТЬ
            // ============================================================
            DrawSectionHeader(listing, "СТИЛЬ");

            if (listing.ButtonTextLabeledPct("Стиль", StyleLabels[(int)settings.Style], 0.55f,
                TextAnchor.MiddleLeft, null, null, null))
            {
                var opts = new List<FloatMenuOption>();
                for (int i = 0; i < StyleLabels.Length; i++)
                {
                    int idx = i;
                    opts.Add(new FloatMenuOption(StyleLabels[i], () =>
                    {
                        settings.Style = (RenderStyle)idx;
                        settings.CurrentPreset = "Custom";
                        TextureInterceptor.ClearCache();
                        _previewNeedsUpdate = true;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            listing.Label($"Интенсивность: {Mathf.RoundToInt(settings.Intensity * 100f)}%");
            Rect sliderRect = listing.GetRect(22f);
            settings.Intensity = Widgets.HorizontalSlider(sliderRect, settings.Intensity, 0.05f, 1.0f, false,
                "Слабо", "Сильно");
            _previewNeedsUpdate = true;

            listing.Gap(6f);

            // ============================================================
            // ЦВЕТОКОРРЕКЦИЯ
            // ============================================================
            DrawSectionHeader(listing, "ЦВЕТОКОРРЕКЦИЯ");

            listing.Label($"Сдвиг оттенка: {settings.HueShift:F0}°");
            Rect hueRect = listing.GetRect(22f);
            settings.HueShift = Widgets.HorizontalSlider(hueRect, settings.HueShift, -180f, 180f, false,
                "-180°", "+180°");
            _previewNeedsUpdate = true;

            listing.Label($"Насыщенность: {settings.Saturation:F2}");
            Rect satRect = listing.GetRect(22f);
            settings.Saturation = Widgets.HorizontalSlider(satRect, settings.Saturation, 0f, 2f, false,
                "Серо", "Ярко");
            _previewNeedsUpdate = true;

            listing.Label($"Яркость: {settings.Brightness:F2}");
            Rect brightRect = listing.GetRect(22f);
            settings.Brightness = Widgets.HorizontalSlider(brightRect, settings.Brightness, 0.5f, 1.5f, false,
                "Тёмно", "Светло");
            _previewNeedsUpdate = true;

            listing.Label($"Контраст: {settings.Contrast:F2}");
            Rect contRect = listing.GetRect(22f);
            settings.Contrast = Widgets.HorizontalSlider(contRect, settings.Contrast, 0.5f, 1.5f, false,
                "Плоско", "Резко");
            _previewNeedsUpdate = true;

            listing.Label($"Цветовой фильтр (Tint): R={settings.TintR:F2} G={settings.TintG:F2} B={settings.TintB:F2}");
            Rect tintRRect = listing.GetRect(20f);
            settings.TintR = Widgets.HorizontalSlider(tintRRect, settings.TintR, 0f, 1.5f, false, "R", "");
            Rect tintGRect = listing.GetRect(20f);
            settings.TintG = Widgets.HorizontalSlider(tintGRect, settings.TintG, 0f, 1.5f, false, "G", "");
            Rect tintBRect = listing.GetRect(20f);
            settings.TintB = Widgets.HorizontalSlider(tintBRect, settings.TintB, 0f, 1.5f, false, "B", "");
            _previewNeedsUpdate = true;

            listing.Gap(6f);

            // ============================================================
            // ПОГОДНЫЙ ОВЕРЛЕЙ
            // ============================================================
            DrawSectionHeader(listing, "ПОГОДНЫЙ ОВЕРЛЕЙ (запекается в текстуру)");

            if (settings.Performance < PerformanceProfile.Medium)
            {
                listing.Label("  Требует профиль Medium или High");
                GUI.enabled = false;
            }

            if (listing.ButtonTextLabeledPct("Погода", WeatherLabels[(int)settings.Weather], 0.55f,
                TextAnchor.MiddleLeft, null, null, null))
            {
                var opts = new List<FloatMenuOption>();
                for (int i = 0; i < WeatherLabels.Length; i++)
                {
                    int idx = i;
                    opts.Add(new FloatMenuOption(WeatherLabels[i], () =>
                    {
                        settings.Weather = (WeatherOverlayType)idx;
                        settings.CurrentPreset = "Custom";
                        TextureInterceptor.ClearCache();
                        _previewNeedsUpdate = true;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            if (settings.Weather != WeatherOverlayType.None)
            {
                listing.Label($"Интенсивность погоды: {Mathf.RoundToInt(settings.WeatherIntensity * 100f)}%" +
                (settings.Performance == PerformanceProfile.High ? " (×1.5 на Heavy)" : ""));
                Rect wRect = listing.GetRect(22f);
                settings.WeatherIntensity = Widgets.HorizontalSlider(wRect, settings.WeatherIntensity, 0.05f, 1.0f,
                    false, "Лёгкая", "Сильная");
                _previewNeedsUpdate = true;
            }

            if (settings.Performance < PerformanceProfile.Medium)
                GUI.enabled = true;

            listing.Gap(6f);

            // ============================================================
            // ПРОФИЛЬ ПРОИЗВОДИТЕЛЬНОСТИ
            // ============================================================
            DrawSectionHeader(listing, "ПРОФИЛЬ ПРОИЗВОДИТЕЛЬНОСТИ");

            if (listing.ButtonTextLabeledPct("Профиль", PerformanceLabels[(int)settings.Performance], 0.55f,
                TextAnchor.MiddleLeft, null, null, null))
            {
                var opts = new List<FloatMenuOption>();
                for (int i = 0; i < PerformanceLabels.Length; i++)
                {
                    int idx = i;
                    string desc = i == 0 ? "Low — только статические стили, 0 нагрузка"
                        : i == 1 ? "Medium — + погодные оверлеи, цветокоррекция"
                        : "High — максимальная детализация";
                    opts.Add(new FloatMenuOption($"{PerformanceLabels[i]}\n  {desc}", () =>
                    {
                        settings.Performance = (PerformanceProfile)idx;
                        settings.CurrentPreset = "Custom";
                        TextureInterceptor.ClearCache();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            listing.Gap(6f);

            // ============================================================
            // ФИЛЬТРЫ КАТЕГОРИЙ
            // ============================================================
            DrawSectionHeader(listing, "КАТЕГОРИИ ТЕКСТУР");

            listing.CheckboxLabeled("  Предметы (оружие, еда, ресурсы)", ref settings.ProcessItems);
            listing.CheckboxLabeled("  Здания (стены, мебель, турели)", ref settings.ProcessBuildings);
            listing.CheckboxLabeled("  Терен (земля, трава, вода)", ref settings.ProcessTerrain);
            listing.CheckboxLabeled("  Растения", ref settings.ProcessPlants);
            listing.CheckboxLabeled("  Пешки (персонажи)", ref settings.ProcessPawns);
            listing.CheckboxLabeled("  UI / Меню", ref settings.ProcessUI);

            listing.Gap(6f);

            // ============================================================
            // ПЕР-БИОМ СТИЛИ
            // ============================================================
            DrawSectionHeader(listing, "ПЕР-БИОМ СТИЛИ");

            listing.CheckboxLabeled("  Использовать стиль для биома", ref settings.UsePerBiomeStyles,
                "Перекрывает стиль для террейна/растений в выбранном биоме.");

            if (settings.UsePerBiomeStyles)
            {
                listing.Gap(2f);

                if (listing.ButtonTextLabeledPct("  Биом", BiomeLabels[(int)settings.SelectedBiome], 0.5f,
                    TextAnchor.MiddleLeft, null, null, null))
                {
                    var opts = new List<FloatMenuOption>();
                    for (int i = 0; i < BiomeLabels.Length; i++)
                    {
                        int idx = i;
                        opts.Add(new FloatMenuOption(BiomeLabels[i], () =>
                        {
                            settings.SelectedBiome = (BiomeCategory)idx;
                            settings.CurrentPreset = "Custom";
                            TextureInterceptor.ClearCache();
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(opts));
                }

                if (settings.SelectedBiome != BiomeCategory.Auto)
                {
                    if (listing.ButtonTextLabeledPct("  Стиль биома", StyleLabels[(int)settings.BiomeOverrideStyle], 0.5f,
                        TextAnchor.MiddleLeft, null, null, null))
                    {
                        var opts = new List<FloatMenuOption>();
                        for (int i = 0; i < StyleLabels.Length; i++)
                        {
                            int idx = i;
                            opts.Add(new FloatMenuOption(StyleLabels[i], () =>
                            {
                                settings.BiomeOverrideStyle = (RenderStyle)idx;
                                settings.CurrentPreset = "Custom";
                                TextureInterceptor.ClearCache();
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(opts));
                    }

                    listing.Label($"  Интенсивность биома: {Mathf.RoundToInt(settings.BiomeOverrideIntensity * 100f)}%");
                    Rect bioRect = listing.GetRect(22f);
                    settings.BiomeOverrideIntensity = Widgets.HorizontalSlider(bioRect, settings.BiomeOverrideIntensity, 0.05f, 1.0f,
                        false, "Слабо", "Сильно");
                }
            }

            listing.Gap(6f);

            // ============================================================
            // МАСКИ ПО DEF'АМ
            // ============================================================
            DrawSectionHeader(listing, "МАСКИ ПО DEF'АМ");

            listing.CheckboxLabeled("  Включить маскирование", ref settings.UseDefMasks,
                "Фильтрация текстур по именам def'ов (через запятую).");

            if (settings.UseDefMasks)
            {
                listing.CheckboxLabeled("    Режим: Чёрный список (исключить)", ref settings.DefMaskIsBlacklist);
                listing.Label("    Список def'ов (через запятую или ;):");
                Rect maskRect = listing.GetRect(28f);
                settings.DefMaskText = Widgets.TextField(maskRect, settings.DefMaskText);
            }

            listing.Gap(6f);

            // ============================================================
            // ПРЕВЬЮ
            // ============================================================
            DrawSectionHeader(listing, "ПРЕВЬЮ");

            listing.CheckboxLabeled("  Показывать превью текстуры", ref settings.PreviewEnabled);

            if (settings.PreviewEnabled)
            {
                DrawPreview(listing, settings);
            }

            listing.Gap(6f);

            // ============================================================
            // СТАТИСТИКА И УПРАВЛЕНИЕ
            // ============================================================
            DrawSectionHeader(listing, "УПРАВЛЕНИЕ");

            listing.Label(TextureInterceptor.GetStats());

            listing.Gap(4f);
            Rect resetRect = listing.GetRect(30f);
            if (Widgets.ButtonText(resetRect, "Сбросить кэш текстур"))
            {
                TextureInterceptor.ClearCache();
            }

            Rect applyRect = listing.GetRect(30f);
            if (Widgets.ButtonText(applyRect, "Применить немедленно"))
            {
                int count = TextureInterceptor.ReprocessAll();
                settings.Write();
            }
            listing.Label("  ↑ перерисует все загруженные текстуры с текущими настройками");

            Rect defaultsRect = listing.GetRect(30f);
            if (Widgets.ButtonText(defaultsRect, "Сбросить ВСЕ настройки"))
            {
                settings.ResetToDefaults();
                TextureInterceptor.ClearCache();
                _previewNeedsUpdate = true;
            }

            listing.Gap(8f);

            // Версия
            Text.Font = GameFont.Tiny;
            listing.Label($"ShaderForge v{ShaderForgeInfo.Version} | Автор: Elengal | 12 стилей, 6 погод, пресеты");
            Text.Font = GameFont.Small;

            listing.End();
            GUI.EndScrollView();
        }

        // ============================================================
        // UI HELPERS
        // ============================================================

        private void DrawSectionHeader(Listing_Standard listing, string title)
        {
            listing.Gap(4f);
            Rect headerRect = listing.GetRect(22f);
            Widgets.DrawHighlightSelected(headerRect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(headerRect, $"  {title}");
            Text.Anchor = TextAnchor.UpperLeft;
            listing.Gap(2f);
        }

        private void DrawPreview(Listing_Standard listing, ShaderForgeSettings settings)
        {
            // Генерация превью
            int previewSize = 96;
            float time = Time.realtimeSinceStartup;

            // Обновляем превью раз в 0.5 секунды
            if (_previewTexture == null || _previewNeedsUpdate || time - _lastPreviewUpdate > 0.5f)
            {
                GeneratePreview(settings, previewSize);
                _lastPreviewUpdate = time;
                _previewNeedsUpdate = false;
            }

            if (_previewTexture != null)
            {
                Rect previewRect = listing.GetRect(previewSize + 8f);
                float xOffset = (previewRect.width - previewSize * 2 - 8f) / 2f;

                // Оригинал
                Rect origRect = new Rect(previewRect.x + xOffset, previewRect.y + 4f, previewSize, previewSize);
                Widgets.DrawBoxSolid(origRect, Color.black);
                GUI.DrawTexture(origRect, _previewTexture);

                // Обработанная
                Texture2D processed = GenerateProcessedPreview(settings, previewSize);
                if (processed != null)
                {
                    Rect procRect = new Rect(origRect.xMax + 8f, origRect.y, previewSize, previewSize);
                    Widgets.DrawBoxSolid(procRect, Color.black);
                    GUI.DrawTexture(procRect, processed);
                }

                // Подписи
                Rect labelRect = new Rect(previewRect.x, origRect.yMax + 2f, previewRect.width, 20f);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(origRect.center.x - 30f, labelRect.y, 60f, 20f), "Оригинал");
                Widgets.Label(new Rect(origRect.center.x + previewSize + 8f + 14f, labelRect.y, 60f, 20f),
                    $"Стиль: {StyleLabels[(int)settings.Style]}");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            listing.Gap(4f);
        }

        /// <summary>
        /// Генерирует тестовую текстуру для превью.
        /// </summary>
        private void GeneratePreview(ShaderForgeSettings settings, int size)
        {
            Color[] pixels = new Color[size * size];

            // Генерируем тестовую текстуру: градиент + шумы + геометрия
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Каменный блок с грязью
                    Color stone = new Color(0.45f, 0.42f, 0.38f, 1f);
                    Color dirt = new Color(0.35f, 0.28f, 0.18f, 1f);
                    Color metal = new Color(0.55f, 0.55f, 0.52f, 1f);

                    // Простой паттерн: камень + металлическая вставка + грязь
                    Color baseColor;
                    if (u > 0.3f && u < 0.7f && v > 0.4f && v < 0.6f)
                        baseColor = metal; // Металлическая полоса
                    else if (v > 0.7f)
                        baseColor = dirt; // Грязь внизу
                    else
                        baseColor = stone; // Камень

                    // Шум для разнообразия
                    float noise = (Mathf.Sin(x * 0.5f) * Mathf.Cos(y * 0.7f) + 1f) * 0.5f;
                    baseColor.r *= 0.9f + noise * 0.2f;
                    baseColor.g *= 0.9f + noise * 0.2f;
                    baseColor.b *= 0.9f + noise * 0.2f;

                    pixels[y * size + x] = baseColor;
                }
            }

            if (_previewTexture == null || _previewTexture.width != size)
            {
                _previewTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            }

            _previewTexture.SetPixels(pixels);
            _previewTexture.Apply(false);
        }

        /// <summary>
        /// Генерирует обработанную версию превью.
        /// </summary>
        private Texture2D GenerateProcessedPreview(ShaderForgeSettings settings, int size)
        {
            if (_previewTexture == null) return null;

            try
            {
                Color[] pixels = _previewTexture.GetPixels();
                MaterialType material = MaterialAnalyzer.AnalyzeImage(pixels);

                // Применяем стиль
                if (settings.Style != RenderStyle.None)
                {
                    pixels = ProceduralProcessor.Process(pixels, size, size, material, settings.Style, settings.Intensity);
                }

                // Применяем цветокоррекцию
                pixels = ProceduralProcessor.ApplyColorCorrection(pixels, settings.GetColorCorrection());

                // Применяем погоду
                if (settings.ShouldApplyWeather())
                {
                    pixels = ProceduralProcessor.ApplyWeatherOverlay(pixels, size, size,
                        settings.Weather, settings.WeatherIntensity);
                }

                Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
                result.SetPixels(pixels);
                result.Apply(false);
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
