using Microsoft.Azure.Devices.Client;
using System;

namespace SmartHomePi
{
    /// <summary>
    /// Main class that will be executed on this console app.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Azure IoT Hub Connection string for this IoT device
        /// </summary>
        private static string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");

        /// <summary>
        /// Main method which will execute the program in the RPi. Program will exit once ENTER key is pressed.
        /// </summary>
        /// <param name="args">[Optional]Azure IoT Hub Connection string in case IOTHUB_DEVICE_CONN_STRING environment variable is not set.</param>
        /// <returns>0 if the application finished succesfully. 1 otherwise.</returns>
        public static int Main(string[] args)
        {
            // If env variable is not set, try to get the connection string from the console args.
            if (string.IsNullOrEmpty(s_deviceConnectionString) && args.Length > 0)
            {
                s_deviceConnectionString = args[0];
            }

            // Connect with Azure IoT Hub.
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString);
            if (deviceClient == null)
            {
                Console.WriteLine("Failed to create DeviceClient!");
                return 1;
            }

            // Create the SampleObject and wait for ENTER in order to terminate the program.
            using (var sample = new BuildSample(deviceClient))
            {
                Console.WriteLine("Press ENTER to exit...");
                string line = Console.ReadLine();
                Console.WriteLine("Done.\n");
            }
            return 0;
        }
    }
}
