using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using SmartHomeCloud.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeCloud
{
    /// <summary>
    /// Class that will abstract the communication between a IoTClient and Azure IoT Hub.
    /// </summary>
    public class HubController : IDisposable
    {
        /// <summary>
        /// The Service Client connection that is used to talk to Azure IoT Hub.
        /// </summary>
        private ServiceClient _serviceClient;
        /// <summary>
        /// The name of the device on the IoT Hub that we will be talking to.
        /// </summary>
        private readonly string _deviceToControl;

        /// <summary>
        /// Creates an instance of HubController which can talk to a device in an Azure IoT Hub.
        /// </summary>
        /// <param name="azureIoTHubConnectionString">The service connection string which will be used to talk to Azure IoT</param>
        /// <param name="deviceToControl">The id of the device to control.</param>
        public HubController(string azureIoTHubConnectionString, string deviceToControl = "MyRaspberryPi")
        {
            // Validate the connection string, and create the Service client in case of success.
            if (string.IsNullOrEmpty(azureIoTHubConnectionString))
            {
                throw new ArgumentException("Connection string can not be null or empty", nameof(azureIoTHubConnectionString));
            }
            try
            {
                _serviceClient = ServiceClient.CreateFromConnectionString(azureIoTHubConnectionString);
            }
            catch(Exception e)
            {
                throw new InvalidOperationException("Error when connecting with Azure IoT Hub.", e);
            }
            _deviceToControl = deviceToControl;
        }

        /// <summary>
        /// Method that will change a single light bulb state.
        /// </summary>
        /// <param name="lightBulbToChange">The id of the light bulb to change.</param>
        /// <param name="newState">True in order to turn the light bulb on. False otherwise.</param>
        public async Task ChangeLightBulbState(int lightBulbToChange, bool newState)
        {
            // Generate the method invocation with the payload, and send it with the service client.
            var methodInvocation = new CloudToDeviceMethod("ChangeLightBulbState") { ResponseTimeout = TimeSpan.FromSeconds(30) };
            LightBulbState payload = new LightBulbState { Id = lightBulbToChange, State = newState };
            methodInvocation.SetPayloadJson(JsonConvert.SerializeObject(payload));
            var response = await _serviceClient.InvokeDeviceMethodAsync(_deviceToControl, methodInvocation);
            if (response.Status != 200)
            {
                throw new ApplicationException($"There was an error when processing your request. The server returned http {response.Status}\n{response.ToString()}");
            }
        }

        /// <summary>
        /// Method that will query the IoT Device for the status of all the light bulbs connected to it.
        /// </summary>
        /// <returns>A collection of light bulb status.</returns>
        public async Task<IEnumerable<LightBulbState>> GetLightBulbStatus()
        {
            // Construct the method invocation, and parse the results into a collection.
            var methodInvocation = new CloudToDeviceMethod("GetLightBulbStatus") { ResponseTimeout = TimeSpan.FromSeconds(30) };
            var response = await _serviceClient.InvokeDeviceMethodAsync(_deviceToControl, methodInvocation);
            if (response.Status == 200)
            {
                var result = JsonConvert.DeserializeObject<LightBulbState[]>(response.GetPayloadAsJson());
                return result;
            }
            else
            {
                throw new ApplicationException($"There was an error when processing your request. The server returned http {response.Status}\n{response.ToString()}");
            }
        }

        /// <summary>
        /// Method that will query the IoT Device for the current Temperature and Pressure measurement.
        /// </summary>
        /// <returns>An object containing both the temperature in degrees Fahrenheit and the pressure in Pascals.</returns>
        public async Task<TemperatureData> GetTemperatureAndPreassure()
        {
            var methodInvocation = new CloudToDeviceMethod("GetTemperatureAndPreassure") { ResponseTimeout = TimeSpan.FromSeconds(30) };
            var response = await _serviceClient.InvokeDeviceMethodAsync(_deviceToControl, methodInvocation);
            if (response.Status == 200)
            {
                var result = JsonConvert.DeserializeObject<TemperatureData>(response.GetPayloadAsJson());
                return result;
            }
            else
            {
                throw new ApplicationException($"There was an error when processing your request. The server returned http {response.Status}\n{response.ToString()}");
            }
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            _serviceClient?.Dispose();
            _serviceClient = null;
        }
    }
}
