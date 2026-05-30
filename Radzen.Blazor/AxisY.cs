namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies which value axis a <see cref="RadzenChart" /> series is plotted against.
    /// </summary>
    public enum AxisY
    {
        /// <summary>
        /// The series uses the primary (left) value axis. This is the default.
        /// </summary>
        Primary,
        /// <summary>
        /// The series uses the secondary (right) value axis. A right <see cref="RadzenValueAxis" />
        /// with <see cref="ValueAxisPosition.Right" /> is rendered for series assigned to this axis.
        /// </summary>
        Secondary
    }
}
