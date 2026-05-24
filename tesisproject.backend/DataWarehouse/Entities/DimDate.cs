using System;

namespace tesisproject.backend.DataWarehouse.Entities
{
    public class DimDate
    {
        public int DateKey { get; set; }
        public DateTime Date { get; set; }

        public int Year { get; set; }
        public int Quarter { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = null!;
        public int Day { get; set; }
        public int WeekOfYear { get; set; }
        public bool IsWeekend { get; set; }
    }
}
