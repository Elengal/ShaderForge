using System;
using UnityEngine;

namespace ShaderForge
{
    /// <summary>
    /// Процедурные функции шума — армия шейдеров на C#.
    /// Perlin noise, FBM, Voronoi, Turbulence, Ridged, DomainWarp.
    /// Всё генерируется из математики, никаких текстур.
    /// v2.0 — добавлены: RidgedNoise, SeamlessVoronoi, DomainWarp, CellularNoise
    /// </summary>
    public static class ProceduralNoise
    {
        // Сид для воспроизводимости (каждая текстура получает свой сид)
        private static int _seed = 42;

        public static void SetSeed(int seed) => _seed = seed;

        // ============================================================
        // PERLIN NOISE 2D
        // ============================================================

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        private static float Grad2D(int hash, float x, float y)
        {
            int h = hash & 7;
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>
        /// 2D Perlin noise. Возвращает значение в диапазоне примерно [-1, 1].
        /// </summary>
        public static float Perlin(float x, float y)
        {
            int xi = (int)Math.Floor(x) & 0xFF;
            int yi = (int)Math.Floor(y) & 0xFF;

            float xf = x - (int)Math.Floor(x);
            float yf = y - (int)Math.Floor(y);

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = Hash2D(xi, yi);
            int ab = Hash2D(xi, yi + 1);
            int ba = Hash2D(xi + 1, yi);
            int bb = Hash2D(xi + 1, yi + 1);

            float x1 = Lerp(Grad2D(aa, xf, yf), Grad2D(ba, xf - 1f, yf), u);
            float x2 = Lerp(Grad2D(ab, xf, yf - 1f), Grad2D(bb, xf - 1f, yf - 1f), u);

            return Lerp(x1, x2, v);
        }

        // ============================================================
        // FBM (Fractional Brownian Motion)
        // ============================================================

        /// <summary>
        /// FBM — многослойный шум для реалистичных текстур.
        /// </summary>
        public static float FBM(float x, float y, int octaves = 4, float lacunarity = 2.0f, float persistence = 0.5f)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxVal = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += Perlin(x * frequency, y * frequency) * amplitude;
                maxVal += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxVal;
        }

        // ============================================================
        // VORONOI NOISE (ячейки, трещины, пятна)
        // ============================================================

        /// <summary>
        /// Voronoi noise — ячеистая структура.
        /// </summary>
        public static float Voronoi(float x, float y, float scale = 5.0f)
        {
            x *= scale;
            y *= scale;

            int ix = (int)Math.Floor(x);
            int iy = (int)Math.Floor(y);

            float minDist = float.MaxValue;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = ix + dx;
                    int cy = iy + dy;

                    float px = cx + Random01(Hash2D(cx, cy));
                    float py = cy + Random01(Hash2D(cx + 31, cy + 17));

                    float distX = x - px;
                    float distY = y - py;
                    float dist = distX * distX + distY * distY;

                    if (dist < minDist)
                        minDist = dist;
                }
            }

            return (float)Math.Sqrt(minDist);
        }

        // ============================================================
        // VORONOI EDGES (только границы ячеек)
        // ============================================================

        /// <summary>
        /// Voronoi Edges — возвращает только границы ячеек (0..1).
        /// Отлично для рун, трещин, контуров.
        /// edgeWidth = толщина границы (0.02 = тонкие, 0.1 = жирные)
        /// </summary>
        public static float VoronoiEdges(float x, float y, float scale = 5.0f, float edgeWidth = 0.05f)
        {
            x *= scale;
            y *= scale;

            int ix = (int)Math.Floor(x);
            int iy = (int)Math.Floor(y);

            float minDist = float.MaxValue;
            float secondDist = float.MaxValue;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = ix + dx;
                    int cy = iy + dy;

                    float px = cx + Random01(Hash2D(cx, cy));
                    float py = cy + Random01(Hash2D(cx + 31, cy + 17));

                    float distX = x - px;
                    float distY = y - py;
                    float dist = (float)Math.Sqrt(distX * distX + distY * distY);

                    if (dist < minDist)
                    {
                        secondDist = minDist;
                        minDist = dist;
                    }
                    else if (dist < secondDist)
                    {
                        secondDist = dist;
                    }
                }
            }

            float edge = secondDist - minDist;
            return Mathf.Clamp01(1f - edge / edgeWidth);
        }

        // ============================================================
        // SEAMLESS VORONOI (бесшовные ячейки)
        // ============================================================

        /// <summary>
        /// Seamless Voronoi — бесшовная версия для тайловых текстур.
        /// </summary>
        public static float SeamlessVoronoi(float u, float v, float scale = 4.0f)
        {
            float s = u * scale;
            float t = v * scale;

            // Четыре пробы для бесшовности
            float v1 = Voronoi(s, t, 1f);
            float v2 = Voronoi(s + 5.2f, t + 1.3f, 1f);
            float v3 = Voronoi(s + 1.7f, t + 9.2f, 1f);
            float v4 = Voronoi(s + 8.3f, t + 2.8f, 1f);

            // Плавное сведение
            float ix = (float)Math.Cos(s * 0.5f) * (1.0f - 2.0f * t);
            float iy = (float)Math.Sin(s * 0.5f) * (1.0f - 2.0f * t);
            float jx = (float)Math.Cos(t * 0.5f + 1.0f) * (1.0f - 2.0f * s);
            float jy = (float)Math.Sin(t * 0.5f + 1.0f) * (1.0f - 2.0f * s);

            return Lerp(
                Lerp(v1, v2, ix),
                Lerp(v3, v4, jx),
                iy
            );
        }

        // ============================================================
        // RIDGED NOISE (резкие хребты, горы, молнии)
        // ============================================================

        /// <summary>
        /// Ridged Multifractal — резкие хребты вместо плавных холмов.
        /// Идеально для: молний, рун, обожжённых поверхностей, радиоактивных линий.
        /// </summary>
        public static float RidgedNoise(float x, float y, int octaves = 4, float lacunarity = 2.0f, float gain = 2.0f)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float prev = 1f;

            for (int i = 0; i < octaves; i++)
            {
                float n = Perlin(x * frequency, y * frequency);
                n = 1f - (float)Math.Abs(n); // Инверсия — создаём хребты
                n = n * n; // Усиление резкости

                // Weight blending для детализации
                float weight = Mathf.Clamp01(n * prev);
                prev = weight;

                total += n * amplitude * weight;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return total;
        }

        // ============================================================
        // DOMAIN WARP (искажение координат)
        // ============================================================

        /// <summary>
        /// Domain Warp — искажает UV-координаты через шум.
        /// Создаёт органические, плавные паттерны.
        /// Отлично для: пламени, дыма, ядовитых луж, магических потоков.
        /// </summary>
        public static void DomainWarp(float u, float v, float scale, float strength,
            out float warpedU, out float warpedV, int octaves = 3)
        {
            float qx = FBM(u * scale, v * scale, octaves);
            float qy = FBM(u * scale + 5.2f, v * scale + 1.3f, octaves);

            warpedU = u + qx * strength;
            warpedV = v + qy * strength;
        }

        // ============================================================
        // TURBULENCE (для огня, облаков, искажений)
        // ============================================================

        /// <summary>
        /// Turbulence — абсолютное значение FBM.
        /// </summary>
        public static float Turbulence(float x, float y, int octaves = 4)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;

            for (int i = 0; i < octaves; i++)
            {
                total += Math.Abs(Perlin(x * frequency, y * frequency)) * amplitude;
                amplitude *= 0.5f;
                frequency *= 2.0f;
            }

            return total;
        }

        // ============================================================
        // SEAMLESS FBM (бесшовные текстуры)
        // ============================================================

        /// <summary>
        /// Seamless FBM — бесшовная версия для тайловых текстур.
        /// </summary>
        public static float SeamlessFBM(float x, float y, int octaves = 4, float scale = 1.0f)
        {
            float s = x * scale;
            float t = y * scale;

            float n1 = FBM(s, t, octaves);
            float n2 = FBM(s + 5.2f, t + 1.3f, octaves);
            float n3 = FBM(s + 1.7f, t + 9.2f, octaves);
            float n4 = FBM(s + 8.3f, t + 2.8f, octaves);

            float ix = (float)Math.Cos(s * 0.5f) * (1.0f - 2.0f * t);
            float iy = (float)Math.Sin(s * 0.5f) * (1.0f - 2.0f * t);
            float jx = (float)Math.Cos(t * 0.5f + 1.0f) * (1.0f - 2.0f * s);
            float jy = (float)Math.Sin(t * 0.5f + 1.0f) * (1.0f - 2.0f * s);

            return Lerp(
                Lerp(n1, n2, ix),
                Lerp(n3, n4, jx),
                iy
            );
        }

        // ============================================================
        // CELLULAR NOISE (биологические текстуры)
        // ============================================================

        /// <summary>
        /// Cellular Noise — органические ячейки с двумя расстояниями.
        /// F1 = ближайшая ячейка, F2 = вторая.
        /// Используется для: биологических текстур, грибов, мха.
        /// </summary>
        public static void Cellular(float x, float y, float scale,
            out float f1, out float f2)
        {
            x *= scale;
            y *= scale;

            int ix = (int)Math.Floor(x);
            int iy = (int)Math.Floor(y);

            f1 = float.MaxValue;
            f2 = float.MaxValue;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = ix + dx;
                    int cy = iy + dy;

                    float px = cx + Random01(Hash2D(cx, cy));
                    float py = cy + Random01(Hash2D(cx + 31, cy + 17));

                    float distX = x - px;
                    float distY = y - py;
                    float dist = (float)Math.Sqrt(distX * distX + distY * distY);

                    if (dist < f1)
                    {
                        f2 = f1;
                        f1 = dist;
                    }
                    else if (dist < f2)
                    {
                        f2 = dist;
                    }
                }
            }
        }

        // ============================================================
        // HASH ФУНКЦИИ (детерминированный рандом)
        // ============================================================

        private static int Hash2D(int x, int y)
        {
            int n = x * 374761393 + y * 668265263 + _seed;
            n = (n ^ (n >> 13)) * 1274126177;
            n = n ^ (n >> 16);
            return n;
        }

        private static float Random01(int hash)
        {
            return (float)(hash & 0xFFFF) / 65535f;
        }
    }
}
