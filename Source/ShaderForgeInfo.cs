using System.Reflection;
using UnityEngine;

namespace ShaderForge
{
    /// <summary>
    /// ShaderForge v2.0 — Процедурная перерисовка ВСЕХ текстур в RimWorld.
    /// 
    /// Один патч на ContentFinder&lt;Texture2D&gt;.Get() = вся игра перерисована.
    /// Работает автоматически на все моды. Без рук. Без рисунков. Чистая математика.
    /// 
    /// "Армия шейдеров" — каждый пиксель проходит через процедурный шум.
    /// 
    /// v2.0 NEW:
    /// - 5 новых стилей: Mystic, Toxic, Scorched, Overgrown, Wasteland
    /// - Статическая цветокоррекция (hue, saturation, brightness, contrast, tint)
    /// - Погодные оверлеи (snow, dust, ash, rain, fog) — запекаются в текстуру
    /// - 8 встроенных пресетов (Ядерная Зима, Токсичная Пустошь, ...)
    /// - Пер-биом стили (отдельный стиль для террейна/растений)
    /// - Маски по def'ам (blacklist/whitelist)
    /// - Профили производительности (Low/Medium/High)
    /// - Превью стиля прямо в настройках
    /// - 3 новых типа материалов (Mushroom, Crystal, Ash)
    /// - Новые шумовые функции (Ridged, DomainWarp, VoronoiEdges, Cellular)
    /// 
    /// Автор: Semushkin Alexander Gennadyevich (Elengal)
    /// GitHub: https://github.com/Elengal
    /// Email: Konter88@mail.ru
    /// </summary>
    public static class ShaderForgeInfo
    {
        public const string ModId = "com.shaderforge.textureprocessor";
        public const string ModName = "ShaderForge — Процедурные Текстуры v2";
        public const string Version = "2.0.0";

        public static string GetDescription()
        {
            return "Процедурная перерисовка всех текстур в RimWorld.\n" +
                   "Работает автоматически на все моды.\n" +
                   "Без текстур. Без рисунков. Чистая математика.\n\n" +
                   "12 стилей: боевой, ржавый, пустыня, снег, древний, кровь,\n" +
                   "чистый, мистический, токсичный, обожжённый, заросший, пустошь.\n\n" +
                   "6 фильтров: предметы, здания, терен, растения, пешки, UI.\n" +
                   "8 пресетов: Ядерная Зима, Токсичная Пустошь, Древние Руины, ...\n" +
                   "Цветокоррекция: оттенок, насыщенность, яркость, контраст, tint.\n" +
                   "5 погодных оверлеев: снег, пыль, пепел, дождь, туман.\n" +
                   "Пер-биом стили, маски по def'ам, профили производительности.";
        }
    }
}
