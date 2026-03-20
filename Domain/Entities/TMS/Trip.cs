public class Trip
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int VehicleId { get; set; }
    public int DeviceId { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}