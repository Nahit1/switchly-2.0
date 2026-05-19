namespace Switchly_2._0.WebApi.Models.Enums;

public enum RolloutScheduleStatus
{
    Draft = 1,        // henüz başlatılmadı — şimdilik kullanılmıyor
    Active = 2,       // auto-promoter çalışıyor
    Paused = 3,       // manuel pause edilmiş, resume bekliyor
    Completed = 4,    // tüm adımlar tamamlandı (terminal)
    RolledBack = 5    // rollback yapıldı (terminal)
}
