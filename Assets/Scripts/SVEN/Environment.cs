namespace SVEN
{
    /// <summary>
    /// The environment where the application is running.
    /// </summary>
    public class Environment : Resource
    {
        /// <summary>
        /// The name of the environment.
        /// </summary>
        private static Environment current;
        /// <summary>
        /// The current environment.
        /// </summary>
        public static Environment Current
        {
            get
            {
                current ??= new Environment();
                return current;
            }
        }
    }
}