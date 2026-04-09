using System;
using UnityEngine;

namespace ShaderForge
{
    /// <summary>
    /// Режимы перерисовки — разные стили для текстур.
    /// Каждый режим — свой набор шумов и эффектов.
    /// v2.0 — добавлены: Mystic, Toxic, Scorched, Overgrown, Wasteland
    /// </summary>
    public enum RenderStyle
    {
        None = 0,        // Без изменений
        BattleWorn = 1,  // Боевой: потёртости, грязь, вмятины
        Rusty = 2,       // Ржавый: ржавчина на металле, выцветание
        Desert = 3,      // Пустыня: песок, выцветшие цвета, жар
        Frozen = 4,      // Заснеженный: иней, обморожение, лёд
        Ancient = 5,     // Древний: мох, трещины, выветривание
        Blood = 6,       // Кровавый: пятна крови, грязь
        Clean = 7,       // Заводской: чистый, отполированный
        Mystic = 8,      // Мистический: магическое свечение, руны
        Toxic = 9,       // Токсичный: ядовитые пятна, радиация
        Scorched = 10,   // Обожжённый: огонь, пепел, плавление
        Overgrown = 11,  // Заросший: мох, лозы, лианы
        Wasteland = 12   // Пустошь: постапокалипсис, распад
    }

    /// <summary>
    /// Профиль производительности.
    /// Low = статические стили только (любое железо).
    /// Medium = + простые оверлеи (цвет, погода baked).
    /// High = + полная детализация всех эффектов.
    /// </summary>
    public enum PerformanceProfile
    {
        Low = 0,      // Только статические стили, нулевая нагрузка
        Medium = 1,   // + погодные оверлеи, цветокоррекция baked
        High = 2      // + максимальная детализация, все эффекты
    }

    /// <summary>
    /// Тип статического погодного оверлея (запекается в текстуру).
    /// </summary>
    public enum WeatherOverlayType
    {
        None = 0,     // Без погоды
        Snow = 1,     // Снежинки / иней на текстурах
        Dust = 2,     // Пыльная буря
        Ash = 3,      // Пепел / дым
        Rain = 4,     // Дождевые потёки
        Fog = 5       // Туман / бледность
    }

    /// <summary>
    /// Категория биома для пер-биом стилей.
    /// </summary>
    public enum BiomeCategory
    {
        Auto = 0,             // Определять по текущему биому
        TemperateForest = 1,  // Умеренный лес
        BorealForest = 2,     // Тайга
        Tundra = 3,           // Тундра
        IceSheet = 4,         // Ледяной щит
        Desert = 5,           // Пустыня
        ExtremeDesert = 6,    // Экстремальная пустыня
        TropicalRainforest = 7,// Тропический лес
        TemperateSwamp = 8,   // Болото
        ColdBog = 9           // Холодное болото
    }

    /// <summary>
    /// Конфигурация цветокоррекции (статическая, применяется при загрузке).
    /// </summary>
    public class ColorCorrectionSettings
    {
        public float HueShift = 0f;           // -180..180 градусов
        public float Saturation = 1f;         // 0..2 (1 = норма)
        public float Brightness = 1f;         // 0.5..1.5
        public float Contrast = 1f;           // 0.5..1.5
        public Color TintColor = Color.white;  // Цветовой фильтр

        /// <summary>
        /// Применяет цветокоррекцию к одному пикселю.
        /// </summary>
        public Color Apply(Color c)
        {
            Color result = c;

            // 1. Tint (цветовой фильтр)
            result.r *= TintColor.r;
            result.g *= TintColor.g;
            result.b *= TintColor.b;

            // 2. RGB → HSL
            float h, s, l;
            ColorToHSL(result, out h, out s, out l);

            // 3. Hue shift
            h = (h + HueShift / 360f) % 1f;
            if (h < 0f) h += 1f;

            // 4. Saturation
            s = Mathf.Clamp01(s * Saturation);

            // 5. Brightness
            l = Mathf.Clamp01(l * Brightness);

            // 6. HSL → RGB
            result = HSLToColor(h, s, l, result.a);

            // 7. Contrast
            if (Contrast != 1f)
            {
                float lum = 0.299f * result.r + 0.587f * result.g + 0.114f * result.b;
                result.r = Mathf.Clamp01(lum + (result.r - lum) * Contrast);
                result.g = Mathf.Clamp01(lum + (result.g - lum) * Contrast);
                result.b = Mathf.Clamp01(lum + (result.b - lum) * Contrast);
            }

            return result;
        }

        // === УТИЛИТЫ RGB ↔ HSL ===

        private static void ColorToHSL(Color c, out float h, out float s, out float l)
        {
            float max = Mathf.Max(c.r, c.g, c.b);
            float min = Mathf.Min(c.r, c.g, c.b);
            float delta = max - min;

            l = (max + min) / 2f;

            if (Mathf.Approximately(delta, 0f))
            {
                h = 0f;
                s = 0f;
            }
            else
            {
                s = delta / (1f - Mathf.Abs(2f * l - 1f) + 0.0001f);

                if (Mathf.Approximately(max, c.r))
                    h = ((c.g - c.b) / delta) % 6f;
                else if (Mathf.Approximately(max, c.g))
                    h = (c.b - c.r) / delta + 2f;
                else
                    h = (c.r - c.g) / delta + 4f;

                h /= 6f;
                if (h < 0f) h += 1f;
            }
        }

        private static Color HSLToColor(float h, float s, float l, float a)
        {
            if (Mathf.Approximately(s, 0f))
                return new Color(l, l, l, a);

            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;

            float r = HueToRGB(p, q, h + 1f / 3f);
            float g = HueToRGB(p, q, h);
            float b = HueToRGB(p, q, h - 1f / 3f);

            return new Color(r, g, b, a);
        }

        private static float HueToRGB(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }
    }

    /// <summary>
    /// Процедурный процессор — перерисовывает пиксели.
    /// Для каждого типа материала — свои эффекты.
    /// v2.0 — 12 стилей, цветокоррекция, погодные оверлеи.
    /// </summary>
    public static class ProceduralProcessor
    {
        // ============================================================
        // ОСНОВНОЙ API
        // ============================================================

        /// <summary>
        /// Обрабатывает массив пикселей на основе материала и стиля.
        /// </summary>
        public static Color[] Process(Color[] pixels, int width, int height,
            MaterialType material, RenderStyle style, float intensity = 0.5f)
        {
            if (style == RenderStyle.None || intensity <= 0f)
                return pixels;

            ProceduralNoise.SetSeed(width * 1000 + height);

            Color[] result = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                result[i] = pixels[i];
                result[i] = ApplyPixelEffect(result[i], i, width, height, material, style, intensity);
            }

            return result;
        }

        /// <summary>
        /// Применяет цветокоррекцию к массиву пикселей (после стиля).
        /// </summary>
        public static Color[] ApplyColorCorrection(Color[] pixels, ColorCorrectionSettings cc)
        {
            if (cc == null) return pixels;

            bool hasChanges = Mathf.Abs(cc.HueShift) > 0.1f || Mathf.Abs(cc.Saturation - 1f) > 0.01f
                || Mathf.Abs(cc.Brightness - 1f) > 0.01f || Mathf.Abs(cc.Contrast - 1f) > 0.01f
                || cc.TintColor != Color.white;

            if (!hasChanges) return pixels;

            Color[] result = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                result[i] = pixels[i].a < 0.1f ? pixels[i] : cc.Apply(pixels[i]);
            }

            return result;
        }

        /// <summary>
        /// Применяет погодный оверлей к массиву пикселей (статический, baked).
        /// </summary>
        public static Color[] ApplyWeatherOverlay(Color[] pixels, int width, int height,
            WeatherOverlayType weather, float weatherIntensity)
        {
            if (weather == WeatherOverlayType.None || weatherIntensity <= 0f)
                return pixels;

            ProceduralNoise.SetSeed(width * 777 + height * 333 + (int)weather * 111);

            Color[] result = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                result[i] = pixels[i];
                if (pixels[i].a < 0.1f) continue;

                int x = i % width;
                float u = (float)x / width;
                float v = (float)(i / width) / height;

                switch (weather)
                {
                    case WeatherOverlayType.Snow:
                        result[i] = ApplySnowOverlay(result[i], u, v, weatherIntensity);
                        break;
                    case WeatherOverlayType.Dust:
                        result[i] = ApplyDustOverlay(result[i], u, v, weatherIntensity);
                        break;
                    case WeatherOverlayType.Ash:
                        result[i] = ApplyAshOverlay(result[i], u, v, weatherIntensity);
                        break;
                    case WeatherOverlayType.Rain:
                        result[i] = ApplyRainOverlay(result[i], u, v, weatherIntensity);
                        break;
                    case WeatherOverlayType.Fog:
                        result[i] = ApplyFogOverlay(result[i], u, v, weatherIntensity);
                        break;
                }
            }

            return result;
        }

        // ============================================================
        // ВНУТРЕННИЕ МЕТОДЫ
        // ============================================================

        private static Color ApplyPixelEffect(Color c, int index, int width, int height,
            MaterialType material, RenderStyle style, float intensity)
        {
            if (c.a < 0.1f)
                return c;

            int x = index % width;
            float u = (float)x / width;
            float v = (float)(index / width) / height;

            switch (style)
            {
                case RenderStyle.BattleWorn:
                    return ApplyBattleWorn(c, u, v, material, intensity);
                case RenderStyle.Rusty:
                    return ApplyRusty(c, u, v, material, intensity);
                case RenderStyle.Desert:
                    return ApplyDesert(c, u, v, material, intensity);
                case RenderStyle.Frozen:
                    return ApplyFrozen(c, u, v, material, intensity);
                case RenderStyle.Ancient:
                    return ApplyAncient(c, u, v, material, intensity);
                case RenderStyle.Blood:
                    return ApplyBlood(c, u, v, material, intensity);
                case RenderStyle.Clean:
                    return ApplyClean(c, u, v, material, intensity);
                case RenderStyle.Mystic:
                    return ApplyMystic(c, u, v, material, intensity);
                case RenderStyle.Toxic:
                    return ApplyToxic(c, u, v, material, intensity);
                case RenderStyle.Scorched:
                    return ApplyScorched(c, u, v, material, intensity);
                case RenderStyle.Overgrown:
                    return ApplyOvergrown(c, u, v, material, intensity);
                case RenderStyle.Wasteland:
                    return ApplyWasteland(c, u, v, material, intensity);
                default:
                    return c;
            }
        }

        // ============================================================
        // ОРИГИНАЛЬНЫЕ СТИЛИ (v1.0)
        // ============================================================

        private static Color ApplyBattleWorn(Color c, float u, float v, MaterialType mat, float intensity)
        {
            float dirt = ProceduralNoise.FBM(u * 8f, v * 8f, 4);
            dirt = Mathf.Clamp01((dirt + 1f) / 2f);

            float wear = ProceduralNoise.FBM(u * 20f, v * 20f, 3, 2f, 0.7f);
            wear = Mathf.Clamp01((wear + 1f) / 2f);

            float scratch = ProceduralNoise.FBM(u * 50f + 100f, v * 50f + 200f, 2);
            scratch = Mathf.Clamp01((scratch + 1f) / 2f);

            Color result = c;

            float dirtAmount = dirt * 0.3f * intensity;
            result = Blend(result, new Color(0.25f, 0.2f, 0.12f, 1f), dirtAmount);

            float wearAmount = wear * 0.15f * intensity;
            result.r *= (1f - wearAmount);
            result.g *= (1f - wearAmount);
            result.b *= (1f - wearAmount);

            if (mat == MaterialType.Metal)
            {
                float scratchAmount = scratch * 0.2f * intensity;
                result = Blend(result, new Color(0.15f, 0.15f, 0.15f, 1f), scratchAmount);
            }

            return result;
        }

        private static Color ApplyRusty(Color c, float u, float v, MaterialType mat, float intensity)
        {
            float rust = ProceduralNoise.FBM(u * 6f + 50f, v * 6f + 50f, 5);
            rust = Mathf.Clamp01((rust + 1f) / 2f);

            float spots = ProceduralNoise.Voronoi(u * 4f, v * 4f, 8f);
            spots = Mathf.Clamp01(1f - spots);

            Color result = c;

            if (mat == MaterialType.Metal)
            {
                float rustAmount = (rust * 0.4f + spots * 0.3f) * intensity;
                result = Blend(result, new Color(0.6f, 0.25f, 0.05f, 1f), rustAmount);
            }
            else
            {
                float fade = rust * 0.2f * intensity;
                result = Blend(result, new Color(0.7f, 0.65f, 0.55f, 1f), fade);
            }

            return result;
        }

        private static Color ApplyDesert(Color c, float u, float v, MaterialType mat, float intensity)
        {
            float sand = ProceduralNoise.FBM(u * 12f + 200f, v * 12f + 200f, 4);
            sand = Mathf.Clamp01((sand + 1f) / 2f);

            float grain = ProceduralNoise.FBM(u * 30f, v * 30f, 2, 2.5f, 0.3f);
            grain = Mathf.Clamp01((grain + 1f) / 2f);

            Color result = c;

            float sandAmount = (sand * 0.4f + grain * 0.1f) * intensity;
            result = Blend(result, new Color(0.82f, 0.72f, 0.52f, 1f), sandAmount);

            float fade = sand * 0.15f * intensity;
            result.r = result.r + (1f - result.r) * fade;
            result.g = result.g + (1f - result.g) * fade;
            result.b = result.b + (1f - result.b) * fade;

            return result;
        }

        private static Color ApplyFrozen(Color c, float u, float v, MaterialType mat, float intensity)
        {
            float frost = ProceduralNoise.FBM(u * 10f + 300f, v * 10f + 300f, 5);
            frost = Mathf.Clamp01((frost + 1f) / 2f);

            float ice = ProceduralNoise.Voronoi(u * 6f + 300f, v * 6f + 300f, 10f);
            ice = Mathf.Clamp01(1f - ice * 1.5f);

            Color result = c;

            float frostAmount = (frost * 0.3f + ice * 0.2f) * intensity;
            result = Blend(result, new Color(0.85f, 0.9f, 0.95f, 1f), frostAmount);

            float cold = frost * 0.1f * intensity;
            result.b = Mathf.Min(1f, result.b + cold);
            result.r *= (1f - cold * 0.5f);

            return result;
        }

        private static Color ApplyAncient(Color c, float u, float v, MaterialType mat, float intensity)
        {
            float moss = ProceduralNoise.FBM(u * 7f + 400f, v * 7f + 400f, 4);
            moss = Mathf.Clamp01((moss + 1f) / 2f);

            float crack = ProceduralNoise.Voronoi(u * 5f + 400f, v * 5f + 400f, 6f);
            crack = Mathf.Clamp01(crack < 0.05f ? 1f : 0f);

            float weather = ProceduralNoise.FBM(u * 3f, v * 3f, 3);
            weather = Mathf.Clamp01((weather + 1f) / 2f);

            Color result = c;

            if (mat == MaterialType.Stone || mat == MaterialType.Metal)
            {
                float mossAmount = moss * 0.35f * intensity;
                result = Blend(result, new Color(0.2f, 0.35f, 0.15f, 1f), mossAmount);
            }

            if (crack > 0f)
            {
                result = Blend(result, new Color(0.1f, 0.08f, 0.06f, 1f), crack * 0.5f * intensity);
            }

            float fade = weather * 0.2f * intensity;
            result = Blend(result, new Color(0.6f, 0.58f, 0.5f, 1f), fade);

            return result;
        }

        private static Color ApplyBlood(Color c, float u, float v, MaterialType mat, float intensity)
        {
            float blood = ProceduralNoise.FBM(u * 8f + 500f, v * 8f + 500f, 4);
            blood = Mathf.Clamp01((blood - 0.1f) * 1.5f);

            float splatter = ProceduralNoise.Voronoi(u * 5f + 500f, v * 5f + 500f, 12f);
            splatter = Mathf.Clamp01(splatter < 0.15f ? 1f : 0f);

            Color result = c;

            float bloodAmount = (blood * 0.3f + splatter * 0.4f) * intensity;
            result = Blend(result, new Color(0.35f, 0.02f, 0.02f, 1f), bloodAmount);

            float dirt = ProceduralNoise.FBM(u * 6f, v * 6f, 3);
            dirt = Mathf.Clamp01((dirt + 1f) / 2f);
            float dirtAmount = dirt * 0.15f * intensity;
            result = Blend(result, new Color(0.2f, 0.15f, 0.1f, 1f), dirtAmount);

            return result;
        }

        private static Color ApplyClean(Color c, float u, float v, MaterialType mat, float intensity)
        {
            float shine = ProceduralNoise.FBM(u * 15f + 600f, v * 15f + 600f, 2, 2f, 0.3f);
            shine = Mathf.Clamp01((shine + 1f) / 2f);

            Color result = c;

            float shineAmount = shine * 0.08f * intensity;
            result.r = Mathf.Min(1f, result.r + shineAmount);
            result.g = Mathf.Min(1f, result.g + shineAmount);
            result.b = Mathf.Min(1f, result.b + shineAmount);

            float lum = 0.299f * result.r + 0.587f * result.g + 0.114f * result.b;
            result.r += (result.r - lum) * 0.1f * intensity;
            result.g += (result.g - lum) * 0.1f * intensity;
            result.b += (result.b - lum) * 0.1f * intensity;

            return result;
        }

        // ============================================================
        // НОВЫЕ СТИЛИ (v2.0)
        // ============================================================

        /// <summary>
        /// МИСТИЧЕСКИЙ: магическое свечение, руны на камне, эфирные линии.
        /// Сине-фиолетовая палитра, светящиеся символы.
        /// </summary>
        private static Color ApplyMystic(Color c, float u, float v, MaterialType mat, float intensity)
        {
            Color result = c;

            // 1. Эфирное свечение (domain warp для органичных потоков)
            float warpU, warpV;
            ProceduralNoise.DomainWarp(u, v, 3f, 0.15f * intensity, out warpU, out warpV, 3);
            float glow = ProceduralNoise.FBM(warpU * 6f + 700f, warpV * 6f + 700f, 4);
            glow = Mathf.Clamp01((glow + 0.3f) / 1.3f);

            // 2. Энергетические линии (ridged noise — резкие脉络)
            float energy = ProceduralNoise.RidgedNoise(u * 10f + 750f, v * 10f + 750f, 3);
            energy = Mathf.Clamp01(energy * 0.8f);

            // 3. Руны на камне/металле (Voronoi edges)
            float runes = 0f;
            if (mat == MaterialType.Stone || mat == MaterialType.Metal)
            {
                runes = ProceduralNoise.VoronoiEdges(u * 5f + 800f, v * 5f + 800f, 6f, 0.04f);
            }

            // Составляем цвет свечения (синий + фиолетовый)
            float magicR = 0.4f + glow * 0.2f;
            float magicG = 0.2f + glow * 0.1f;
            float magicB = 0.8f + glow * 0.2f;

            // Эфирное свечение — мягкий оверлей
            Color glowColor = new Color(magicR, magicG, magicB, 1f);
            result = Blend(result, glowColor, glow * 0.2f * intensity);

            // Энергетические линии — яркие сине-фиолетовые
            Color lineColor = new Color(0.5f, 0.3f, 1.0f, 1f);
            result = Blend(result, lineColor, energy * 0.35f * intensity);

            // Руны — яркие символы
            if (runes > 0f)
            {
                Color runeColor = new Color(0.6f, 0.5f, 1.0f, 1f);
                result = Blend(result, runeColor, runes * 0.5f * intensity);
                // Свечение рун — яркость
                result.r = Mathf.Min(1f, result.r + runes * 0.15f * intensity);
                result.b = Mathf.Min(1f, result.b + runes * 0.25f * intensity);
            }

            return result;
        }

        /// <summary>
        /// ТОКСИЧНЫЙ: ядовитые пятна, радиоактивное свечение, коррозия.
        /// Зелёно-жёлтая палитра, кислотные разводы.
        /// </summary>
        private static Color ApplyToxic(Color c, float u, float v, MaterialType mat, float intensity)
        {
            Color result = c;

            // 1. Кислотные лужи (domain warp для органичных форм)
            float warpU = u, warpV = v;
            ProceduralNoise.DomainWarp(u, v, 4f, 0.2f * intensity, out warpU, out warpV, 3);
            float acid = ProceduralNoise.FBM(warpU * 5f + 900f, warpV * 5f + 900f, 5);
            acid = Mathf.Clamp01((acid - 0.2f) * 2.0f); // Концентрированные пятна

            // 2. Радиоактивные точки (Voronoi пятна)
            float radiation = ProceduralNoise.Voronoi(u * 7f + 950f, v * 7f + 950f, 10f);
            radiation = Mathf.Clamp01(1f - radiation * 2f);

            // 3. Радиоактивные линии (ridged noise)
            float radLines = ProceduralNoise.RidgedNoise(u * 12f + 1000f, v * 12f + 1000f, 2);
            radLines = Mathf.Clamp01(radLines * 0.5f);

            // Кислотный налёт — жёлто-зелёный
            float acidAmount = acid * 0.35f * intensity;
            result = Blend(result, new Color(0.4f, 0.6f, 0.05f, 1f), acidAmount);

            // Радиоактивное свечение — ярко-зелёное
            float radAmount = (radiation * 0.25f + radLines * 0.15f) * intensity;
            result = Blend(result, new Color(0.2f, 0.9f, 0.1f, 1f), radAmount);

            // Яркость радиации — пульсирующее свечение
            float pulse = Mathf.Clamp01(radiation * 0.3f * intensity);
            result.g = Mathf.Min(1f, result.g + pulse * 0.3f);
            result.b = Mathf.Min(1f, result.b + pulse * 0.15f);

            // Коррозия на металле
            if (mat == MaterialType.Metal)
            {
                float corrosion = ProceduralNoise.FBM(u * 8f + 1050f, v * 8f + 1050f, 3);
                corrosion = Mathf.Clamp01((corrosion + 1f) / 2f);
                result = Blend(result, new Color(0.15f, 0.25f, 0.05f, 1f), corrosion * 0.2f * intensity);
            }

            return result;
        }

        /// <summary>
        /// ОБОЖЖЁННЫЙ: огненные повреждения, пепел, плавление, закопчённость.
        /// Тёмная палитра с оранжевыми угольками.
        /// </summary>
        private static Color ApplyScorched(Color c, float u, float v, MaterialType mat, float intensity)
        {
            Color result = c;

            // 1. Обжиг — тёмные обожжённые области
            float burn = ProceduralNoise.FBM(u * 6f + 1100f, v * 6f + 1100f, 5);
            burn = Mathf.Clamp01((burn + 0.5f) / 1.5f); // Смещение к тёмным зонам

            // 2. Плавление (domain warp — деформированные области)
            float warpU = u, warpV = v;
            ProceduralNoise.DomainWarp(u, v, 5f, 0.1f * intensity, out warpU, out warpV, 2);
            float melt = ProceduralNoise.FBM(warpU * 8f + 1150f, warpV * 8f + 1150f, 3);
            melt = Mathf.Clamp01((melt + 1f) / 2f);

            // 3. Угольки — горячие точки (Voronoi)
            float ember = ProceduralNoise.Voronoi(u * 12f + 1200f, v * 12f + 1200f, 15f);
            ember = Mathf.Clamp01(ember < 0.12f ? 1f : 0f); // Маленькие точки

            // 4. Дымовая копоть (высокочастотный шум)
            float soot = ProceduralNoise.FBM(u * 25f + 1250f, v * 25f + 1250f, 3, 2f, 0.6f);
            soot = Mathf.Clamp01((soot + 1f) / 2f);

            // Обжиг — затемнение
            float burnAmount = burn * 0.45f * intensity;
            result = Blend(result, new Color(0.08f, 0.05f, 0.03f, 1f), burnAmount);

            // Плавление — тёмно-коричневые потёки
            if (mat == MaterialType.Metal || mat == MaterialType.Stone)
            {
                float meltAmount = melt * 0.2f * intensity;
                result = Blend(result, new Color(0.12f, 0.08f, 0.04f, 1f), meltAmount);
            }
            // Сильнее на дереве и растениях
            else if (mat == MaterialType.Wood || mat == MaterialType.Plant)
            {
                float meltAmount = melt * 0.35f * intensity;
                result = Blend(result, new Color(0.05f, 0.03f, 0.01f, 1f), meltAmount);
            }

            // Угольки — горячие оранжевые точки
            if (ember > 0f)
            {
                Color emberColor = new Color(1.0f, 0.4f, 0.05f, 1f);
                result = Blend(result, emberColor, ember * 0.6f * intensity);
                result.r = Mathf.Min(1f, result.r + ember * 0.2f * intensity);
            }

            // Дым — лёгкая копоть
            float sootAmount = soot * 0.15f * intensity;
            result = Blend(result, new Color(0.15f, 0.13f, 0.12f, 1f), sootAmount);

            return result;
        }

        /// <summary>
        /// ЗАРОСШИЙ: густой мох, лианы, лichen, корни.
        /// Зелёная палитра, органические паттерны.
        /// </summary>
        private static Color ApplyOvergrown(Color c, float u, float v, MaterialType mat, float intensity)
        {
            Color result = c;

            // 1. Мох — плотный зелёный налёт (FBM)
            float moss = ProceduralNoise.FBM(u * 7f + 1300f, v * 7f + 1300f, 5);
            moss = Mathf.Clamp01((moss + 0.3f) / 1.3f); // Много мха

            // 2. Лианы — полоски (ridged noise = резкие линии)
            float vines = ProceduralNoise.RidgedNoise(u * 4f + 1350f, v * 8f + 1350f, 3);
            vines = Mathf.Clamp01(vines * 0.6f);

            // 3. Биологические ячейки — лишайник
            float f1, f2;
            ProceduralNoise.Cellular(u * 10f + 1400f, v * 10f + 1400f, 6f, out f1, out f2);
            float lichen = Mathf.Clamp01(1f - (f2 - f1) * 8f); // Только границы ячеек

            // 4. Листья — зелёные пятна (Voronoi)
            float leaves = ProceduralNoise.Voronoi(u * 8f + 1450f, v * 8f + 1450f, 12f);
            leaves = Mathf.Clamp01(leaves < 0.15f ? 1f : 0f);

            // Мох — основной оверлей
            float mossAmount = moss * 0.4f * intensity;
            result = Blend(result, new Color(0.15f, 0.3f, 0.08f, 1f), mossAmount);

            // Лианы — тёмно-зелёные полосы
            float vineAmount = vines * 0.3f * intensity;
            result = Blend(result, new Color(0.1f, 0.22f, 0.05f, 1f), vineAmount);

            // Лишайник — светло-зелёные/жёлтые точки
            if (lichen > 0f)
            {
                float lichenAmount = lichen * 0.35f * intensity;
                Color lichenColor = (mat == MaterialType.Stone || mat == MaterialType.Metal)
                    ? new Color(0.25f, 0.35f, 0.1f, 1f)  // На камне — жёлто-зелёный
                    : new Color(0.2f, 0.35f, 0.12f, 1f);  // На остальном — зелёный
                result = Blend(result, lichenColor, lichenAmount);
            }

            // Листья — яркие зелёные пятна
            if (leaves > 0f)
            {
                float leafAmount = leaves * 0.3f * intensity;
                result = Blend(result, new Color(0.1f, 0.4f, 0.05f, 1f), leafAmount);
            }

            // На дереве и камнях — сильнее
            if (mat == MaterialType.Stone || mat == MaterialType.Wood)
            {
                float extra = moss * 0.1f * intensity;
                result = Blend(result, new Color(0.12f, 0.25f, 0.05f, 1f), extra);
            }

            return result;
        }

        /// <summary>
        /// ПУСТОШЬ: постапокалипсис — распад, радиация, песок, разрушение.
        /// Бледная серо-коричневая палитра с радиоактивными включениями.
        /// </summary>
        private static Color ApplyWasteland(Color c, float u, float v, MaterialType mat, float intensity)
        {
            Color result = c;

            // 1. Выветривание / обесцвечивание
            float weather = ProceduralNoise.FBM(u * 4f + 1500f, v * 4f + 1500f, 4);
            weather = Mathf.Clamp01((weather + 1f) / 2f);

            // 2. Трещины разрушения (Voronoi edges — широкие)
            float cracks = ProceduralNoise.VoronoiEdges(u * 3f + 1550f, v * 3f + 1550f, 4f, 0.08f);

            // 3. Радиоактивные пятна (Voronoi)
            float radSpots = ProceduralNoise.Voronoi(u * 6f + 1600f, v * 6f + 1600f, 8f);
            radSpots = Mathf.Clamp01(radSpots < 0.2f ? 1f : 0f);

            // 4. Пыль / песок (высокочастотный FBM)
            float dust = ProceduralNoise.FBM(u * 15f + 1650f, v * 15f + 1650f, 3, 2.5f, 0.4f);
            dust = Mathf.Clamp01((dust + 1f) / 2f);

            // 5. Пепельный налёт
            float ash = ProceduralNoise.FBM(u * 5f + 1700f, v * 5f + 1700f, 2);
            ash = Mathf.Clamp01((ash + 0.5f) / 1.5f);

            // Выцветание — палитра пустоши (бледные серо-коричневые тона)
            float weatherAmount = weather * 0.3f * intensity;
            result = Blend(result, new Color(0.55f, 0.5f, 0.42f, 1f), weatherAmount);

            // Обесцвечивание (снижение насыщенности)
            float desat = weather * 0.25f * intensity;
            float lum = 0.299f * result.r + 0.587f * result.g + 0.114f * result.b;
            result.r = result.r + (lum - result.r) * desat;
            result.g = result.g + (lum - result.g) * desat;
            result.b = result.b + (lum - result.b) * desat;

            // Трещины — тёмные провалы
            if (cracks > 0f)
            {
                float crackAmount = cracks * 0.4f * intensity;
                result = Blend(result, new Color(0.08f, 0.06f, 0.05f, 1f), crackAmount);
            }

            // Радиация — едва заметные зелёные пятна
            if (radSpots > 0f)
            {
                float radAmount = radSpots * 0.2f * intensity;
                result = Blend(result, new Color(0.25f, 0.35f, 0.15f, 1f), radAmount);
                result.g = Mathf.Min(1f, result.g + radSpots * 0.1f * intensity);
            }

            // Пыль — песочный налёт
            float dustAmount = dust * 0.2f * intensity;
            result = Blend(result, new Color(0.6f, 0.55f, 0.45f, 1f), dustAmount);

            // Пепел — лёгкий серый налёт
            float ashAmount = ash * 0.15f * intensity;
            result = Blend(result, new Color(0.5f, 0.48f, 0.46f, 1f), ashAmount);

            return result;
        }

        // ============================================================
        // ПОГОДНЫЕ ОВЕРЛЕИ (статические, запекаются в текстуру)
        // ============================================================

        /// <summary>
        /// Снег — белые хлопья и иней на поверхностях.
        /// </summary>
        private static Color ApplySnowOverlay(Color c, float u, float v, float intensity)
        {
            // Снежинки (точечный шум)
            float snowflakes = ProceduralNoise.Voronoi(u * 15f + 2000f, v * 15f + 2000f, 20f);
            snowflakes = Mathf.Clamp01(snowflakes < 0.08f ? 1f : 0f);

            // Иней (мягкий FBM)
            float frost = ProceduralNoise.FBM(u * 12f + 2050f, v * 12f + 2050f, 4);
            frost = Mathf.Clamp01((frost + 0.5f) / 1.5f);

            Color result = c;

            // Снежинки
            if (snowflakes > 0f)
            {
                result = Blend(result, new Color(0.95f, 0.97f, 1.0f, 1f), snowflakes * 0.7f * intensity);
            }

            // Иней
            result = Blend(result, new Color(0.85f, 0.9f, 0.95f, 1f), frost * 0.15f * intensity);

            return result;
        }

        /// <summary>
        /// Пыль — песчаные частицы и тёплый оттенок.
        /// </summary>
        private static Color ApplyDustOverlay(Color c, float u, float v, float intensity)
        {
            // Пылевые частицы
            float dust = ProceduralNoise.FBM(u * 18f + 2100f, v * 18f + 2100f, 3, 2f, 0.4f);
            dust = Mathf.Clamp01((dust + 0.3f) / 1.3f);

            // Крупинки
            float grains = ProceduralNoise.Voronoi(u * 25f + 2150f, v * 25f + 2150f, 30f);
            grains = Mathf.Clamp01(grains < 0.1f ? 1f : 0f);

            Color result = c;

            // Пылевой налёт
            result = Blend(result, new Color(0.75f, 0.65f, 0.45f, 1f), dust * 0.2f * intensity);

            // Крупинки
            if (grains > 0f)
            {
                result = Blend(result, new Color(0.8f, 0.7f, 0.5f, 1f), grains * 0.5f * intensity);
            }

            return result;
        }

        /// <summary>
        /// Пепел — тёмные частицы, дымовой налёт.
        /// </summary>
        private static Color ApplyAshOverlay(Color c, float u, float v, float intensity)
        {
            // Пепельные хлопья
            float ash = ProceduralNoise.FBM(u * 14f + 2200f, v * 14f + 2200f, 3);
            ash = Mathf.Clamp01((ash + 0.5f) / 1.5f);

            // Тёмные пятна
            float darkSpots = ProceduralNoise.Voronoi(u * 10f + 2250f, v * 10f + 2250f, 12f);
            darkSpots = Mathf.Clamp01(darkSpots < 0.12f ? 1f : 0f);

            Color result = c;

            // Пепельный налёт
            result = Blend(result, new Color(0.4f, 0.38f, 0.36f, 1f), ash * 0.2f * intensity);

            // Тёмные пятна
            if (darkSpots > 0f)
            {
                result = Blend(result, new Color(0.2f, 0.18f, 0.17f, 1f), darkSpots * 0.3f * intensity);
            }

            return result;
        }

        /// <summary>
        /// Дождь — потёки и мокрые пятна.
        /// </summary>
        private static Color ApplyRainOverlay(Color c, float u, float v, float intensity)
        {
            // Потёки (вертикальные полосы через noise)
            float streak = ProceduralNoise.FBM(u * 30f + 2300f, v * 5f + 2300f, 3, 2f, 0.6f);
            streak = Mathf.Clamp01((streak + 0.2f) / 1.2f);

            // Мокрые пятна
            float wet = ProceduralNoise.Voronoi(u * 8f + 2350f, v * 8f + 2350f, 10f);
            wet = Mathf.Clamp01(1f - wet * 1.5f);

            Color result = c;

            // Потёки — затемнение
            result.r *= (1f - streak * 0.08f * intensity);
            result.g *= (1f - streak * 0.08f * intensity);
            result.b *= (1f - streak * 0.05f * intensity);

            // Мокрые пятна — лёгкий блеск + затемнение
            float wetAmount = wet * 0.1f * intensity;
            float lum = 0.299f * result.r + 0.587f * result.g + 0.114f * result.b;
            result.r = result.r + (lum - result.r) * wetAmount * 0.5f;
            result.g = result.g + (lum - result.g) * wetAmount * 0.5f;
            result.b = result.b + (lum - result.b) * wetAmount * 0.5f;
            result.r = Mathf.Min(1f, result.r + wet * 0.03f * intensity);
            result.g = Mathf.Min(1f, result.g + wet * 0.03f * intensity);
            result.b = Mathf.Min(1f, result.b + wet * 0.05f * intensity);

            return result;
        }

        /// <summary>
        /// Туман — общая бледность и размытие контраста.
        /// </summary>
        private static Color ApplyFogOverlay(Color c, float u, float v, float intensity)
        {
            // Туманность (низкочастотный шум)
            float fog = ProceduralNoise.FBM(u * 3f + 2400f, v * 3f + 2400f, 3);
            fog = Mathf.Clamp01((fog + 0.5f) / 1.5f);

            // Плотность (переходы)
            float density = ProceduralNoise.FBM(u * 6f + 2450f, v * 6f + 2450f, 2);
            density = Mathf.Clamp01((density + 0.3f) / 1.3f);

            Color result = c;

            // Бледность к средне-серому
            Color fogColor = new Color(0.7f, 0.72f, 0.75f, 1f);
            float fogAmount = (fog * 0.6f + density * 0.2f) * intensity;
            result = Blend(result, fogColor, fogAmount * 0.25f);

            // Снижение контраста
            float lum = 0.299f * result.r + 0.587f * result.g + 0.114f * result.b;
            float desat = fog * 0.2f * intensity;
            result.r = result.r + (lum - result.r) * desat;
            result.g = result.g + (lum - result.g) * desat;
            result.b = result.b + (lum - result.b) * desat;

            return result;
        }

        // ============================================================
        // УТИЛИТЫ СМЕШИВАНИЯ
        // ============================================================

        /// <summary>
        /// Смешивает два цвета. amount = 0 → base, amount = 1 → overlay.
        /// </summary>
        private static Color Blend(Color baseColor, Color overlay, float amount)
        {
            amount = Mathf.Clamp01(amount);
            return new Color(
                baseColor.r + (overlay.r - baseColor.r) * amount,
                baseColor.g + (overlay.g - baseColor.g) * amount,
                baseColor.b + (overlay.b - baseColor.b) * amount,
                baseColor.a
            );
        }
    }
}
