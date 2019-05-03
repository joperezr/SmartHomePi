namespace SmartHomeCloud.Models
{
    /// <summary>
    /// Model class to represent the messages for the light bulb state.
    /// </summary>
    public class LightBulbState
    {
        /// <summary>
        /// The State of the light bulb on/off.
        /// </summary>
        public bool State { get; set; }
        /// <summary>
        /// The id of the light bulb to be controlled.
        /// </summary>
        public int Id { get; set; }
    }

    /// <summary>
    /// Model class to represent the messages with Temperature information.
    /// </summary>
    public class TemperatureData
    {
        /// <summary>
        /// The temperature in degrees Fahrenheit.
        /// </summary>
        public double TemperatureInFahrenheit { get; set; }
        /// <summary>
        /// The pressure in Pascals.
        /// </summary>
        public double PreassureInPascals { get; set; }
    }
}
