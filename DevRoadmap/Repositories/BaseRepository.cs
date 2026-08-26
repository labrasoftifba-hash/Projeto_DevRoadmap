using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DevRoadmap.Repositories
{
    public abstract class BaseRepository
    {

        protected readonly string _stringConnection;
        public BaseRepository(IConfiguration configuration)
        {
            _stringConnection = configuration.GetConnectionString("DefaultConnection");

            if(_stringConnection == null)
            {
                throw new ArgumentNullException(nameof(_stringConnection));
            }
        }
    }
}