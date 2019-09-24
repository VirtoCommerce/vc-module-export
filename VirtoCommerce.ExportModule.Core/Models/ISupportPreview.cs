namespace VirtoCommerce.ExportModule.Core.Model
{
    /// <summary>
    /// Interface for allowing data source return viewable entries for preview (e.g. could be light-weight objects without references or properties we do not want to show on preview)
    /// </summary>
    public interface ISupportPreview
    {
        /// <summary>
        /// Fetches viewable data representation from data source.
        /// </summary>
        /// <returns>True if some data received, false otherwise.</returns>
        bool FetchViewable();
    }
}
