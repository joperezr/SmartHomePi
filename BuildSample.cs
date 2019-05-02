using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Device.Gpio;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomePi
{
    /// <summary>
    /// Driver class which handles the methods sent from Azure IoT Hub and controls the Raspberry Pi.
    /// </summary>
    internal class BuildSample : IDisposable
    {
        /// <summary>
        /// The DeviceClient that provides communication with Azure IoT Hub.
        /// </summary>
        private DeviceClient _deviceClient;
        /// <summary>
        /// The GpioController that controls the RaspberryPi.
        /// </summary>
        private GpioController _gpioController;

        /// <summary>
        /// Main constructor. Takes in the DeviceClient connection with Azure IoT Hub.
        /// </summary>
        /// <param name="deviceClient">Azure IoT Hub DeviceClient connection.</param>
        public BuildSample(DeviceClient deviceClient)
        {
            // Setting the Azure method handlers for C2D communication
            _deviceClient = deviceClient;
            _deviceClient.SetMethodHandlerAsync("ChangeLightBulbState", ChangeLightBulbState, null).Wait();

            // Setting up Gpio Pins
            _gpioController = new GpioController();
            _gpioController.OpenPin(26, PinMode.Output);
            _gpioController.OpenPin(20, PinMode.Output);
            _gpioController.OpenPin(21, PinMode.Output);
            _gpioController.Write(26, true);
            _gpioController.Write(20, true);
            _gpioController.Write(21, true);
        }

        /// <summary>
        /// Method handler that will change the state of a light bulb connected to the Raspberry Pi.
        /// </summary>
        /// <param name="methodRequest">The request that contains the payload with the light bulb information.</param>
        /// <param name="userContext">The user context</param>
        /// <returns>A method response with a status code and a message.</returns>
        public Task<MethodResponse> ChangeLightBulbState(MethodRequest methodRequest, object userContext)
        {
            // Get the data from the method request, and serialize it to a LightBulbState object.
            var data = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(methodRequest.DataAsJson));
            LightBulbState dataAsState;
            try
            {
                dataAsState = JsonConvert.DeserializeObject<LightBulbState>(data);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid payload.");
                Console.ResetColor();
                return Task.FromResult(new MethodResponse(500));
            }
            int gpioPin;

            // Select the gpioPin that will need to get changed depending on the method request.
            // Error in case of invalid method request.
            switch(dataAsState.Id)
            {
                case 1:
                    gpioPin = 26;
                    break;
                case 2:
                    gpioPin = 20;
                    break;
                case 3:
                    gpioPin = 21;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid light bulb Id. Acceptable values are (1, 2, 3).");
                    Console.ResetColor();
                    return Task.FromResult(new MethodResponse(500));
            }

            // Print message to the console of the message that was received.
            Console.ForegroundColor = ConsoleColor.Green;
            string state = dataAsState.State ? "On" : "Off";
            Console.WriteLine($"Setting the bulb with id {dataAsState.Id} to {state}");
            Console.ResetColor();

            // Turn the light on/off.
            _gpioController.Write(gpioPin, !dataAsState.State);

            // Construct the method response payload and send the response back to the Cloud.
            var result = new
            {
                status = "Success",
                message = $"The light bulb {dataAsState.Id} was turned {state}"
            };
            var resultString = JsonConvert.SerializeObject(result);
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(resultString), 200));
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            // Dispose the connection with Azure IoT Hub.
            _deviceClient?.Dispose();
            _deviceClient = null;
            // Dispose the Raspberry Pi controller.
            _gpioController?.Dispose();
            _gpioController = null;
        }
    }

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
}