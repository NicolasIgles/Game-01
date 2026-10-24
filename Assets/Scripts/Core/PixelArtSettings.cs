namespace Game01
{
    /// <summary>
    /// Valores base de pixel art del proyecto. Cambiarlos acá y volver a correr
    /// Tools > Pixel Art > Aplicar configuración del proyecto.
    /// </summary>
    public static class PixelArtSettings
    {
        /// <summary>Píxeles por unidad de Unity. 1 tile = 16 px = 1 unidad.</summary>
        public const int PixelsPerUnit = 16;

        /// <summary>Tamaño de tile en píxeles.</summary>
        public const int TileSize = 16;

        /// <summary>Resolución interna del juego (escala ×4 a 1080p, ×8 a 4K).</summary>
        public const int ReferenceWidth = 480;
        public const int ReferenceHeight = 270;
    }
}
