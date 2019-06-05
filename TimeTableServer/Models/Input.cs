using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TimeTableServer.Models
{
    public class Input
    {
        public int StudyingSystem { get; set; }
        public ICollection<Record> Records { get; set; }
    }

    public class Record
    {
        public string ClassName { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
    }

    public class Appointment
    {
        public string Subject { get; set; }
        public string Teacher { get; set; }
        public int NumOfHours { get; set; }
    }
}
