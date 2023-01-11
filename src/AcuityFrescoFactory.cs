using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDashPluginAcuityFresco
{
	/// <summary>
	/// Plugin factory for devices that require communications using IBasicCommunications or custom communication methods
	/// </summary>
	public class AcuityFrescoFactory : EssentialsPluginDeviceFactory<AcuityFrescoDevice>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		public AcuityFrescoFactory()
		{
			// Set the minimum Essentials Framework Version
			MinimumEssentialsFrameworkVersion = "1.9.7";

			// In the constructor we initialize the list with the typenames that will build an instance of this device
			// only include unique typenames, when the constructur is used all the typenames will be evaluated in lower case.
			TypeNames = new List<string>() { "acuityfresco" };
		}

		/// <summary>
		/// Builds and returns an instance of AcuityFrescoDevice
		/// </summary>
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			try
			{
				Debug.Console(2, new string('*', 80));
				Debug.Console(2, new string('*', 80));
				Debug.Console(0, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);				
				
				// get the plugin device properties configuration object & check for null 
				var propertiesConfig = dc.Properties.ToObject<AcuityFrescoPropertiesConfig>();
				if (propertiesConfig == null)
				{
					Debug.Console(0, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
					return null;
				}				

				// build the plugin device comms (for all other comms methods) & check for null			
				var comms = CommFactory.CreateCommForDevice(dc);
				if (comms != null) return new AcuityFrescoDevice(dc.Key, dc.Name, propertiesConfig, comms);
				Debug.Console(0, "[{0}] Factory: failed to create comm for {1}", dc.Key, dc.Name);
				return null;
			}
			catch (Exception ex)
			{
				Debug.Console(0, "[{0}] Factory BuildDevice Exception: {1}", dc.Key, ex);
				return null;
			}
		}
	}
}