using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaDistribution.Client.Data
{
    internal class FilePartRepository
    {
        private readonly IClientDbContextFactory factory;

        public FilePartRepository(IClientDbContextFactory factory)
        {
            this.factory = factory;
        }

        //public int Add(filePartMessage )
    }
}
