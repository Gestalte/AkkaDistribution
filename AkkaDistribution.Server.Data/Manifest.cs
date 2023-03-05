using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaDistribution.Server.Data
{
    public class Manifest
    {
        public int ManifestId { get; set; }
        public DateTime Timestamp { get; set; }
        public ICollection<ManifestEntry> ManifestEntries { get; set; }
    }
}
