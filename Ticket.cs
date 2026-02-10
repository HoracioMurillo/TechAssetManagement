public class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; }
    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; }
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    // Solución: Agregar la propiedad de navegación TicketHistories
    public ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>();
}