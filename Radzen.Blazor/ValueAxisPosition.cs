namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies the side of the plot area on which a <see cref="RadzenValueAxis" /> is rendered.
    /// </summary>
    public enum ValueAxisPosition
    {
        /// <summary>
        /// The value axis is rendered on the left of the plot area and is the primary axis. This is the default.
        /// </summary>
        Left,
        /// <summary>
        /// The value axis is rendered on the right of the plot area and is the secondary axis.
        /// Series assigned to <see cref="AxisY.Secondary" /> are plotted against this axis.
        /// </summary>
        Right
    }
}
