namespace Almostengr.EcobeeStatus.DataTransfer
{
    public class SensorState
    {
        public SensorState(string state)
        {
            State = state;
        }

        public string State { get; set; }
    }
}
