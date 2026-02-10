namespace TechAssetManagement.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalAssets { get; set; }
        public int AvailableAssets { get; set; }
        public int AssignedAssets { get; set; }
        public int AssetsInMaintenance { get; set; }

        public int OpenTickets { get; set; }
        public int CriticalTickets { get; set; }

        // Lista de últimos tickets para mostrar una tabla rápida
        public List<Core.Entities.Ticket> RecentTickets { get; set; }
    }
}