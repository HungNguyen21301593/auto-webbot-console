using System;
using System.Collections.Generic;
using System.Text;

namespace AutoBot.Model
{
    class AppSetting
    {
        public Credential Credential { get; set; }
        public string OutputFilePrefix { get; set; }
        public Filter Filter { get; set; }
    }
    class Filter
    {
        public FixedPriceProjects FixedPriceProjects { get; set; }
        public HourlyProjects HourlyProjects { get; set; }
        public string Duration { get; set; }
        public Contests Contests { get; set; }
        public string Type { get; set; }
        public string Skills { get; set; }
        public string English { get; set; }
    }
    class FixedPriceProjects
    {
        public bool Check { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }

    class HourlyProjects
    {
        public bool Check { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }

    class Contests
    {
        public bool Check { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }
    class Credential
    {
        public string Email { get; set; }
        public string Pass { get; set; }
    }
}
