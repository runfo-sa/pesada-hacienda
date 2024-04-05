namespace pesada_hacienda.Core
{
    /// <summary>
    /// Estructura que guarda el nombre + extension y los datos de un frame guardado.
    /// </summary>
    public struct SavedFrame(string name, MemoryStream data)
    {
        /// <summary>
        /// Nombre del frame guardado, incluye la extension ".jpg"
        /// </summary>
        public string Name { get; set; } = name;

        public MemoryStream Data { get; set; } = data;
    }
}
