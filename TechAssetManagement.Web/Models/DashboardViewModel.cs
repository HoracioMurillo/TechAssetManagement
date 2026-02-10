namespace TechAssetManagement.Web.Models
{
    //Clase que representa los datos para el dashboard principal de la aplicación#}
    public class DashboardViewModel
    {
        public int TotalAssets { get; set; }
        public int AvailableAssets { get; set; }
        public int AssignedAssets { get; set; }
        public int AssetsInMaintenance { get; set; }
        public int OpenTickets { get; set; }
        public int CriticalTickets { get; set; }
        public List<Core.Entities.Ticket> RecentTickets { get; set; }
    }
}