using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Craftelio.DataAccess.DbInitializer
{
    public interface IDbInitializer
    {
        void Initialize();
		Task InitializeAsync();
	}
}
