using System;
using UnityEngine;

namespace ShaderForge
{
    /// <summary>
    /// Определяет тип материала по цвету пикселя.
    /// Автоматическая классификация без всяких метаданных.
    /// v2.0 — добавлены: Mushroom, Crystal, Ash
    /// </summary>
    public enum MaterialType
    {
        Unknown,
        Metal,      // серый, стальной
        Wood,       // коричневый, тёплый
        Stone,      // серо-коричневый, тёмный
        Fabric,     // яркий, насыщенный
        Skin,       // бежевый, розоватый
        Plastic,    // чистый, насыщенный
        Glass,      // светло-голубой, прозрачный
        Plant,      // зелёный
        Dirt,       // тёмно-коричневый, грязный
        Snow,       // белый, бледный
        Fire,       // красный, оранжевый, жёлтый
        Blood,      // тёмно-красный
        Water,      // синий, голубой
        Mushroom,   // пурпурный, лавандовый
        Crystal,    // светлый, яркий, преломляющий
        Ash         // тёмно-серый, пепельный
    }

    /// <summary>
    /// Анализатор материалов — определяет ЧТО на картинке по ЦВЕТУ.
    /// Ни одного рисунка, ни одной метки — чистая математика.
    /// </summary>
    public static class MaterialAnalyzer
    {
        /// <summary>
        /// Анализирует один пиксель и определяет тип материала.
        /// </summary>
        public static MaterialType ClassifyPixel(Color c, float alphaThreshold = 0.1f)
        {
            // Прозрачные пиксели — пропуск
            if (c.a < alphaThreshold)
                return MaterialType.Unknown;

            float r = c.r;
            float g = c.g;
            float b = c.b;

            // HSL для лучшей классификации
            float max = Mathf.Max(r, g, b);
            float min = Mathf.Min(r, g, b);
            float delta = max - min;

            // Яркость и насыщенность
            float lightness = (max + min) / 2f;
            float saturation = delta == 0f ? 0f : delta / (1f - Mathf.Abs(2f * lightness - 1f));

            // Тон (hue)
            float hue = 0f;
            if (delta > 0f)
            {
                if (Mathf.Approximately(max, r))
                    hue = 60f * (((g - b) / delta) % 6f);
                else if (Mathf.Approximately(max, g))
                    hue = 60f * (((b - r) / delta) + 2f);
                else
                    hue = 60f * (((r - g) / delta) + 4f);
            }
            if (hue < 0) hue += 360f;

            // === КЛАССИФИКАЦИЯ ===

            // Белый / почти белый → Снег / Кристалл
            if (lightness > 0.85f && saturation < 0.15f)
                return MaterialType.Snow;

            // Кристалл: очень яркий, средне-насыщенный, любые цвета кроме белого
            if (lightness > 0.7f && saturation > 0.2f && saturation < 0.8f)
                return MaterialType.Crystal;

            // Чёрный / очень тёмный → Ash или Unknown
            if (lightness < 0.1f)
                return saturation > 0.02f ? MaterialType.Ash : MaterialType.Unknown;

            // Огонь: красно-оранжево-жёлтый, яркий
            if (hue >= 0f && hue < 60f && saturation > 0.5f && lightness > 0.4f)
                return MaterialType.Fire;

            // Кровь: тёмно-красный
            if (hue >= 340f || (hue < 20f && saturation > 0.4f && lightness < 0.4f && lightness > 0.1f))
                return MaterialType.Blood;

            // Растения: зелёный
            if (hue >= 80f && hue < 170f && saturation > 0.2f)
                return MaterialType.Plant;

            // Вода / голубое
            if (hue >= 190f && hue < 260f && saturation > 0.25f)
                return MaterialType.Water;

            // Грибы: пурпурный / лавандовый (hue 260-320)
            if (hue >= 260f && hue < 320f && saturation > 0.15f && lightness > 0.15f && lightness < 0.6f)
                return MaterialType.Mushroom;

            // Дерево: коричневый
            if (hue >= 15f && hue < 50f && saturation > 0.2f && saturation < 0.8f && lightness > 0.1f && lightness < 0.6f)
                return MaterialType.Wood;

            // Грязь / земля
            if (hue >= 10f && hue < 45f && saturation < 0.35f && lightness < 0.35f && lightness > 0.08f)
                return MaterialType.Dirt;

            // Пепел: тёмно-серый, низкая насыщенность
            if (lightness < 0.2f && saturation < 0.08f)
                return MaterialType.Ash;

            // Камень: серо-коричневый
            if (saturation < 0.2f && lightness > 0.2f && lightness < 0.7f)
                return MaterialType.Stone;

            // Металл: серый
            if (saturation < 0.1f && lightness > 0.15f && lightness < 0.85f)
                return MaterialType.Metal;

            // Кожа: бежевый
            if (hue >= 15f && hue < 40f && saturation > 0.1f && saturation < 0.6f && lightness > 0.5f && lightness < 0.8f)
                return MaterialType.Skin;

            // Ткань: яркий, насыщенный
            if (saturation > 0.4f && lightness > 0.3f && lightness < 0.7f)
                return MaterialType.Fabric;

            return MaterialType.Unknown;
        }

        /// <summary>
        /// Анализирует ВЕСЬ массив пикселей и определяет доминирующий материал.
        /// </summary>
        public static MaterialType AnalyzeImage(Color[] pixels)
        {
            int[] counts = new int[(int)MaterialType.Ash + 1];
            int totalValid = 0;

            foreach (Color c in pixels)
            {
                MaterialType type = ClassifyPixel(c);
                if (type != MaterialType.Unknown)
                {
                    counts[(int)type]++;
                    totalValid++;
                }
            }

            if (totalValid == 0)
                return MaterialType.Unknown;

            int maxCount = 0;
            MaterialType dominant = MaterialType.Unknown;

            for (int i = 1; i < counts.Length; i++)
            {
                if (counts[i] > maxCount)
                {
                    maxCount = counts[i];
                    dominant = (MaterialType)i;
                }
            }

            return dominant;
        }
    }
}
