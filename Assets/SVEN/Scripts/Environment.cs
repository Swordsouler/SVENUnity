namespace SVEN
{
    /// <summary>
    /// The environment where the application is running.
    /// </summary>
    public static class Environment
    {
        /// <summary>
        /// Resource identifier for the environment.
        /// </summary>
        private static string _resourceID = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the resource identifier for the environment.
        /// </summary>
        /// <returns>Resource identifier.</returns>
        public static string ResourceID()
        {
            return _resourceID;
        }

        /// <summary>
        /// Gets the resource for the environment.
        /// </summary>
        /// <returns>Resource.</returns>
        public static string Resource()
        {
            return ResourceID();
        }
    }
}