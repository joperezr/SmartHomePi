using Iot.Device.Bmx280;
using Iot.Units;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using SmartHomeCloud.Models;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.I2c.Drivers;
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
        /// Dictionary that holds the current state of all light bulbs.
        /// </summary>
        private Dictionary<int, bool> _lightsStatus;
        /// <summary>
        /// The DeviceClient that provides communication with Azure IoT Hub.
        /// </summary>
        private DeviceClient _deviceClient;
        /// <summary>
        /// The GpioController that controls the RaspberryPi.
        /// </summary>
        private GpioController _gpioController;
        /// <summary>
        /// Bme280 Temperature Sensor that will be used to get preassure and temperature data.
        /// </summary>
        private Bme280 _temperatureSensor;

        /// <summary>
        /// Main constructor. Takes in the DeviceClient connection with Azure IoT Hub.
        /// </summary>
        /// <param name="deviceClient">Azure IoT Hub DeviceClient connection.</param>
        public BuildSample(DeviceClient deviceClient)
        {
            // Setting the Azure method handlers for C2D communication
            _deviceClient = deviceClient;
            _deviceClient.SetMethodHandlerAsync("ChangeLightBulbState", ChangeLightBulbState, null).Wait();
            _deviceClient.SetMethodHandlerAsync("GetLightBulbStatus", GetLightBulbStatus, null).Wait(); 
            _deviceClient.SetMethodHandlerAsync("GetTemperatureAndPreassure", GetTemperatureAndPreassure, null).Wait();

            // Setting up the temperature sensor
            var i2cDevice = new UnixI2cDevice(new I2cConnectionSettings(1, 0x77));
            _temperatureSensor = new Bme280(i2cDevice);

            // Setting up Gpio Pins
            _gpioController = new GpioController();
            _gpioController.OpenPin(26, PinMode.Output);
            _gpioController.OpenPin(20, PinMode.Output);
            _gpioController.OpenPin(21, PinMode.Output);
            _gpioController.Write(26, true);
            _gpioController.Write(20, true);
            _gpioController.Write(21, true);

            // Setting up Dictionary of light bulb state
            _lightsStatus = new Dictionary<int, bool>();
            _lightsStatus.Add(1, false);
            _lightsStatus.Add(2, false);
            _lightsStatus.Add(3, false);
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
            _lightsStatus[dataAsState.Id] = dataAsState.State;

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
        /// Method that will return the state of all configured light bulbs.
        /// </summary>
        /// <param name="methodRequest">The request that was sent from the server.</param>
        /// <param name="userContext">The user context</param>
        /// <returns>A list of LightBulbState with the information of the status of each light bulb.</returns>
        public Task<MethodResponse> GetLightBulbStatus(MethodRequest methodRequest, object userContext)
        {
            // Print message to the console of the message that was received.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Getting a request from Hub for Light Status.");
            Console.ResetColor();

            // Construct the response with the current state.
            LightBulbState[] result = new LightBulbState[]
            {
                new LightBulbState { Id = 1, State = _lightsStatus[1] },
                new LightBulbState { Id = 3, State = _lightsStatus[3] }
            };
            var resultString = JsonConvert.SerializeObject(result);
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(resultString), 200));
        }

        /// <summary>
        /// Method handler that will return the current temperature and pressure information.
        /// </summary>
        /// <param name="methodRequest">The request that was sent from the server.</param>
        /// <param name="userContext">The user context.</param>
        /// <returns></returns>
        public async Task<MethodResponse> GetTemperatureAndPreassure(MethodRequest methodRequest, object userContext)
        {
            // Print message to the console of the message that was received.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Getting a request from Hub for temperature and preassure info.");
            Console.ResetColor();

            // Get temperature and preassure data.
            Temperature temp = await _temperatureSensor.ReadTemperatureAsync();
            double pressure = await _temperatureSensor.ReadPressureAsync();

            // Print message to the console of the data.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Temperature in degrees Fahrenheit: {temp.Fahrenheit}");
            Console.WriteLine($"Preassure in Pascals:              {pressure}");
            Console.ResetColor();

            // Construct the response object.
            var result = new TemperatureData { TemperatureInFahrenheit = temp.Fahrenheit, PreassureInPascals = pressure };
            var resultString = JsonConvert.SerializeObject(result);
            return new MethodResponse(Encoding.UTF8.GetBytes(resultString), 200);
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
            // Dispose temperature sensor.
            _temperatureSensor?.Dispose();
            _temperatureSensor = null;
        }
    }
}