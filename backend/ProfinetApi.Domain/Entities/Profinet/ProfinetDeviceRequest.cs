using System;
using System.Collections.Generic;
using System.Text;

namespace ProfinetApi.Domain.Entities.Profinet
{
    public class ProfinetDeviceRequest
    {
        public string MacAddress { get; set; }
        public string RequestedName { get; set; } 
        public DateTime LastSeen { get; set; }
    }
}
