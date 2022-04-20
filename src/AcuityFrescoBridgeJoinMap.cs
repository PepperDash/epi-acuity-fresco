using PepperDash.Essentials.Core.Bridges;

namespace PepperDashPluginAcuityFresco
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
    public class AcuityFrescoBridgeJoinMap : GenericLightingJoinMap
	{
        /*
        GenericLightingJoinMap
        
         [JoinName("IsOnline")] 
         * JoinNumber = 1
         * JoinSpan = 1
         * JoinCapabilities = eJoinCapabilities.ToSIMPL
         * JoinType = eJoinType.Digital

        [JoinName("SelectScene")]
         * JoinNumber = 1
         * JoinSpan = 1
         * JoinCapabilities = eJoinCapabilities.FromSIMPL
         * JoinType = eJoinType.Digital

        [JoinName("SelectSceneDirect")]
         * JoinNumber = 11
         * JoinSpan = 10
         * JoinCapabilities = eJoinCapabilities.ToFromSIMPL
         * JoinType = eJoinType.DigitalSerial

        [JoinName("ButtonVisibility")]
         * JoinNumber = 41
         * JoinSpan = 10
         * JoinCapabilities = eJoinCapabilities.ToSIMPL
         * JoinType = eJoinType.Digital

        ** NOT USED **
        [JoinName("IntegrationIdSet")]
         * JoinNumber = 1
         * JoinSpan = 1
         * JoinCapabilities = eJoinCapabilities.FromSIMPL
         * JoinType = eJoinType.Serial
        */

		#region Digital

		

		#endregion


		#region Analog		
		

		#endregion


		#region Serial
		

		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
		public AcuityFrescoBridgeJoinMap(uint joinStart)
			: base(joinStart, typeof(AcuityFrescoBridgeJoinMap))
		{
		}
	}
}
