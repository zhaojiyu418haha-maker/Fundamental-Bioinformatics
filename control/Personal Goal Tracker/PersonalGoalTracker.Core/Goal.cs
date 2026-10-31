using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalGoalTracker.Core
{
    public class Goal
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Period { get; set; } // week, month, year
        public string Status { get; set; } // active, completed
    }
}
